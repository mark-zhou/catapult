using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using website.Models;

namespace website.Controllers
{
	public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly Users.UserStore _userStore;

		public AdminController(ILogger<AdminController> logger, Users.UserStore userStore)
		{
			_logger = logger;
			_userStore = userStore;
		}

		public IActionResult Index()
        {
            if (_userStore.IsEmpty)
            {
                return RedirectToAction("create-user");
			}

            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login");
			}

			return View();
        }

        [HttpGet]
        [ActionName("create-user")]
		public IActionResult CreateUser()
        {
			return View("CreateUser");
		}

        [HttpPost]
		[ActionName("create-user")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateUserAsync(string username, string password)
		{
			if (User.Identity?.IsAuthenticated != true)
			{
                if (_userStore.IsEmpty)
                {
					_logger.LogWarning("Creating first user.");
				}
				else
                {
                    _logger.LogError("User is not authenticated and user store is not empty.");
                    return RedirectToAction("Login");
                }
			}

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
			{
				_logger.LogWarning("Username or password is empty.");
				return View();
			}

			if (password.Length < 8)
			{
				_logger.LogWarning("Password must be at least 8 characters long.");
				ViewBag.Error = "Password must be at least 8 characters long.";
				return View();
			}

			var hashedPassword = HashPassword(password);

			try
			{
				_userStore.AddUser(username, hashedPassword);
				_logger.LogInformation($"User '{username}' created successfully.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to create user '{username}'.");
				return BadRequest(ex.Message);
			}

            if (User.Identity?.IsAuthenticated == false)
            {
                return await Login(username, password);
			}

			return RedirectToAction("Index");
		}

		static string HashPassword(string password)
		{
			var sha256 = SHA256.Create();
			var hashedPassword = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
			return hashedPassword;
		}

		[HttpGet]
        public IActionResult Login()
        {
            return View();
        }

		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

			var hashedPassword = HashPassword(password);

			if (_userStore.TryLogin(username, hashedPassword) != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username)
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"User '{username}' logged in successfully.");
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return Redirect("/");
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
