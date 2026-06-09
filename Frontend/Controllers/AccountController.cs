using Microsoft.AspNetCore.Mvc;
using Frontend.Models;
using Backend.Services;
using Backend.Models;

namespace Frontend.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;

        public AccountController(ILogger<AccountController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Password))
            {
                try
                {
                    var (success, message, user) = await _userService.LoginUserAsync(model.Email, model.Password);

                    if (success && user != null)
                    {
                        HttpContext.Session.SetInt32("UserId", user.MaNguoiDung);
                        HttpContext.Session.SetString("UserEmail", user.Email ?? "");
                        HttpContext.Session.SetString("UserName", user.HoTen ?? "");

                        TempData["SuccessMessage"] = message;
                        return RedirectToAction("Dashboard", "Home");
                    }
                    else
                    {
                        ModelState.AddModelError("", message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi đăng nhập");
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.FullName) && !string.IsNullOrEmpty(model.Password))
            {
                try
                {
                    string tenDangNhap = model.Email.Split('@')[0];

                    var (success, message, userId) = await _userService.RegisterUserAsync(
                        tenDangNhap,
                        model.Email,
                        model.FullName,
                        model.Password
                    );

                    if (success)
                    {
                        TempData["SuccessMessage"] = message;
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("", message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi đăng ký");
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại.");
                }
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Login");
        }
    }
}
