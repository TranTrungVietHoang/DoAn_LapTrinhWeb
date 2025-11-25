using AutoMapper;
using HShop.Data;
using HShop.Helpers;
using HShop.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HShop.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly Hshop2023Context db;
        private readonly IMapper _mapper;

        public KhachHangController(Hshop2023Context context, IMapper mapper)
        {
            db = context;
            _mapper = mapper;
        }

        #region Đăng ký
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DangKy(RegisterVM model, IFormFile? Hinh)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var khachHang = _mapper.Map<KhachHang>(model);
                    khachHang.RandomKey = MyUtil.GenerateRamdomKey();
                    khachHang.MatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey);
                    khachHang.HieuLuc = true;
                    khachHang.VaiTro = 0; // 0 = khách hàng thường

                    if (Hinh != null)
                    {
                        khachHang.Hinh = MyUtil.UploadHinh(Hinh, "KhachHang");
                    }

                    db.KhachHangs.Add(khachHang);
                    db.SaveChanges();
                    TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập để tiếp tục.";
                    return RedirectToAction("DangNhap");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi đăng ký: {ex.Message}");
                }
            }
            return View(model);
        }
        #endregion

        #region Đăng nhập
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("loi", "Vui lòng nhập đầy đủ thông tin.");
                return View(model);
            }

            var khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == model.UserName);
            if (khachHang == null)
            {
                ModelState.AddModelError("loi", "Không tồn tại tài khoản này.");
                return View(model);
            }

            if (!khachHang.HieuLuc)
            {
                ModelState.AddModelError("loi", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            // Kiểm tra mật khẩu
            var hashedPassword = model.Password.ToMd5Hash(khachHang.RandomKey);
            if (khachHang.MatKhau != hashedPassword)
            {
                ModelState.AddModelError("loi", "Sai mật khẩu.");
                return View(model);
            }

            // Xác định vai trò
            string role = (khachHang.VaiTro == 1) ? "Admin" : "Customer";

            // Tạo danh sách claim
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, khachHang.MaKh),
                new Claim(ClaimTypes.Name, khachHang.HoTen ?? khachHang.MaKh),
                new Claim(ClaimTypes.Email, khachHang.Email ?? ""),
                new Claim(MySetting.CLAIM_CUSTOMERID, khachHang.MaKh),
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            // Điều hướng theo vai trò
            if (role == "Admin")
                return RedirectToAction("Index", "Admin"); // Trang quản trị

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return Redirect(ReturnUrl);

            return RedirectToAction("Index", "Home"); // Trang chủ khách hàng
        }
        #endregion

        #region Hồ sơ khách hàng
        [Authorize]
        public IActionResult Profile()
        {
            var maKh = User.Claims.FirstOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            if (string.IsNullOrEmpty(maKh))
                return RedirectToAction("DangNhap");

            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.MaKh == maKh);
            if (khachHang == null)
                return RedirectToAction("DangNhap");

            return View(khachHang);
        }
        #endregion

        #region Đăng xuất
        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("DangNhap");
        }
        #endregion

        #region Lịch sử mua hàng
        [Authorize(Roles = "Customer,Admin")]
        public IActionResult LichSuMuaHang()
        {
            var maKh = User.Claims.FirstOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            if (string.IsNullOrEmpty(maKh))
                return RedirectToAction("DangNhap");

            var hoaDons = db.HoaDons
                .Include(hd => hd.MaTrangThaiNavigation)
                .Where(hd => hd.MaKh == maKh)
                .OrderByDescending(hd => hd.NgayDat)
                .ToList();

            return View(hoaDons);
        }
        #endregion
    }
}
