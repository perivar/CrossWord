using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using CrossWord.API.Models;
using CrossWord.Scraper.MySQLDbService.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using CrossWord.API.Services;

namespace CrossWord.API.Controllers
{
    [Produces("application/json")]
    [ApiController] // Note this breaks HttpPost parameters that are not a model, like a Login method with username and password as string parameters
    // [ApiVersionNeutral]
    [ApiVersion("1.0")] // this attribute isn't required, but it's easier to understand
    [Route("api/[controller]/[action]")]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration config;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly WordHintDbContext db;
        private readonly IMapper mapper;
        private readonly ITokenService tokenService;

        public AccountController(IConfiguration config, UserManager<ApplicationUser> userManager, WordHintDbContext db, IMapper mapper, ITokenService tokenService)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
            this.mapper = mapper;
            this.tokenService = tokenService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserModelRegister userRegister)
        {
            // map dto to entity
            var ApplicationUser = mapper.Map<ApplicationUser>(userRegister);

            var result = await userManager.CreateAsync(ApplicationUser, userRegister.Password);

            if (result == IdentityResult.Success)
            {
                return await Login(new UserModelLogin() { UserName = userRegister.UserName, Password = userRegister.Password });
            }
            else return BadRequest(result);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(UserModelLogin userLogin)
        {
            string username = userLogin.UserName;
            string password = userLogin.Password;

            // get the ApplicationUser to verify
            //var userToVerify = await userManager.FindByNameAsync(username);
            var userToVerify = userManager.Users.Include(b => b.RefreshTokens).Single(u => u.UserName == username);

            if (userToVerify == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }

            // check the credentials
            if (await userManager.CheckPasswordAsync(userToVerify, password))
            {
                var claims = new List<Claim>()
                {
                    new Claim(JwtRegisteredClaimNames.Sub, username), // The "sub" (subject) claim identifies the principal that is the subject of the JWT.
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // The "jti" (JWT ID) claim provides a unique identifier for the JWT.
                };

                // Adding roles code
                var userClaims = await userManager.GetClaimsAsync(userToVerify);        // UserManager.GetClaimsAsync(user) queries the UserClaims table.
                // var roleClaims = await roleManager.GetClaimsAsync(userToVerify);     // RoleManager.GetClaimsAsync(role) queries the RoleClaims table.
                // var roles = await userManager.GetRolesAsync(userToVerify);           // System.NotSupportedException: Store does not implement IUserRoleStore<TUser>.
                claims.AddRange(userClaims);

                // generate access token
                var accessToken = tokenService.GenerateAccessToken(claims);

                // generate and add refresh token
                var refreshToken = tokenService.GenerateRefreshToken();
                userToVerify.AddRefreshToken(refreshToken, Request.HttpContext.Connection.RemoteIpAddress?.ToString());
                await userManager.UpdateAsync(userToVerify);

                // return basic user info (without password) and token to store client side
                var userModel = mapper.Map<UserModel>(userToVerify);
                return Ok(new
                {
                    User = userModel,
                    Claims = userClaims,
                    Token = accessToken,
                    RefreshToken = refreshToken
                });
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(string token, string refreshToken)
        {
            var principal = tokenService.GetPrincipalFromExpiredToken(token);

            // invalid token/signing key was passed and we can't extract user claims
            if (principal != null)
            {
                // The "sub" (subject) claim identifies the principal that is the subject of the JWT.
                var userName = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                if (userName != null)
                {
                    var user = userManager.Users.Include(b => b.RefreshTokens).SingleOrDefault(u => u.UserName == userName.Value);
                    if (user != null && user.HasValidRefreshToken(refreshToken))
                    {
                        var newJwtToken = tokenService.GenerateAccessToken(principal.Claims);
                        var newRefreshToken = tokenService.GenerateRefreshToken();

                        user.RemoveRefreshToken(refreshToken); // delete the token we've exchanged
                        user.AddRefreshToken(newRefreshToken, Request.HttpContext.Connection.RemoteIpAddress?.ToString());
                        await userManager.UpdateAsync(user);

                        return Ok(new
                        {
                            Token = newJwtToken,
                            RefreshToken = newRefreshToken
                        });
                    }
                }
            }

            return BadRequest();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddRole(RoleModel user)
        {
            string username = user.UserName;
            string role = user.Role;

            // get the ApplicationUser to verify
            var userToVerify = await userManager.FindByNameAsync(username);

            if (userToVerify == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }

            // check the credentials
            if (!string.IsNullOrEmpty(role))
            {
                var newRoleClaim = new Claim(ClaimTypes.Role, role);

                // check role hasn't already been added
                var userClaims = await userManager.GetClaimsAsync(userToVerify);
                var existingRoleClaim = userClaims.FirstOrDefault(c => c.Value == role);
                if (existingRoleClaim == null)
                {
                    await userManager.AddClaimAsync(userToVerify, newRoleClaim);
                    return Ok($"Role '{role}' successfully added to username '{username}'.");
                }
                else
                {
                    return BadRequest($"Role '{role}' already added to username '{username}'.");
                }
            }
            else
            {
                return BadRequest();
            }
        }

        // GET: /Account/GenerateForgotPasswordToken
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> GenerateForgotPasswordToken(string username)
        {
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }
            return Ok(await userManager.GeneratePasswordResetTokenAsync(user));
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword(string username, string password, string token)
        {
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }
            if (!string.IsNullOrEmpty(password))
            {
                var updateResult = await userManager.ResetPasswordAsync(user, token, password);
                if (updateResult.Succeeded)
                {
                    return Ok();
                }
                else
                {
                    return BadRequest(updateResult.Errors);
                }
            }
            else
            {
                return BadRequest("Password cannot be empty");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetAll()
        {
            // loading navigational properties on users must be done manually
            var users = userManager.Users.Include(a => a.RefreshTokens);
            var userModels = mapper.Map<IList<UserModel>>(users);
            return Ok(userModels);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{username}")]
        public async Task<IActionResult> GetByName(string username)
        {
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }
            var userClaims = await userManager.GetClaimsAsync(user);        // UserManager.GetClaimsAsync(user) queries the UserClaims table.            
            var userModel = mapper.Map<UserModel>(user);
            return Ok(new
            {
                User = userModel,
                Claims = userClaims
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{username}")]
        public async Task<IActionResult> Update(string username, [FromBody]UserModelRegister userModel)
        {
            if (username != userModel.UserName)
            {
                return BadRequest();
            }

            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }

            // Update it with the values from the view model
            user.UserName = userModel.UserName;
            user.Email = userModel.Email;
            user.PhoneNumber = userModel.PhoneNumber;

            if (!string.IsNullOrEmpty(userModel.Password))
            {
                // Generate the reset token (this would generally be sent out as a query parameter as part of a 'reset' link in an email)
                string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

                // Use the reset token to verify the provenance of the reset request and reset the password.
                var updateResult = await userManager.ResetPasswordAsync(user, resetToken, userModel.Password);

                if (!updateResult.Succeeded)
                {
                    return BadRequest(updateResult.Errors);
                }
            }

            try
            {
                await userManager.UpdateAsync(user);
                var returnUser = mapper.Map<UserModel>(user);
                return Ok(returnUser);
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{username}")]
        public async Task<IActionResult> Delete(string username)
        {
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }

            // don't delete admin users
            var userClaims = await userManager.GetClaimsAsync(user);
            var result = userClaims.FirstOrDefault(a => a.Value == "Admin");
            if (result != null)
            {
                return BadRequest("Cannot delete Admin users!");
            }

            try
            {
                var deleteResult = await userManager.DeleteAsync(user);
                return Ok(user.Id);
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
        }
    }
}