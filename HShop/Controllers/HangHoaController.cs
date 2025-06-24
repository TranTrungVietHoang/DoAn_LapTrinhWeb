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

        public HangHoaController(Hshop2023Context conetxt)
        {
            db = conetxt;
        }

		public IActionResult Index(int? loai)
		{
			var hangHoas = db.HangHoas.AsQueryable();

			if (loai.HasValue)
			{
				hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
			}

			var result = hangHoas.Select(p => new HangHoaVM
			{
				MaHh = p.MaHh,
				TenHH = p.TenHh,
				DonGia = p.DonGia ?? 0,
				Hinh = p.Hinh ?? "",
				MoTaNgan = p.MoTaDonVi ?? "",
				TenLoai = p.MaLoaiNavigation.TenLoai
			}).ToList(); 

			return View(result);
		}

		public IActionResult Search(string? query)
		{
			var hangHoas = db.HangHoas.AsQueryable();

			if (!string.IsNullOrEmpty(query))
			{
				hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
			}

			var result = hangHoas.Select(p => new HangHoaVM
			{
				MaHh = p.MaHh,
				TenHH = p.TenHh,
				DonGia = p.DonGia ?? 0,
				Hinh = p.Hinh ?? "",
				MoTaNgan = p.MoTaDonVi ?? "",
				TenLoai = p.MaLoaiNavigation.TenLoai
			}).ToList();

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
                SoLuongTon = 10, // Giả lập
                DiemDanhGia = 5  // Giả lập
            };
            return View(result);
        }

        // GET: Create
        public IActionResult Create()
        {
            ViewData["MaLoai"] = new SelectList(db.Loais, "MaLoai", "TenLoai");
            ViewData["MaNcc"] = new SelectList(db.NhaCungCaps, "MaNcc", "TenCongTy");
            return View();
        }

        // POST: Create
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
