using System;

namespace HShop.Data;

public partial class CouponHistory
{
    public int Id { get; set; }

    public string CustomerId { get; set; } = null!;

    public string CouponCode { get; set; } = null!;

    public DateTime UsedDate { get; set; }

    public int? OrderId { get; set; }
  
}
