using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HShop.Data;

public partial class NhaCungCap
{
    [Key]
    [Display(Name = "Mã nhà cung cấp")]
    public string MaNcc { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập tên công ty")]
    [Display(Name = "Tên công ty")]
    public string TenCongTy { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng cung cấp logo")]
    [Display(Name = "Logo công ty")]
    public string Logo { get; set; } = null!;

    [Display(Name = "Người liên lạc")]
    public string? NguoiLienLac { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Display(Name = "Điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? DienThoai { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [Display(Name = "Mô tả thêm")]
    public string? MoTa { get; set; }

    // ====== Navigation properties ======
    [Display(Name = "Danh sách hàng hóa")]
    public virtual ICollection<HangHoa> HangHoas { get; set; } = new List<HangHoa>();
}
