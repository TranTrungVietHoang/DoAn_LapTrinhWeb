using System;

namespace HShop.Data
{
    public partial class Comment
    {
        public int CommentId { get; set; }
        public int MaHH { get; set; }
        public string MaKH { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; }
        public string ImagePath { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsHidden { get; set; } = false;  // ✅ Cho phép Admin ẩn bình luận

        public virtual HangHoa MaHHNavigation { get; set; }
        public virtual KhachHang MaKHNavigation { get; set; }
    }
}
