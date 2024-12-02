using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Project1.Models;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
namespace Project1.Controllers
{
    public class LoginController : Controller
    {
        PizzaOnlineContext db = new PizzaOnlineContext();
        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(TUser user)
        {
            if (HttpContext.Session.GetString("Email") == null)
            {
                var u = db.TUsers.FirstOrDefault(x => x.Email.Equals(user.Email) && x.Password.Equals(user.Password));

                if (u != null)
                {
                    HttpContext.Session.SetString("Email", u.Email.ToString());
                    HttpContext.Session.SetString("UserId", u.UserId.ToString());
                    ViewData["UserId"] = this.User;
                    TempData["Title"] = "Thành công";
                    TempData["Content"] = "Đăng nhập thành công.";
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, u.Email),
                        new Claim("UserId", u.UserId.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var clainsPricipal = new ClaimsPrincipal(claimsIdentity);

                    /*AuthenticationProperties authProperties = new AuthenticationProperties();*/

                    /* if (user.RememberMe)
                     {
                         authProperties.IsPersistent = true;
                         authProperties.ExpiresUtc = DateTime.Now.AddDays(2);
                     }

                     await HttpContext.SignInAsync(clainsPricipal, authProperties);*/

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = (bool)user.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                    if (u.RoleId == 1)
                    {
                        HttpContext.Session.SetString("UserNickname", u.Email.ToString());
                        return RedirectToAction("Index", "HomeAdmin", new { area = "Admin" });
                    }
                    else
                    {
                        return RedirectToAction("Index", "Menu", new { userId = u.UserId });
                    }
                    
                }
                else
                {
                    TempData["Title"] = "Thất bại";
                    TempData["Content"] = "Email hoặc mật khẩu không đúng.";
                    if (!ModelState.IsValid)
                    {
                        return View(user);
                    }
                    return RedirectToAction("Index", "Login");
                }
            }
            return RedirectToAction("Index", "Home");
        }

        

        public async Task LoginGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse")
            });
        }

        public async Task<IActionResult> GoogleResponse()
        {
            // Kiểm tra nếu có bất kỳ lỗi nào hoặc người dùng từ chối đăng nhập
            var error = Request.Query["error"].ToString();
            if (!string.IsNullOrEmpty(error) && error.Equals("access_denied", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Login");
            }

            // Kiểm tra xem có truy vấn "error" khác hay không và điều hướng về login
            if (Request.Query.ContainsKey("error"))
            {
                return RedirectToAction("Index", "Login");
            }

            // Xác thực người dùng thông qua Cookie
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result?.Principal == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Kiểm tra các claims nếu cần
            var claim = result.Principal.Identities.FirstOrDefault()?.Claims.Select(claim => new
            {
                claim.Issuer,
                claim.OriginalIssuer,
                claim.Type,
                claim.Value
            });

            var emailClaim = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var nameClaim = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(emailClaim) || string.IsNullOrEmpty(nameClaim))
            {
                return RedirectToAction("Index", "Login");
            }

            // Nếu không  claim, quay lại trang login
            if (claim == null)
            {
                return RedirectToAction("Index", "Login");
            }
            HttpContext.Session.SetString("Email", emailClaim);
            HttpContext.Session.SetString("Name", nameClaim);
            ViewData["UserId"] = this.User;
            TempData["Title"] = "Thành công";
            TempData["Content"] = "Đăng nhập thành công.";

            // Nếu tất cả đều thành công, điều hướng đến trang chủ
            return RedirectToAction("Index", "Menu", new { area = "" });
        }

        public IActionResult LogoutUsual()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("Email");
            return RedirectToAction("Index", "Login");
        }
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }
    }
}
