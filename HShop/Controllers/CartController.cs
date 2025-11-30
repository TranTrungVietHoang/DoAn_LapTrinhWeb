using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HShop.Data;
using HShop.ViewModels;
using HShop.Helpers;
using HShop.Services;

namespace ECommerceMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly PaypalClient _paypalClient;
        private readonly Hshop2023Context db;
        private readonly IVnPayService _vnPayservice;

        public CartController(Hshop2023Context context, PaypalClient paypalClient, IVnPayService vnPayservice)
        {
            _paypalClient = paypalClient;
            db = context;
            _vnPayservice = vnPayservice;
        }

        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();


        // =============================================
        // CART INDEX
        // =============================================
        public IActionResult Index()
        {
            var cart = Cart;

            var discount = HttpContext.Session.Get<double?>("CouponDiscount") ?? 0;
            var finalTotal = HttpContext.Session.Get<double?>("FinalTotal")
                             ?? cart.Sum(p => p.ThanhTien);

            ViewBag.Coupons = db.Coupons
                .Where(c => c.IsActive && (c.ExpiryDate == null || c.ExpiryDate > DateTime.Now))
                .OrderBy(c => c.Priority)
                .ToList();

            ViewBag.Discount = discount;
            ViewBag.FinalTotal = finalTotal;

            return View(cart);
        }


        // =============================================
        // APPLY COUPON
        // =============================================
        [HttpPost]
        public IActionResult ApplyCoupon(int couponId)
        {
            var coupon = db.Coupons.Find(couponId);
            if (coupon == null || !coupon.IsActive)
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ" });

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate < DateTime.Now)
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn" });

            var cart = Cart;
            double totalAmount = cart.Sum(p => p.ThanhTien);
            int totalQty = cart.Sum(p => p.SoLuong);
            string? customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            // Validate conditions
            if (coupon.MinQuantity.HasValue && totalQty < coupon.MinQuantity.Value)
                return Json(new { success = false, message = $"Bạn phải mua ít nhất {coupon.MinQuantity.Value} sản phẩm" });

            if (coupon.MinOrderAmount.HasValue && totalAmount < (double)coupon.MinOrderAmount.Value)
                return Json(new { success = false, message = $"Đơn hàng phải từ ${coupon.MinOrderAmount.Value}" });

            if (coupon.OnlyForFirstOrder == true)
            {
                if (customerId == null)
                    return Json(new { success = false, message = "Bạn cần đăng nhập" });

                if (db.HoaDons.Any(h => h.MaKh == customerId))
                    return Json(new { success = false, message = "Mã này chỉ dành cho đơn đầu tiên" });
            }

            if (coupon.Code == "WELCOME10")
            {
                if (customerId == null)
                    return Json(new { success = false, message = "Bạn cần đăng nhập" });

                if (db.HoaDons.Any(h => h.MaKh == customerId))
                    return Json(new { success = false, message = "Mã này chỉ dành cho khách hàng mới" });

                if (db.CouponHistories.Any(c => c.CustomerId == customerId && c.CouponCode == coupon.Code))
                    return Json(new { success = false, message = "Bạn đã dùng mã này rồi" });
            }

            if (coupon.PerUserLimit.HasValue && customerId != null)
            {
                var used = db.CouponHistories.Count(c => c.CustomerId == customerId && c.CouponCode == coupon.Code);
                if (used >= coupon.PerUserLimit.Value)
                    return Json(new { success = false, message = "Bạn đã dùng hết số lượt" });
            }

            if (coupon.UsageLimit.HasValue)
            {
                var used = db.CouponHistories.Count(c => c.CouponCode == coupon.Code);
                if (used >= coupon.UsageLimit.Value)
                    return Json(new { success = false, message = "Mã đã hết lượt sử dụng" });
            }

            // Tính giảm giá
            double discount = 0;
            if (coupon.DiscountPercent > 0)
            {
                discount = totalAmount * ((double)coupon.DiscountPercent / 100);

                if (coupon.MaxDiscount.HasValue && discount > (double)coupon.MaxDiscount.Value)
                    discount = (double)coupon.MaxDiscount.Value;
            }
            else if (coupon.DiscountAmount.HasValue)
            {
                discount = (double)coupon.DiscountAmount.Value;
            }

            HttpContext.Session.Set("Coupon", coupon);
            HttpContext.Session.Set("CouponDiscount", discount);
            HttpContext.Session.Set("FinalTotal", totalAmount - discount);

            return Json(new { success = true, discount = discount, newTotal = totalAmount - discount });
        }


        // =============================================
        // ADD TO CART
        // =============================================
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.MaHh == id);

            if (item == null)
            {
                var product = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
                if (product == null)
                    return Redirect("/404");

                cart.Add(new CartItem
                {
                    MaHh = product.MaHh,
                    TenHH = product.TenHh,
                    DonGia = product.DonGia ?? 0,
                    Hinh = product.Hinh ?? "",
                    SoLuong = quantity
                });
            }
            else
            {
                item.SoLuong += quantity;
            }

            HttpContext.Session.Set(MySetting.CART_KEY, cart);
            return RedirectToAction("Index");
        }


        // =============================================
        // REMOVE ITEM
        // =============================================
        public IActionResult RemoveCart(int id)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.MaHh == id);

            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.Set(MySetting.CART_KEY, cart);
            }

            if (!cart.Any())
            {
                HttpContext.Session.Remove("Coupon");
                HttpContext.Session.Remove("CouponDiscount");
                HttpContext.Session.Remove("FinalTotal");
            }

            return RedirectToAction("Index");
        }



        [Authorize]
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.MaHh == id);

            if (item != null)
            {
                item.SoLuong = quantity;
                HttpContext.Session.Set(MySetting.CART_KEY, cart);
            }

            double subtotal = cart.Sum(p => p.ThanhTien);
            var coupon = HttpContext.Session.Get<Coupon>("Coupon");

            double discount = 0;
            bool valid = true;

            if (coupon != null)
            {
                if (coupon.MinOrderAmount.HasValue && subtotal < (double)coupon.MinOrderAmount.Value)
                    valid = false;

                if (coupon.MinQuantity.HasValue && cart.Sum(x => x.SoLuong) < coupon.MinQuantity.Value)
                    valid = false;

                if (valid)
                {
                    if (coupon.DiscountPercent > 0)
                    {
                        discount = subtotal * ((double)coupon.DiscountPercent / 100);
                        if (coupon.MaxDiscount.HasValue && discount > (double)coupon.MaxDiscount.Value)
                            discount = (double)coupon.MaxDiscount.Value;
                    }
                    else if (coupon.DiscountAmount.HasValue)
                    {
                        discount = (double)coupon.DiscountAmount.Value;
                    }

                    HttpContext.Session.Set("CouponDiscount", discount);
                }
                else
                {
                    HttpContext.Session.Remove("Coupon");
                    HttpContext.Session.Remove("CouponDiscount");
                }
            }

            double finalTotal = subtotal - discount;

            return Json(new
            {
                subtotal,
                discount,
                finalTotal,
                couponRemoved = !valid
            });
        }


       
        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = Cart;

            double discount = HttpContext.Session.Get<double>("CouponDiscount");
            double subtotal = cart.Sum(p => p.ThanhTien);
            double final = subtotal - discount;

            if (final < 0) final = 0;

            ViewBag.Discount = discount;
            ViewBag.FinalTotal = final;
            ViewBag.PaypalClientdId = _paypalClient.ClientId;

            return View(cart);
        }


        // =============================================
        // CHECKOUT SUBMIT (COD + VNPAY)
        // =============================================
        [HttpPost]
        public IActionResult Checkout(CheckoutVM model, string payment = "COD")
        {
            if (model.GiongKhachHang)
            {
                ModelState.Remove("HoTen");
                ModelState.Remove("DiaChi");
                ModelState.Remove("DienThoai");
            }
            else
            {
                // Validate manually when NOT using customer info
                if (string.IsNullOrWhiteSpace(model.HoTen))
                    ModelState.AddModelError("HoTen", "Vui lòng nhập tên người nhận");
                
                if (string.IsNullOrWhiteSpace(model.DiaChi))
                    ModelState.AddModelError("DiaChi", "Vui lòng nhập địa chỉ nhận hàng");
                
                if (string.IsNullOrWhiteSpace(model.DienThoai))
                    ModelState.AddModelError("DienThoai", "Vui lòng nhập số điện thoại");
            }

        
            if (!ModelState.IsValid)
            {
                var cart = Cart;

                double discount = HttpContext.Session.Get<double>("CouponDiscount");
                double subtotal = cart.Sum(p => p.ThanhTien);
                double final = subtotal - discount;

                if (final < 0) final = 0;

                ViewBag.Discount = discount;
                ViewBag.FinalTotal = final;
                ViewBag.PaypalClientdId = _paypalClient.ClientId;

                return View(cart);
            }

            var coupon = HttpContext.Session.Get<Coupon>("Coupon");
            double couponDiscount = HttpContext.Session.Get<double>("CouponDiscount");
            double subtotal2 = Cart.Sum(p => p.ThanhTien);
            double finalTotal = subtotal2 - couponDiscount;
            if (finalTotal < 0) finalTotal = 0;

            // ===================== VNPAY =======================
            if (payment == "Thanh toán VNPay")
            {
                var vnPayModel = new VnPaymentRequestModel
                {
                    Amount = finalTotal,
                    CreatedDate = DateTime.Now,
                    Description = $"{model.HoTen} {model.DienThoai}",
                    FullName = model.HoTen,
                    OrderId = new Random().Next(1000, 100000)
                };
                return Redirect(_vnPayservice.CreatePaymentUrl(HttpContext, vnPayModel));
            }


            // ===================== LẤY KHÁCH HÀNG =======================
            var customerId = HttpContext.User.Claims
                .SingleOrDefault(x => x.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            var khachHang = model.GiongKhachHang
                ? db.KhachHangs.Single(kh => kh.MaKh == customerId)
                : new KhachHang();


            // ===================== TẠO HOÁ ĐƠN =======================
            var hoadon = new HoaDon
            {
                MaKh = customerId,
                HoTen = model.HoTen ?? khachHang.HoTen,
                DiaChi = model.DiaChi ?? khachHang.DiaChi,
                DienThoai = model.DienThoai ?? khachHang.DienThoai,
                NgayDat = DateTime.Now,
                CachThanhToan = "COD",
                CachVanChuyen = "GRAB",
                MaTrangThai = 0,
                GhiChu = model.GhiChu,
                GiamGia = (decimal)couponDiscount
            };


            db.Database.BeginTransaction();

            try
            {
                db.Add(hoadon);
                db.SaveChanges();

                double tongTien = Cart.Sum(x => x.ThanhTien);   // Tổng tiền trước giảm
                double giamGiaTong = couponDiscount;            // Tổng giảm giá

                var list = new List<ChiTietHd>();

                foreach (var item in Cart)
                {
                    double giaTriSP = item.ThanhTien;

                    // Phân bổ giảm giá theo tỷ lệ
                    double giamGiaSP = Math.Round((giaTriSP / tongTien) * giamGiaTong);

                    if (giamGiaSP > giaTriSP) giamGiaSP = giaTriSP; // tránh âm

                    list.Add(new ChiTietHd
                    {
                        MaHd = hoadon.MaHd,
                        MaHh = item.MaHh,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia,
                        CouponCode = coupon?.Code,
                        DiscountValue = (decimal)giamGiaSP,
                        GiamGia = giamGiaSP
                    });
                }

                db.AddRange(list);

                if (coupon != null)
                {
                    db.CouponHistories.Add(new CouponHistory
                    {
                        CustomerId = customerId,
                        CouponCode = coupon.Code,
                        UsedDate = DateTime.Now,
                        OrderId = hoadon.MaHd
                    });
                }

                db.SaveChanges();
                db.Database.CommitTransaction();


                HttpContext.Session.Remove(MySetting.CART_KEY);
                HttpContext.Session.Remove("Coupon");
                HttpContext.Session.Remove("CouponDiscount");

                return View("Success");
            }
            catch (Exception ex)
            {
                db.Database.RollbackTransaction();
                
                // Log error to debug
                ModelState.AddModelError("", $"Lỗi khi đặt hàng: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Chi tiết: {ex.InnerException.Message}");
                }

                var cart = Cart;
                double discount = HttpContext.Session.Get<double>("CouponDiscount");
                double subtotal = cart.Sum(p => p.ThanhTien);
                double final = subtotal - discount;
                if (final < 0) final = 0;

                ViewBag.Discount = discount;
                ViewBag.FinalTotal = final;
                ViewBag.PaypalClientdId = _paypalClient.ClientId;

                return View(cart);
            }
        }



        // ===================== SUCCESS =======================
        [Authorize]
        public IActionResult PaymentSuccess()
        {
            return View("Success");
        }


        // ===================== PAYPAL =======================
        [Authorize]
        [HttpPost("/Cart/create-paypal-order")]
        public async Task<IActionResult> CreatePaypalOrder(CancellationToken ct)
        {
            var amount = Cart.Sum(p => p.ThanhTien).ToString();

            var response = await _paypalClient.CreateOrder(amount, "USD", "HD" + DateTime.Now.Ticks);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("/Cart/capture-paypal-order")]
        public async Task<IActionResult> CapturePaypalOrder(string orderID)
        {
            var response = await _paypalClient.CaptureOrder(orderID);

            string errorMessage;
            SaveOrder("PayPal", "Thanh toán bằng PayPal", out errorMessage);

            return Ok(response);
        }


        // ===================== VNPAY CALLBACK =======================
        [Authorize]
        public IActionResult PaymentCallBack()
        {
            var response = _vnPayservice.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
                return RedirectToAction("PaymentFail");

            string errorMessage;
            SaveOrder("VNPay", "Thanh toán qua VNPay", out errorMessage);

            return RedirectToAction("PaymentSuccess");
        }


        // ===================== LƯU HOÁ ĐƠN CHUNG =======================
        private bool SaveOrder(string method, string note, out string error)
        {
            error = string.Empty;

            try
            {
                var customerId = HttpContext.User.Claims
                    .Single(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;

                var kh = db.KhachHangs.Single(k => k.MaKh == customerId);

                var coupon = HttpContext.Session.Get<Coupon>("Coupon");
                double discountValue = HttpContext.Session.Get<double>("CouponDiscount");

                var hd = new HoaDon
                {
                    MaKh = customerId,
                    HoTen = kh.HoTen,
                    DiaChi = kh.DiaChi,
                    DienThoai = kh.DienThoai,
                    NgayDat = DateTime.Now,
                    CachThanhToan = method,
                    CachVanChuyen = "GRAB",
                    MaTrangThai = 0,
                    GhiChu = note,
                    GiamGia = (decimal)discountValue
                };

                db.Database.BeginTransaction();
                db.HoaDons.Add(hd);
                db.SaveChanges();

                double tongTien = Cart.Sum(x => x.ThanhTien);
                double giamGiaTong = discountValue;

                foreach (var item in Cart)
                {
                    double giaTriSP = item.ThanhTien;

                    double giamGiaSP = Math.Round((giaTriSP / tongTien) * giamGiaTong);
                    if (giamGiaSP > giaTriSP) giamGiaSP = giaTriSP;

                    db.ChiTietHds.Add(new ChiTietHd
                    {
                        MaHd = hd.MaHd,
                        MaHh = item.MaHh,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia,
                        CouponCode = coupon?.Code,
                        DiscountValue = (decimal)giamGiaSP,
                        GiamGia = giamGiaSP
                    });
                }

                if (coupon != null)
                {
                    db.CouponHistories.Add(new CouponHistory
                    {
                        CustomerId = customerId,
                        CouponCode = coupon.Code,
                        UsedDate = DateTime.Now,
                        OrderId = hd.MaHd
                    });
                }

                db.SaveChanges();
                db.Database.CommitTransaction();

                HttpContext.Session.Remove(MySetting.CART_KEY);
                HttpContext.Session.Remove("Coupon");
                HttpContext.Session.Remove("CouponDiscount");

                return true;
            }
            catch (Exception ex)
            {
                db.Database.RollbackTransaction();
                error = ex.Message;
                return false;
            }
        }
    }
}
