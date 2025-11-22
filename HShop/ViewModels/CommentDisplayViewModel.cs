using System;

namespace HShop.ViewModels
{
    public class CommentDisplayViewModel
    {
        public int CommentId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAvatar { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; }
        public string ImagePath { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
