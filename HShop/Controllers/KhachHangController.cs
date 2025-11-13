using AutoMapper;
using HShop.Data;
using HShop.Helpers;
using HShop.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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

        #region Register
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DangKy(RegisterVM model, IFormFile Hinh)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var khachHang = _mapper.Map<KhachHang>(model);
                    khachHang.RandomKey = MyUtil.GenerateRamdomKey();
                    khachHang.MatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey);
                    khachHang.HieuLuc = true;
                    khachHang.VaiTro = 0;

                    if (Hinh != null)
                    {
                        khachHang.Hinh = MyUtil.UploadHinh(Hinh, "KhachHang");
                    }

                    db.Add(khachHang);
                    db.SaveChanges();
                    return RedirectToAction("Index", "HangHoa");
                }
                catch (Exception ex)
                {
                    var mess = $"{ex.Message}";
                }
            }
            return View();
        }
        #endregion


        #region Login
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
            if (ModelState.IsValid)
            {
                var khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == model.UserName);
                if (khachHang == null)
                {
                    ModelState.AddModelError("loi", "Không có khách hàng này");
                }
                else if (!khachHang.HieuLuc)
                {
                    ModelState.AddModelError("loi", "Tài khoản đã bị khóa. Vui lòng liên hệ Admin.");
                }
                else if (khachHang.MatKhau != model.Password.ToMd5Hash(khachHang.RandomKey))
                {
                    ModelState.AddModelError("loi", "Sai thông tin đăng nhập");
                }
                else
                {
                    // Lưu thông tin đăng nhập
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, khachHang.MaKh),
                        new Claim(ClaimTypes.Name, khachHang.HoTen ?? khachHang.MaKh),
                        new Claim(ClaimTypes.Email, khachHang.Email ?? ""),
                        new Claim(MySetting.CLAIM_CUSTOMERID, khachHang.MaKh),
                        new Claim(ClaimTypes.Role, "Customer")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                    if (Url.IsLocalUrl(ReturnUrl))
                        return Redirect(ReturnUrl);
                    else
                        return Redirect("/");
                }
            }
            return View();
        }
        #endregion


        #region Profile
        [Authorize]
        public IActionResult Profile()
        {
            // Lấy mã khách hàng từ Claims sau khi đăng nhập
            var maKh = User.Claims.FirstOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            if (string.IsNullOrEmpty(maKh))
            {
                return RedirectToAction("DangNhap");
            }

            // Lấy thông tin khách hàng từ DB
            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.MaKh == maKh);
            if (khachHang == null)
            {
                return RedirectToAction("DangNhap");
            }

            return View(khachHang);
        }
        #endregion


        #region Logout
        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }
        #endregion


        #region Lịch sử mua hàng
        [Authorize]
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
