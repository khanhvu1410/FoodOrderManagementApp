using Project1.Models;
using Project1.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using Project1.Services;

namespace Project1.Controllers
{
    public class UserController : Controller
    {
        private readonly PizzaOnlineContext _db;
        private readonly EmailService _emailService;

        public UserController(PizzaOnlineContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Resgister()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Resgister(RegisterUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var maxUserId = _db.TUsers.Max(u => u.UserId);

                var lastUserCode = _db.TUsers
               .Where(u => u.Code.StartsWith("USER"))
               .OrderByDescending(u => u.Code)
               .Select(u => u.Code)
               .FirstOrDefault();

                // Tách phần số ra khỏi Code
                int newCodeNumber = 1;
                if (lastUserCode != null && lastUserCode.Length > 4)
                {
                    int.TryParse(lastUserCode.Substring(4), out newCodeNumber);
                    newCodeNumber++;
                }

                // Tạo Code mới dạng USER + số tăng dần
                var newUserCode = "USER" + newCodeNumber.ToString("D2");

                var user = new TUser
                {
                    UserId = maxUserId + 1,
                    RoleId = 2,
                    Code = newUserCode,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Nickname = model.Nickname,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Password = model.Password,
                    Address = model.Address,
                    Point = 0,
                    Deleted = false
                };

                _db.TUsers.Add(user);
                _db.SaveChanges(); 

                return RedirectToAction("Index", "Login");
            }

            return View(model);
        }

        // GET: Hiển thị form quên mật khẩu
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Xử lý gửi mail quên mật khẩu
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = _db.TUsers.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                TempData["Error"] = "Email không tồn tại trong hệ thống!";
                return View();
            }

            try
            {
                string subject = "Khôi phục mật khẩu";
                string body = $@"
                <h2>Xin chào {user.Email},</h2>
                <p>Mật khẩu của bạn là: <strong>{user.Password}</strong></p>
                <p>Vui lòng đăng nhập và đổi mật khẩu để bảo mật tài khoản.</p>
                <p>Trân trọng,<br/>Pizza Online</p>";

                await _emailService.SendEmailAsync(email, subject, body);

                TempData["Success"] = "Mật khẩu đã được gửi đến email của bạn!";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // Log the actual error
                TempData["Error"] = $"Có lỗi xảy ra khi gửi email: {ex.Message}";
                return View();
            }
        }
    }
}
