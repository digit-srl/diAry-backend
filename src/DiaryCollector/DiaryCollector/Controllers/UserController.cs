using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DiaryCollector.Controllers {

    [Route("user")]
    public class UserController : BaseController {

        private readonly MongoConnector Mongo;

        public UserController(
            MongoConnector mongoConnector,
            LinkGenerator linker,
            ILogger<UserController> logger
        ) : base(linker, logger) {
            Mongo = mongoConnector;
        }

        [HttpGet("login")]
        public IActionResult Login() {
            return View("Login");
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginPerform(
            [FromForm] string username,
            [FromForm] string password
        ) {
            var user = await Mongo.GetUserByUsername(username);
            if(user == null) {
                return NotFound();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) {
                return NotFound();
            }

            Logger.LogInformation("Logging in user {0}", username);

            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, username)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(
                    new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
                ),
                new AuthenticationProperties {
                    AllowRefresh = true,
                    IsPersistent = true
                }
            );

            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

    }

}
