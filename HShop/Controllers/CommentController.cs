using HShop.Data;
using HShop.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace HShop.Controllers
{
    public class CommentController : Controller
    {
        private readonly Hshop2023Context _context;
        private readonly IWebHostEnvironment _env;

        public CommentController(Hshop2023Context context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        [Authorize] // bắt buộc đăng nhập
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Detail", "HangHoa", new { id = model.MaHH });
            }

            string MaKH = User.Identity.Name; // dự án của bạn dùng MaKH làm username

            // 1. Kiểm tra khách đã mua hàng chưa
            int soLanMua = await _context.ChiTietHds
                .Where(x => x.MaHh == model.MaHH && x.MaHdNavigation.MaKh == MaKH)
                .CountAsync();

            if (soLanMua == 0)
            {
                TempData["ErrorMessage"] = "Bạn phải mua sản phẩm trước khi đánh giá.";
                return RedirectToAction("Detail", "HangHoa", new { id = model.MaHH });
            }

            // 2. Kiểm tra số lần đánh giá đã vượt giới hạn chưa
            int soLanDanhGia = await _context.Comments
                .Where(x => x.MaHH == model.MaHH && x.MaKH == MaKH)
                .CountAsync();

            if (soLanDanhGia >= soLanMua)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá đủ số lần cho phép.";
                return RedirectToAction("Detail", "HangHoa", new { id = model.MaHH });
            }

            // 3. Validate rating
            if (model.Rating < 1 || model.Rating > 5)
            {
                TempData["ErrorMessage"] = "Đánh giá phải từ 1 đến 5 sao.";
                return RedirectToAction("Detail", "HangHoa", new { id = model.MaHH });
            }

            // 4. Lưu hình ảnh
            string imagePath = null;

            if (model.ImageUpload != null)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(model.ImageUpload.FileName).ToLower();
                
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Chỉ chấp nhận file ảnh định dạng JPG, JPEG, PNG, GIF.";
                    return RedirectToAction("Detail", "HangHoa", new { id = model.MaHH });
                }

                // Validate file size (max 5MB)
                if (model.ImageUpload.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Kích thước file ảnh không được vượt quá 5MB.";
                    return RedirectToAction("Detail", "HangHoa", new { id = model.MaHH });
                }

                try
                {
                    string folder = Path.Combine(_env.WebRootPath, "Hinh", "Comments");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid() + extension;
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageUpload.CopyToAsync(stream);
                    }

                    imagePath = $"/Hinh/Comments/{fileName}";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi khi upload hình ảnh. Vui lòng thử lại.";
                    return RedirectToAction("Detail", "HangHoa", new { id = model.MaHH });
                }
            }

            // 5. Lưu vào DB
            try
            {
                var cmt = new Comment
                {
                    MaHH = model.MaHH,
                    MaKH = MaKH,
                    Rating = model.Rating,
                    Content = model.Content ?? string.Empty,
                    ImagePath = imagePath,
                    CreatedDate = DateTime.Now
                };

                _context.Comments.Add(cmt);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi khi lưu đánh giá. Vui lòng thử lại.";
            }

            return RedirectToAction("Detail", "HangHoa", new { id = model.MaHH });
        }
    }
}
