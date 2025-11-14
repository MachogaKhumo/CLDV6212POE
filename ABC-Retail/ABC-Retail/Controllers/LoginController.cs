using ABC_Retail.Data;
using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ABC_Retail.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<LoginController> _logger;

        public LoginController(AuthDbContext db, IFunctionsApi functionsApi, ILogger<LoginController> logger)
        {
            _db = db;
            _functionsApi = functionsApi;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (user == null)
                {
                    ViewBag.Error = "Invalid username or password.";
                    return View(model);
                }

                if (user.PasswordHash != model.Password)
                {
                    ViewBag.Error = "Invalid username or password.";
                    return View(model);
                }

                var customers = await _functionsApi.GetCustomersAsync();
                var customer = customers.FirstOrDefault(c => c.Username == user.Username);

                if (customer == null)
                {
                    _logger.LogWarning("No matching customer found in Azure for username {Username}", user.Username);
                    ViewBag.Error = "No customer record found in the system.";
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("CustomerId", customer.RowKey)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                    });

                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("CustomerId", customer.RowKey);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return user.Role switch
                {
                    "Admin" => RedirectToAction("AdminDashboard", "Home"),
                    _ => RedirectToAction("CustomerDashboard", "Home")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected login error for user {Username}", model.Username);
                ViewBag.Error = "Unexpected error occurred during login. Please try again later.";
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            try
            {
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.Password,
                    Role = model.Role
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                var customer = new Customer
                {
                    Username = model.Username,
                    Name = model.FirstName,
                    Surname = model.LastName,
                    Email = model.Email,
                    ShippingAddress = model.ShippingAddress
                };

                await _functionsApi.CreateCustomerAsync(customer);

                TempData["Success"] = "Registration successful! Please log in.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {Username}", model.Username);
                ViewBag.Error = "Could not complete registration. Please try again later.";
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}