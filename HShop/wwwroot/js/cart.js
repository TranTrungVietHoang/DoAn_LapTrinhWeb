// Cart functionality
$(document).ready(function () {
    // Unbind any existing events first to prevent double-binding
    $('.btn-plus').off('click');
    $('.btn-minus').off('click');
    $('.quantity-input').off('change');

    // Handle quantity increase
    $('.btn-plus').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var $input = $(this).closest('.quantity').find('.quantity-input');
        var currentVal = parseInt($input.val()) || 0;
        var newVal = currentVal + 1;
        $input.val(newVal);

        // Update UI immediately
        updateItemTotal($input);
        updateCartTotals();

        // Update server in background
        var productId = $input.data('id');
        updateQuantityOnServer(productId, newVal);
        return false;
    });

    // Handle quantity decrease
    $('.btn-minus').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var $input = $(this).closest('.quantity').find('.quantity-input');
        var currentVal = parseInt($input.val()) || 0;
        if (currentVal > 1) {
            var newVal = currentVal - 1;
            $input.val(newVal);

            // Update UI immediately
            updateItemTotal($input);
            updateCartTotals();

            // Update server in background
            var productId = $input.data('id');
            updateQuantityOnServer(productId, newVal);
        }
        return false;
    });

    // Function to update individual item total
    function updateItemTotal($input) {
        var quantity = parseInt($input.val()) || 1;
        var price = parseFloat($input.data('price')) || 0;
        var productId = $input.data('id');
        var total = quantity * price;

        $('.item-total[data-product-id="' + productId + '"]').text('$' + total.toFixed(2));
    }

    // Function to update quantity on server (in background)
    function updateQuantityOnServer(productId, quantity) {
        $.post('/Cart/UpdateQuantity',
            { id: productId, quantity: quantity },
            function (response) {
                // Success - no need to reload, UI already updated
            }
        ).fail(function () {
            alert('Lỗi khi cập nhật số lượng');
            location.reload(); // Reload on error to restore correct state
        });
    }

    // Update total when quantity changes manually
    $('.quantity-input').on('change', function () {
        updateItemTotal($(this));
        updateCartTotals();

        // Update server
        var productId = $(this).data('id');
        var quantity = parseInt($(this).val()) || 1;
        updateQuantityOnServer(productId, quantity);
    });

    // Function to update cart totals
    function updateCartTotals() {
        var cartSubtotal = 0;
        $('.quantity-input').each(function () {
            var qty = parseInt($(this).val()) || 1;
            var prc = parseFloat($(this).data('price')) || 0;
            cartSubtotal += qty * prc;
        });

        $('#cart-subtotal').text('$' + cartSubtotal.toFixed(2));

        // Get current discount amount
        var discountText = $('#cart-discount').text().replace('-', '').replace('$', '').replace(/\s/g, '');
        var currentDiscount = parseFloat(discountText) || 0;

        // Recalculate total with discount
        var finalTotal = cartSubtotal - currentDiscount;
        if (finalTotal < 0) finalTotal = 0;
        $('#cart-total').text('$' + finalTotal.toFixed(2));
    }
});
