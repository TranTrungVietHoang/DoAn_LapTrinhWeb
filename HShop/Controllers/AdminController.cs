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

        // ====================================================================
        // ✅ QUẢN LÝ COUPON
        // ====================================================================

        // ✅ Xem danh sách coupons
        public IActionResult QuanLyCoupon()
        {
            try
            {
                var coupons = _db.Coupons
                    .AsNoTracking()
                    .OrderByDescending(c => c.Id)
                    .ToList();

                return View(coupons);
            }
            catch (Exception ex)
            {
                // Log lỗi để debug
                Console.WriteLine($"ERROR in QuanLyCoupon: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Trả về empty list để không crash
                return View(new List<Coupon>());
            }
        }

        // ✅ Form tạo coupon mới (GET)
        [HttpGet]
        public IActionResult TaoCoupon()
        {
            var model = new Coupon
            {
                IsActive = true,
                CreatedAt = DateTime.Now,
                Priority = 0
            };
            return View(model);
        }

        // ✅ Xử lý tạo coupon (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoCoupon(Coupon coupon)
        {
            // Validate unique code
            if (_db.Coupons.Any(c => c.Code == coupon.Code))
            {
                ModelState.AddModelError("Code", "Mã coupon này đã tồn tại.");
            }

            // Validate discount values
            if (coupon.DiscountPercent <= 0 && (!coupon.DiscountAmount.HasValue || coupon.DiscountAmount <= 0))
            {
                ModelState.AddModelError("", "Phải có ít nhất một trong hai: Giảm giá theo % hoặc theo tiền.");
            }

            if (coupon.DiscountPercent > 0 && coupon.DiscountAmount.HasValue && coupon.DiscountAmount > 0)
            {
                ModelState.AddModelError("", "Không thể có cả giảm giá theo % và theo tiền cùng lúc.");
            }

            if (coupon.DiscountPercent < 0 || coupon.DiscountPercent > 100)
            {
                ModelState.AddModelError("DiscountPercent", "Giảm giá phải từ 0 đến 100%.");
            }

            // Validate expiry date
            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate < DateTime.Now)
            {
                ModelState.AddModelError("ExpiryDate", "Ngày hết hạn phải là ngày trong tương lai.");
            }

            if (!ModelState.IsValid)
            {
                return View(coupon);
            }

            try
            {
                coupon.CreatedAt = DateTime.Now;
                _db.Coupons.Add(coupon);
                _db.SaveChanges();

                TempData["Success"] = $"✅ Đã tạo coupon '{coupon.Code}' thành công!";
                return RedirectToAction(nameof(QuanLyCoupon));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi tạo coupon: {ex.Message}";
                return View(coupon);
            }
        }

        // ✅ Form sửa coupon (GET)
        [HttpGet]
        public IActionResult SuaCoupon(int id)
        {
            var coupon = _db.Coupons.Find(id);
            if (coupon == null)
            {
                TempData["Error"] = "Không tìm thấy coupon.";
                return RedirectToAction(nameof(QuanLyCoupon));
            }

            return View("TaoCoupon", coupon);
        }

        // ✅ Xử lý sửa coupon (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaCoupon(Coupon coupon)
        {
            var existing = _db.Coupons.Find(coupon.Id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy coupon cần sửa.";
                return RedirectToAction(nameof(QuanLyCoupon));
            }

            // Validate unique code (except current coupon)
            if (_db.Coupons.Any(c => c.Code == coupon.Code && c.Id != coupon.Id))
            {
                ModelState.AddModelError("Code", "Mã coupon này đã tồn tại.");
            }

            // Validate discount values
            if (coupon.DiscountPercent <= 0 && (!coupon.DiscountAmount.HasValue || coupon.DiscountAmount <= 0))
            {
                ModelState.AddModelError("", "Phải có ít nhất một trong hai: Giảm giá theo % hoặc theo tiền.");
            }

            if (coupon.DiscountPercent > 0 && coupon.DiscountAmount.HasValue && coupon.DiscountAmount > 0)
            {
                ModelState.AddModelError("", "Không thể có cả giảm giá theo % và theo tiền cùng lúc.");
            }

            if (coupon.DiscountPercent < 0 || coupon.DiscountPercent > 100)
            {
                ModelState.AddModelError("DiscountPercent", "Giảm giá phải từ 0 đến 100%.");
            }

            if (!ModelState.IsValid)
            {
                return View("TaoCoupon", coupon);
            }

            try
            {
                // Update fields
                existing.Code = coupon.Code;
                existing.Description = coupon.Description;
                existing.DiscountPercent = coupon.DiscountPercent;
                existing.DiscountAmount = coupon.DiscountAmount;
                existing.MaxDiscount = coupon.MaxDiscount;
                existing.MinOrderAmount = coupon.MinOrderAmount;
                existing.MinQuantity = coupon.MinQuantity;
                existing.ExpiryDate = coupon.ExpiryDate;
                existing.IsActive = coupon.IsActive;
                existing.OnlyForFirstOrder = coupon.OnlyForFirstOrder;
                existing.RequiredProductId = coupon.RequiredProductId;
                existing.UsageLimit = coupon.UsageLimit;
                existing.PerUserLimit = coupon.PerUserLimit;
                existing.CouponType = coupon.CouponType;
                existing.Priority = coupon.Priority;

                _db.SaveChanges();

                TempData["Success"] = $"✅ Đã cập nhật coupon '{coupon.Code}' thành công!";
                return RedirectToAction(nameof(QuanLyCoupon));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi cập nhật coupon: {ex.Message}";
                return View("TaoCoupon", coupon);
            }
        }

        // ✅ Xóa coupon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaCoupon(int id)
        {
            try
            {
                var coupon = _db.Coupons.Find(id);
                if (coupon == null)
                {
                    TempData["Error"] = "Không tìm thấy coupon cần xóa.";
                    return RedirectToAction(nameof(QuanLyCoupon));
                }

                // Check if coupon has been used
                var hasHistory = _db.CouponHistories.Any(h => h.CouponCode == coupon.Code);
                if (hasHistory)
                {
                    TempData["Error"] = $"⚠️ Không thể xóa coupon '{coupon.Code}' vì đã có khách hàng sử dụng. Bạn có thể ẩn coupon thay vì xóa.";
                    return RedirectToAction(nameof(QuanLyCoupon));
                }

                _db.Coupons.Remove(coupon);
                _db.SaveChanges();

                TempData["Success"] = $"🗑️ Đã xóa coupon '{coupon.Code}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi xóa coupon: {ex.Message}";
            }

            return RedirectToAction(nameof(QuanLyCoupon));
        }

        // ✅ Ẩn/Hiện coupon (toggle IsActive)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AnHienCoupon(int id)
        {
            try
            {
                var coupon = _db.Coupons.Find(id);
                if (coupon == null)
                {
                    TempData["Error"] = "Không tìm thấy coupon.";
                    return RedirectToAction(nameof(QuanLyCoupon));
                }

                coupon.IsActive = !coupon.IsActive;
                _db.SaveChanges();

                TempData["Success"] = coupon.IsActive
                    ? $"✅ Đã kích hoạt coupon '{coupon.Code}'!"
                    : $"🔒 Đã vô hiệu hóa coupon '{coupon.Code}'!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi thay đổi trạng thái: {ex.Message}";
            }

            return RedirectToAction(nameof(QuanLyCoupon));
        }

        // ✅ Xem lịch sử sử dụng coupon
        public IActionResult LichSuCoupon()
        {
            var history = _db.CouponHistories
                .OrderByDescending(h => h.UsedDate)
                .ToList();

            // Join với KhachHang để lấy thông tin
            var historyWithCustomer = history.Select(h => new
            {
                History = h,
                Customer = _db.KhachHangs.FirstOrDefault(k => k.MaKh == h.CustomerId)
            }).ToList();

            ViewBag.HistoryWithCustomer = historyWithCustomer;

            return View(history);
        }

        // ✅ Xem chi tiết sử dụng coupon (chỉ bảng)
        public IActionResult ChiTietSuDungCoupon()
        {
            var history = _db.CouponHistories
                .OrderByDescending(h => h.UsedDate)
                .ToList();

            // Debug: Log số lượng records
            Console.WriteLine($"DEBUG: Found {history.Count} coupon history records");
            foreach (var h in history.Take(5))
            {
                Console.WriteLine($"  - ID: {h.Id}, Code: {h.CouponCode}, OrderId: {h.OrderId}, CustomerId: {h.CustomerId}");
            }

            // Join với KhachHang để lấy thông tin
            var historyWithCustomer = history.Select(h => new
            {
                History = h,
                Customer = _db.KhachHangs.FirstOrDefault(k => k.MaKh == h.CustomerId)
            }).ToList();

            ViewBag.HistoryWithCustomer = historyWithCustomer;

            return View(history);
        }

        // ====================================================================
        // ✅ QUẢN LÝ NHÀ CUNG CẤP
        // ====================================================================

        // ✅ Xem danh sách nhà cung cấp
        public IActionResult QuanLyNhaCungCap()
        {
            var suppliers = _db.NhaCungCaps
                .OrderBy(n => n.TenCongTy)
                .ToList();

            // Tính số sản phẩm cho mỗi nhà cung cấp
            var productCounts = new Dictionary<string, int>();
            foreach (var supplier in suppliers)
            {
                var count = _db.HangHoas.Count(h => h.MaNcc == supplier.MaNcc);
                productCounts[supplier.MaNcc] = count;
            }

            ViewBag.ProductCounts = productCounts;

            return View(suppliers);
        }

        // ✅ Form thêm nhà cung cấp (GET)
        [HttpGet]
        public IActionResult ThemNhaCungCap()
        {
            return View(new NhaCungCap());
        }

        // ✅ Xử lý thêm nhà cung cấp (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemNhaCungCap(NhaCungCap ncc)
        {
            // Validate unique MaNcc
            if (_db.NhaCungCaps.Any(n => n.MaNcc == ncc.MaNcc))
            {
                ModelState.AddModelError("MaNcc", "Mã nhà cung cấp này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(ncc);
            }

            try
            {
                _db.NhaCungCaps.Add(ncc);
                _db.SaveChanges();

                TempData["Success"] = $"✅ Đã thêm nhà cung cấp '{ncc.TenCongTy}' thành công!";
                return RedirectToAction(nameof(QuanLyNhaCungCap));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi thêm nhà cung cấp: {ex.Message}";
                return View(ncc);
            }
        }

        // ✅ Xóa nhà cung cấp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaNhaCungCap(string id)
        {
            try
            {
                var ncc = _db.NhaCungCaps.Find(id);
                if (ncc == null)
                {
                    TempData["Error"] = "Không tìm thấy nhà cung cấp cần xóa.";
                    return RedirectToAction(nameof(QuanLyNhaCungCap));
                }

                // Check if supplier has products
                var hasProducts = _db.HangHoas.Any(h => h.MaNcc == id);
                if (hasProducts)
                {
                    TempData["Error"] = $"⚠️ Không thể xóa nhà cung cấp '{ncc.TenCongTy}' vì đã có sản phẩm liên kết.";
                    return RedirectToAction(nameof(QuanLyNhaCungCap));
                }

                _db.NhaCungCaps.Remove(ncc);
                _db.SaveChanges();

                TempData["Success"] = $"🗑️ Đã xóa nhà cung cấp '{ncc.TenCongTy}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi khi xóa nhà cung cấp: {ex.Message}";
            }

            return RedirectToAction(nameof(QuanLyNhaCungCap));
        }
    }
}
