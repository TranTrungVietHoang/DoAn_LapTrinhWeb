using Microsoft.AspNetCore.Http;

namespace HShop.ViewModels
{
    public class CommentCreateViewModel
    {
        public int MaHH { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; }
        public IFormFile ImageUpload { get; set; }
    }
}
