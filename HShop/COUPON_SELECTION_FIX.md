# Fix Missing Coupon Selection in Cart

## Vấn đề
Phần chọn mã giảm giá không hiện ra trong trang giỏ hàng vì method `Index()` trong CartController không load danh sách coupon vào ViewBag.

## Giải pháp

Thay thế method `Index()` trong file `CartController.cs` (khoảng dòng 25-28) từ:

```csharp
public IActionResult Index()
{
    return View(Cart);
}
```

Thành:

```csharp
public IActionResult Index()
{
    // Load active coupons for dropdown
    var activeCoupons = db.Coupons
        .Where(c => c.IsActive && (c.ExpiryDate == null || c.ExpiryDate > DateTime.Now))
        .OrderBy(c => c.Priority)
        .ToList();
    ViewBag.Coupons = activeCoupons;

    return View(Cart);
}
```

## Thêm method ApplyCoupon

Sau method `Index()`, thêm method `ApplyCoupon` này vào CartController (xem file walkthrough.md để có code đầy đủ của method ApplyCoupon với tất cả validation logic).

Sau khi cập nhật xong, phần chọn coupon sẽ hiển thị trong trang giỏ hàng!
