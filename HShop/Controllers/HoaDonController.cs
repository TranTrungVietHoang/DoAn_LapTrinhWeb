using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HShop.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;//

namespace HShop.Controllers
{
    public class HoaDonController : Controller
    {
        private readonly Hshop2023Context db;

        public HoaDonController(Hshop2023Context context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Detail(int id)
        {
            var hoaDon = db.HoaDons
                .Include(hd => hd.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .Include(hd => hd.MaTrangThaiNavigation)
                .FirstOrDefault(hd => hd.MaHd == id);

            if (hoaDon == null)
                return NotFound();

            return View(hoaDon);
        }
    }
}
