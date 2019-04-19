using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using System.Linq;

namespace CrossWord.API.Controllers
{

    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
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
        // ValidateAntiForgeryToken won't work unless we are using the default Identity UI via AddDefaultUI()
        // [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Register(string email, string password)
        {
            var userIdentity = new IdentityUser(email);
            var result = await userManager.CreateAsync(userIdentity, password);

            if (result == IdentityResult.Success)
            {
                return await Login(email, password);
            }
            else return BadRequest();
        }

        [HttpPost]
        [AllowAnonymous]
        // ValidateAntiForgeryToken won't work unless we are using the default Identity UI via AddDefaultUI()
        // [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            // get the IdentityUser to verify
            var userToVerify = await userManager.FindByNameAsync(email);

            if (userToVerify == null) return BadRequest();

            // check the credentials
            if (await userManager.CheckPasswordAsync(userToVerify, password))
            {
                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

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
            else return BadRequest();
        }

        // GET: /Account/GenerateForgotPasswordToken
        [HttpGet]
        [AllowAnonymous]
        [ActionName("GenerateForgotPasswordToken")]
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

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        // ValidateAntiForgeryToken won't work unless we are using the default Identity UI via AddDefaultUI()
        // [ValidateAntiForgeryToken]
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
                return Ok();
            }
            else return BadRequest();
        }
    }
}