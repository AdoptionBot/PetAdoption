using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PetAdoption.Services.Interfaces;
using PetAdoption.Data.TableStorage;
using PetAdoption.Data.TableStorage.Enums;

namespace PetAdoption.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("login")]
        public IActionResult Login(string scheme, string returnUrl = "/")
        {
            var authProperties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback), new { returnUrl }),
                Items = { { "scheme", scheme } }
            };

            return Challenge(authProperties, scheme);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string returnUrl = "/")
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            if (!authenticateResult.Succeeded)
                return Redirect("/login");

            var email = authenticateResult.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = authenticateResult.Principal?.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return Redirect("/login");

            // Check if user exists in database
            var user = await _userService.GetUserByEmailAsync(email);
            
            if (user == null)
            {
                // Create new user with default User role
                user = new User
                {
                    PartitionKey = name ?? email.Split('@')[0],
                    RowKey = email,
                    PhoneNumber = "",
                    Address = "",
                    Country = "",
                    Role = UserRole.User,
                    AccountDisabled = false,
                    ProfileCompleted = false
                };
                
                try
                {
                    await _userService.CreateUserAsync(user);
                    _logger.LogInformation($"Created new user: {email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create user: {email}");
                }
            }
            else if (user.AccountDisabled)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Redirect("/access-denied");
            }

            // Add role claim
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name ?? email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });

            // Redirect to profile page if profile is not complete
            if (!user.ProfileCompleted)
            {
                return Redirect("/profile");
            }

            return Redirect(returnUrl);
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }
    }
}