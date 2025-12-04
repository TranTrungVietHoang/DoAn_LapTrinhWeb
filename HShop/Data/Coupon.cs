using System;
using System.Collections.Generic;

namespace HShop.Data;

public partial class Coupon
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public decimal DiscountPercent { get; set; }

    public decimal? DiscountAmount { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public int? MinQuantity { get; set; }

    public bool OnlyForFirstOrder { get; set; }

    public int? RequiredProductId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? UsageLimit { get; set; }

    public int? PerUserLimit { get; set; }

    public decimal? MaxDiscount { get; set; }

    public string CouponType { get; set; } = "order";

    public int Priority { get; set; }

}
