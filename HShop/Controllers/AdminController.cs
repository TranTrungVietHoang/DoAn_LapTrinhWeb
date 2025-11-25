using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HShop.Data;

namespace HShop.Controllers
{
    [Authorize(Roles = "Admin")] // ✅ Chỉ Admin được phép truy cập
    public class AdminController : Controller
    {
        private readonly Hshop2023Context _db;

        public AdminController(Hshop2023Context context)
        {
            _db = context;
        }

        // ✅ Trang chính (Dashboard)
        public IActionResult Index()
        {
            ViewBag.Title = "Trang quản trị hệ thống";
            return View();
        }

        // ✅ Quản lý hàng hóa
        public IActionResult QuanLyHangHoa()
        {
            var hangHoas = _db.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Include(h => h.MaNccNavigation)
                .ToList();

            return View("~/Views/HangHoas/Index.cshtml", hangHoas);
        }

        // ✅ Form thêm hàng hóa (GET)
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                // ⚙️ Đảm bảo không null ViewBag dù DB trống
                ViewBag.LoaiList = _db.Loais?.ToList() ?? new List<Loai>();
                ViewBag.NccList = _db.NhaCungCaps?.ToList() ?? new List<NhaCungCap>();

                // ✅ Ngăn lỗi TempData null reference
                TempData["Success"] = "";
                TempData["Error"] = "";
            }
            catch (Exception ex)
            {
                // Trường hợp DB lỗi hoặc chưa khởi tạo
                ViewBag.LoaiList = new List<Loai>();
                ViewBag.NccList = new List<NhaCungCap>();
                TempData["Error"] = $"⚠️ Lỗi tải dữ liệu: {ex.Message}";
            }

            return View("ThemHangHoa", new HangHoa());
        }

        // ✅ Xử lý thêm hàng hóa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HangHoa hh, IFormFile? Hinh)
        {
            ViewBag.LoaiList = _db.Loais?.ToList() ?? new List<Loai>();
            ViewBag.NccList = _db.NhaCungCaps?.ToList() ?? new List<NhaCungCap>();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "❌ Vui lòng nhập đầy đủ thông tin hợp lệ.";
                return View("ThemHangHoa", hh);
            }

            try
            {
                // ✅ Upload hình ảnh
                if (Hinh != null && Hinh.Length > 0)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", "HangHoa");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Path.GetFileNameWithoutExtension(Hinh.FileName);
                    string extension = Path.GetExtension(Hinh.FileName);
                    string safeFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    string filePath = Path.Combine(folder, safeFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        Hinh.CopyTo(stream);
                    }

                    hh.Hinh = safeFileName;
                }
                else
                {
                    hh.Hinh = "no-image.png";
                }

                _db.HangHoas.Add(hh);
                _db.SaveChanges();

                TempData["Success"] = "✅ Đã thêm hàng hóa thành công!";
                return RedirectToAction(nameof(QuanLyHangHoa));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi thêm hàng hóa: {ex.Message}";
                return View("ThemHangHoa", hh);
            }
        }

        // ✅ Form sửa hàng hóa (GET)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var hh = _db.HangHoas.Find(id);
            if (hh == null)
            {
                TempData["Error"] = "Không tìm thấy hàng hóa cần sửa.";
                return RedirectToAction(nameof(QuanLyHangHoa));
            }

            ViewBag.LoaiList = _db.Loais?.ToList() ?? new List<Loai>();
            ViewBag.NccList = _db.NhaCungCaps?.ToList() ?? new List<NhaCungCap>();

            return View("ThemHangHoa", hh);
        }

        // ✅ Cập nhật hàng hóa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HangHoa hh, IFormFile? Hinh)
        {
            ViewBag.LoaiList = _db.Loais?.ToList() ?? new List<Loai>();
            ViewBag.NccList = _db.NhaCungCaps?.ToList() ?? new List<NhaCungCap>();

            var existing = _db.HangHoas.Find(hh.MaHh);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy hàng hóa cần cập nhật.";
                return RedirectToAction(nameof(QuanLyHangHoa));
            }

            try
            {
                existing.TenHh = hh.TenHh;
                existing.DonGia = hh.DonGia;
                existing.MoTa = hh.MoTa;
                existing.MaLoai = hh.MaLoai;
                existing.MaNcc = hh.MaNcc;

                if (Hinh != null && Hinh.Length > 0)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", "HangHoa");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Path.GetFileNameWithoutExtension(Hinh.FileName);
                    string extension = Path.GetExtension(Hinh.FileName);
                    string safeFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    string filePath = Path.Combine(folder, safeFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        Hinh.CopyTo(stream);
                    }

                    if (!string.IsNullOrEmpty(existing.Hinh) && existing.Hinh != "no-image.png")
                    {
                        string oldPath = Path.Combine(folder, existing.Hinh);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    existing.Hinh = safeFileName;
                }

                _db.SaveChanges();
                TempData["Success"] = "✅ Cập nhật hàng hóa thành công!";
                return RedirectToAction(nameof(QuanLyHangHoa));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi sửa hàng hóa: {ex.Message}";
                return View("ThemHangHoa", hh);
            }
        }

        // ✅ Xem chi tiết hàng hóa (GET)
        [HttpGet]
        public IActionResult Details(int id)
        {
            var hh = _db.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Include(h => h.MaNccNavigation)
                .FirstOrDefault(h => h.MaHh == id);

            if (hh == null)
            {
                TempData["Error"] = "Không tìm thấy hàng hóa.";
                return RedirectToAction(nameof(QuanLyHangHoa));
            }

            return View("~/Views/HangHoas/Details.cshtml", hh);
        }

        // ✅ Xóa hàng hóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                var hh = _db.HangHoas.Find(id);
                if (hh == null)
                {
                    TempData["Error"] = "Không tìm thấy hàng hóa cần xóa.";
                    return RedirectToAction(nameof(QuanLyHangHoa));
                }

                if (!string.IsNullOrEmpty(hh.Hinh) && hh.Hinh != "no-image.png")
                {
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", "HangHoa", hh.Hinh);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                _db.HangHoas.Remove(hh);
                _db.SaveChanges();

                TempData["Success"] = "🗑️ Đã xóa hàng hóa thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi xóa hàng hóa: {ex.Message}";
            }

            return RedirectToAction(nameof(QuanLyHangHoa));
        }

        // ✅ Quản lý khách hàng
        public IActionResult QuanLyKhachHang()
        {
            var khachHangs = _db.KhachHangs.ToList();
            return View(khachHangs);
        }

        // ✅ Khóa / Mở khóa tài khoản khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KhoaTaiKhoan(string maKh)
        {
            var kh = _db.KhachHangs.Find(maKh);
            if (kh == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(QuanLyKhachHang));
            }

            kh.HieuLuc = !kh.HieuLuc;
            _db.SaveChanges();

            TempData["Success"] = kh.HieuLuc
                ? "🔓 Đã mở khóa tài khoản!"
                : "🔒 Đã khóa tài khoản!";

            return RedirectToAction(nameof(QuanLyKhachHang));
        }

        // ✅ Quản lý đơn hàng
        public IActionResult QuanLyDonHang()
        {
            var donHangs = _db.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                .ToList();

            return View(donHangs);
        }

        // ✅ Cập nhật trạng thái đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhatTrangThai(int maHd, int maTrangThai)
        {
            var hd = _db.HoaDons.Find(maHd);
            if (hd == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(QuanLyDonHang));
            }

            try
            {
                hd.MaTrangThai = maTrangThai;
                _db.SaveChanges();
                TempData["Success"] = "✅ Cập nhật trạng thái đơn hàng thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi cập nhật đơn hàng: {ex.Message}";
            }

            return RedirectToAction(nameof(QuanLyDonHang));
        }

        // ✅ Quản lý bình luận
        public IActionResult QuanLyBinhLuan()
        {
            var comments = _db.Comments
                .Include(c => c.MaHHNavigation)
                .Include(c => c.MaKHNavigation)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();

            return View(comments);
        }

        // ✅ Xóa bình luận
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaBinhLuan(int id)
        {
            try
            {
                var comment = _db.Comments.Find(id);
                if (comment == null)
                {
                    TempData["Error"] = "Không tìm thấy bình luận cần xóa.";
                    return RedirectToAction(nameof(QuanLyBinhLuan));
                }

                // Xóa hình ảnh nếu có
                if (!string.IsNullOrEmpty(comment.ImagePath))
                {
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", comment.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _db.Comments.Remove(comment);
                _db.SaveChanges();

                TempData["Success"] = "🗑️ Đã xóa bình luận thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi xóa bình luận: {ex.Message}";
            }

            return RedirectToAction(nameof(QuanLyBinhLuan));
        }

        // ✅ Ẩn/Hiện bình luận
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AnHienBinhLuan(int id)
        {
            try
            {
                var comment = _db.Comments.Find(id);
                if (comment == null)
                {
                    TempData["Error"] = "Không tìm thấy bình luận.";
                    return RedirectToAction(nameof(QuanLyBinhLuan));
                }

                comment.IsHidden = !comment.IsHidden;
                _db.SaveChanges();

                TempData["Success"] = comment.IsHidden
                    ? "👁️‍🗨️ Đã ẩn bình luận!"
                    : "👁️ Đã hiện bình luận!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi thay đổi trạng thái: {ex.Message}";
            }

            return RedirectToAction(nameof(QuanLyBinhLuan));
        }
    }
}
