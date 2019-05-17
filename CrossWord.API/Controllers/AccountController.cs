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

        public AccountController(IConfiguration config, UserManager<IdentityUser> userManager, WordHintDbContext db)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserModel user)
        {
            string username = user.Username;
            string password = user.Password;

            var userIdentity = new IdentityUser(username);
            var result = await userManager.CreateAsync(userIdentity, password);

            if (result == IdentityResult.Success)
            {
                return await Login(user);
            }
            else return BadRequest(result);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(UserModel user)
        {
            string username = user.Username;
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

                return Ok(new JwtSecurityTokenHandler().WriteToken(token));
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AddRole(RoleModel user)
        {
            string username = user.Username;
            string password = user.Password;
            string role = user.Role;

            // get the IdentityUser to verify
            var userToVerify = await userManager.FindByNameAsync(username);

            if (userToVerify == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }

            // check the credentials
            if (await userManager.CheckPasswordAsync(userToVerify, password) && !string.IsNullOrEmpty(role))
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
        public async Task<ActionResult> GenerateForgotPasswordToken(string email)
        {
            var user = await userManager.FindByNameAsync(email);
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
        public async Task<ActionResult> ResetPassword(string email, string password, string code)
        {
            var user = await userManager.FindByNameAsync(email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest();
            }
            var result = await userManager.ResetPasswordAsync(user, code, password);
            if (result.Succeeded)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public string PingAdmin()
        {
            return "Pong";
        }
    }
}