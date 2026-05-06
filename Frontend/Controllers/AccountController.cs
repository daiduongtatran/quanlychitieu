using Microsoft.AspNetCore.Mvc;
using Frontend.Models;

namespace Frontend.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // TODO: Implement login logic with backend
                    // For now, we'll just validate and store in session
                    
                    // In a real application, you would:
                    // 1. Validate against database
                    // 2. Create authentication token/cookie
                    // 3. Redirect to dashboard
                    
                    // Temporary: Store in session for demo
                    if (!string.IsNullOrEmpty(model.Email))
                    {
                        HttpContext.Session.SetString("UserEmail", model.Email);
                    }
                    
                    TempData["SuccessMessage"] = "Đăng nhập thành công!";
                    return RedirectToAction("Dashboard", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi đăng nhập");
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại.");
                }
            }

            return View(model);
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // TODO: Implement registration logic with backend
                    // For now, we'll just validate
                    
                    // In a real application, you would:
                    // 1. Check if email exists in database
                    // 2. Hash password
                    // 3. Save user to database
                    // 4. Send verification email
                    // 5. Create authentication token
                    
                    // Temporary: Store in session for demo
                    if (!string.IsNullOrEmpty(model.Email))
                    {
                        HttpContext.Session.SetString("UserEmail", model.Email);
                    }
                    if (!string.IsNullOrEmpty(model.FullName))
                    {
                        HttpContext.Session.SetString("UserName", model.FullName);
                    }
                    
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng xác nhận email của bạn.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi đăng ký");
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại.");
                }
            }

            return View(model);
        }

        // GET: Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("email", "Email không được bỏ trống");
                return View();
            }

            // TODO: Implement password reset logic
            // 1. Check if email exists
            // 2. Generate reset token
            // 3. Send reset link via email
            
            TempData["InfoMessage"] = "Hướng dẫn đặt lại mật khẩu đã được gửi đến email của bạn.";
            return RedirectToAction("Login");
        }
    }
}
