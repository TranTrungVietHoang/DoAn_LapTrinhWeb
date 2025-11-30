USE [HShop2025]
GO

-- Update BUY2GET5: Ensure MinQuantity is 2 for any products
IF EXISTS (SELECT 1 FROM Coupon WHERE Code = 'BUY2GET5')
BEGIN
    UPDATE Coupon 
    SET MinQuantity = 2, 
        MinOrderAmount = 0, 
        DiscountAmount = 5, 
        DiscountPercent = 0,
        Description = N'Mua ít nhất 2 sản phẩm bất kỳ giảm 5$'
    WHERE Code = 'BUY2GET5';
END
ELSE
BEGIN
    INSERT INTO Coupon (Code, Description, DiscountPercent, DiscountAmount, MinOrderAmount, MinQuantity, IsActive, Priority)
    VALUES ('BUY2GET5', N'Mua ít nhất 2 sản phẩm bất kỳ giảm 5$', 0, 5, 0, 2, 1, 1);
END

-- Update FIRSTORDER50: Ensure 50% discount with max 20 USD for first order only
IF EXISTS ( SELECT 1 FROM Coupon WHERE Code = 'FIRSTORDER50')
BEGIN
    UPDATE Coupon 
    SET DiscountPercent = 50, 
        MinOrderAmount = 0, 
        OnlyForFirstOrder = 1,
        MaxDiscount = 20,
        Description = N'Giảm 50% tối đa 20 USD cho đơn hàng đầu tiên'
    WHERE Code = 'FIRSTORDER50';
END
ELSE
BEGIN
    INSERT INTO Coupon (Code, Description, DiscountPercent, MinOrderAmount, OnlyForFirstOrder, MaxDiscount, IsActive, Priority)
    VALUES ('FIRSTORDER50', N'Giảm 50% tối đa 20 USD cho đơn hàng đầu tiên', 50, 0, 1, 20, 1, 2);
END

-- Update WELCOME10: Ensure 10% discount for new customers only, one-time use
IF EXISTS (SELECT 1 FROM Coupon WHERE Code = 'WELCOME10')
BEGIN
    UPDATE Coupon 
    SET DiscountPercent = 10, 
        MinOrderAmount = 0,
        PerUserLimit = 1,
        Description = N'Giảm 10% cho khách hàng mới, chỉ sử dụng 1 lần'
    WHERE Code = 'WELCOME10';
END
ELSE
BEGIN
    INSERT INTO Coupon (Code, Description, DiscountPercent, MinOrderAmount, PerUserLimit, IsActive, Priority)
    VALUES ('WELCOME10', N'Giảm 10% cho khách hàng mới, chỉ sử dụng 1 lần', 10, 0, 1, 1, 3);
END

-- Update VOUCHER15: Ensure minimum order amount of 50 USD
IF EXISTS (SELECT 1 FROM Coupon WHERE Code = 'VOUCHER15')
BEGIN
    UPDATE Coupon 
    SET DiscountPercent = 15, 
        MinOrderAmount = 50,
        Description = N'Giảm 15% cho đơn hàng từ 50 USD trở lên'
    WHERE Code = 'VOUCHER15';
END
ELSE
BEGIN
    INSERT INTO Coupon (Code, Description, DiscountPercent, MinOrderAmount, IsActive, Priority)
    VALUES ('VOUCHER15', N'Giảm 15% cho đơn hàng từ 50 USD trở lên', 15, 50, 1, 4);
END

GO
