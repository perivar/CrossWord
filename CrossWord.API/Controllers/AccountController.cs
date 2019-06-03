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
using AutoMapper;

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
        private readonly UserManager<IdentityUser> userManager;
        private readonly WordHintDbContext db;
        private readonly IMapper mapper;

        public AccountController(IConfiguration config, UserManager<IdentityUser> userManager, WordHintDbContext db, IMapper mapper)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
            this.mapper = mapper;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserModel userModel)
        {
            // map dto to entity and set id
            var identityUser = mapper.Map<IdentityUser>(userModel);
            identityUser.Id = null;

            var result = await userManager.CreateAsync(identityUser, userModel.Password);

            if (result == IdentityResult.Success)
            {
                return await Login(new UserNamePasswordModel() { UserName = userModel.UserName, Password = userModel.Password });
            }
            else return BadRequest(result);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(UserNamePasswordModel user)
        {
            string username = user.UserName;
            string password = user.Password;

            // get the IdentityUser to verify
            var userToVerify = await userManager.FindByNameAsync(username);

            if (userToVerify == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }

            // check the credentials
            if (await userManager.CheckPasswordAsync(userToVerify, password))
            {
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Adding roles code
                var userClaims = await userManager.GetClaimsAsync(userToVerify);        // UserManager.GetClaimsAsync(user) queries the UserClaims table.
                // var roleClaims = await roleManager.GetClaimsAsync(userToVerify);     // RoleManager.GetClaimsAsync(role) queries the RoleClaims table.
                // var roles = await userManager.GetRolesAsync(userToVerify);           // System.NotSupportedException: Store does not implement IUserRoleStore<TUser>.
                claims.AddRange(userClaims);

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.AddSeconds(Convert.ToDouble(config["Jwt:ExpireSeconds"]));

                var token = new JwtSecurityToken(
                    issuer: config["Jwt:Issuer"],
                    audience: config["Jwt:Audience"],
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // return basic user info (without password) and token to store client side
                var userModel = mapper.Map<UserModel>(userToVerify);
                return Ok(new
                {
                    User = userModel,
                    Claims = userClaims,
                    Token = tokenString
                });
            }
            else
            {
                return BadRequest();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddRole(RoleModel user)
        {
            string username = user.UserName;
            string role = user.Role;

            // get the IdentityUser to verify
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
            var users = userManager.Users;
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
        public async Task<IActionResult> Update(string username, [FromBody]UserModel userModel)
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