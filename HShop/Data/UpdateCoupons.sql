USE [HShop2025]
GO

-- Fix BUY2GET5: MinQuantity = 2
IF EXISTS (SELECT * FROM Coupon WHERE Code = 'BUY2GET5')
BEGIN
    UPDATE Coupon SET MinQuantity = 2, MinOrderAmount = 0, DiscountAmount = 5, DiscountPercent = 0 WHERE Code = 'BUY2GET5';
END
ELSE
BEGIN
    INSERT INTO Coupon (Code, Description, DiscountPercent, DiscountAmount, MinOrderAmount, MinQuantity, IsActive, Priority)
    VALUES ('BUY2GET5', N'Mua 2 sản phẩm giảm 5$', 0, 5, 0, 2, 1, 1);
END

-- Fix FIRSTORDER50: 50% off, First Order Only
IF EXISTS (SELECT * FROM Coupon WHERE Code = 'FIRSTORDER50')
BEGIN
    UPDATE Coupon SET DiscountPercent = 50, MinOrderAmount = 0, OnlyForFirstOrder = 1 WHERE Code = 'FIRSTORDER50';
END
ELSE
BEGIN
    INSERT INTO Coupon (Code, Description, DiscountPercent, MinOrderAmount, OnlyForFirstOrder, IsActive, Priority)
    VALUES ('FIRSTORDER50', N'Giảm 50% cho đơn hàng đầu tiên', 50, 0, 1, 1, 2);
END

-- Fix WELCOME10: 10% off
IF EXISTS (SELECT * FROM Coupon WHERE Code = 'WELCOME10')
BEGIN
    UPDATE Coupon SET DiscountPercent = 10, MinOrderAmount = 0 WHERE Code = 'WELCOME10';
END
ELSE
BEGIN
    INSERT INTO Coupon (Code, Description, DiscountPercent, MinOrderAmount, IsActive, Priority)
    VALUES ('WELCOME10', N'Chào mừng thành viên mới - Giảm 10%', 10, 0, 1, 3);
END
GO
