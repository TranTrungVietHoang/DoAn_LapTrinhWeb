using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HShop.Data;

public partial class HangHoa
{
    [Key]
    public int MaHh { get; set; }

    [Required(ErrorMessage = "Tên hàng hóa là bắt buộc.")]
    [Display(Name = "Tên hàng hóa")]
    public string TenHh { get; set; } = null!;

    [Display(Name = "Tên rút gọn (Alias)")]
    public string? TenAlias { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại hàng")]
    [Display(Name = "Loại hàng")]
    public int? MaLoai { get; set; }

    [Display(Name = "Mô tả đơn vị")]
    public string? MoTaDonVi { get; set; }

    [Display(Name = "Đơn giá")]
    [DataType(DataType.Currency)]
    public double? DonGia { get; set; }

    [Display(Name = "Hình ảnh")]
    public string? Hinh { get; set; }

    [Display(Name = "Ngày sản xuất")]
    [DataType(DataType.Date)]
    public DateTime NgaySx { get; set; }

    [Display(Name = "Giảm giá (%)")]
    [Range(0, 1, ErrorMessage = "Giảm giá phải từ 0 đến 1 (0% đến 100%)")]
    public double GiamGia { get; set; }

    [Display(Name = "Số lần xem")]
    public int SoLanXem { get; set; }

    [Display(Name = "Mô tả chi tiết")]
    public string? MoTa { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
    [Display(Name = "Nhà cung cấp")]
    public string? MaNcc { get; set; }

    public virtual Loai? MaLoaiNavigation { get; set; }
    public virtual NhaCungCap? MaNccNavigation { get; set; }

    public virtual ICollection<BanBe> BanBes { get; set; } = new List<BanBe>();
    public virtual ICollection<ChiTietHd> ChiTietHds { get; set; } = new List<ChiTietHd>();
    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

}