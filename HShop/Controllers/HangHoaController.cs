using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HShop.Data;
using HShop.ViewModels;

namespace HShop.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly Hshop2023Context db;

        public HangHoaController(Hshop2023Context context)
        {
            db = context;
        }

        
        public IActionResult Index(int? loai, int page = 1, decimal? minPrice = null, decimal? maxPrice = null, string? filter = null)
        {
            int pageSize = 10; // Mỗi trang hiển thị 10 sản phẩm
            var hangHoas = db.HangHoas.AsQueryable();

            
            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
            }

            
            if (minPrice.HasValue)
            {
                double min = (double)minPrice.Value;
                hangHoas = hangHoas.Where(p => (p.DonGia ?? 0) >= min);
            }
            if (maxPrice.HasValue)
            {
                double max = (double)maxPrice.Value;
                hangHoas = hangHoas.Where(p => (p.DonGia ?? 0) <= max);
            }

            
            if (!string.IsNullOrEmpty(filter))
            {
                switch (filter.ToLower())
                {
                    case "sales":
                        hangHoas = hangHoas.Where(p => (p.DonGia ?? 0) < 200);
                        break;
                    case "discount":
                        hangHoas = hangHoas.Where(p => (p.DonGia ?? 0) < 100);
                        break;
                    case "fresh":
                        hangHoas = hangHoas.Where(p => p.MoTa != null && p.MoTa.Contains("fresh"));
                        break;
                    case "organic":
                        hangHoas = hangHoas.Where(p => p.MoTa != null && p.MoTa.Contains("organic"));
                        break;
                    case "expired":
                        hangHoas = hangHoas.Where(p => (p.DonGia ?? 0) > 500);
                        break;
                }
            }

            
            int totalItems = hangHoas.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var result = hangHoas
                .Include(p => p.MaLoaiNavigation)
                .OrderBy(p => p.MaHh)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHH = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "",
                    MoTaNgan = p.MoTaDonVi ?? "",
                    TenLoai = p.MaLoaiNavigation.TenLoai
                })
                .ToList();

            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Loai = loai;
            ViewBag.MinPrice = minPrice ?? 0;
            ViewBag.MaxPrice = maxPrice ?? 1000;
            ViewBag.Filter = filter;

            return View(result);
        }

        
        public IActionResult Search(string? query, int page = 1)
        {
            int pageSize = 10;
            var hangHoas = db.HangHoas.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
            }

            int totalItems = hangHoas.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var result = hangHoas
                .Include(p => p.MaLoaiNavigation)
                .OrderBy(p => p.MaHh)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHH = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "",
                    MoTaNgan = p.MoTaDonVi ?? "",
                    TenLoai = p.MaLoaiNavigation.TenLoai
                })
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Query = query;

            return View(result);
        }

       
        public IActionResult Detail(int id)
        {
            var data = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .Include(p => p.MaNccNavigation)
                .SingleOrDefault(p => p.MaHh == id);

            if (data == null)
            {
                TempData["Message"] = $"Không thấy sản phẩm có mã {id}";
                return Redirect("/404");
            }

            var result = new ChiTietHangHoaVM
            {
                MaHh = data.MaHh,
                TenHH = data.TenHh,
                DonGia = data.DonGia ?? 0,
                ChiTiet = data.MoTa ?? string.Empty,
                Hinh = data.Hinh ?? string.Empty,
                MoTaNgan = data.MoTaDonVi ?? string.Empty,
                TenLoai = data.MaLoaiNavigation.TenLoai,
                SoLuongTon = 10,
                DiemDanhGia = 5
            };

            
            var related = db.HangHoas
                .Where(p => p.MaLoai == data.MaLoai && p.MaHh != id)
                .Take(4)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHH = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "noimage.jpg"
                })
                .ToList();

            ViewBag.RelatedProducts = related;

           
            var featured = db.HangHoas
                .OrderByDescending(p => p.DonGia) // hoặc đổi thành theo lượt xem, ngày tạo, v.v.
                .Take(5)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHH = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "noimage.jpg"
                })
                .ToList();

            ViewBag.FeaturedProducts = featured;

            return View(result);
        }

     
        public IActionResult Create()
        {
            ViewData["MaLoai"] = new SelectList(db.Loais, "MaLoai", "TenLoai");
            ViewData["MaNcc"] = new SelectList(db.NhaCungCaps, "MaNcc", "TenCongTy");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HangHoa hangHoa, IFormFile HinhFile)
        {
            if (ModelState.IsValid)
            {
                if (HinhFile != null && HinhFile.Length > 0)
                {
                    var fileName = Path.GetFileName(HinhFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Hinh/HangHoa", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhFile.CopyToAsync(stream);
                    }
                    hangHoa.Hinh = fileName;
                }

                db.Add(hangHoa);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaLoai"] = new SelectList(db.Loais, "MaLoai", "TenLoai", hangHoa.MaLoai);
            ViewData["MaNcc"] = new SelectList(db.NhaCungCaps, "MaNcc", "TenCongTy", hangHoa.MaNcc);
            return View(hangHoa);
        }
    }
}
