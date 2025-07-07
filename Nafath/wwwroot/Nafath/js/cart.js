// Shopping Cart Module
var shoppingCart = (function () {
    var cart = [];

    function Item(id, name, price, count, imageUrl) {
        this.id = id;
        this.name = name;
        this.price = price;
        this.count = count;
        this.imageUrl = imageUrl;
    }

    function saveCart() {
        localStorage.setItem('shoppingCart', JSON.stringify(cart));
    }

    function loadCart() {
        var storedCart = localStorage.getItem('shoppingCart');
        if (storedCart) {
            cart = JSON.parse(storedCart);
        }
    }

    loadCart();

    return {
        addItemToCart: function (id, name, price, count, imageUrl) {
            for (var i = 0; i < cart.length; i++) {
                if (cart[i].id === id) {
                    cart[i].count++;
                    saveCart();
                    return;
                }
            }
            var item = new Item(id, name, price, count, imageUrl);
            cart.push(item);
            saveCart();
        },

        setCountForItem: function (id, count) {
            for (var i = 0; i < cart.length; i++) {
                if (cart[i].id === id) {
                    cart[i].count = count;
                    break;
                }
            }
            saveCart();
        },

        removeItemFromCart: function (id) {
            for (var i = 0; i < cart.length; i++) {
                if (cart[i].id === id) {
                    cart[i].count--;
                    if (cart[i].count <= 0) {
                        cart.splice(i, 1);
                    }
                    break;
                }
            }
            saveCart();
        },

        removeItemById: function (id) {
            for (var i = 0; i < cart.length; i++) {
                if (cart[i].id === id) {
                    cart.splice(i, 1);
                    break;
                }
            }
            saveCart();
        },

        clearCart: function () {
            cart = [];
            saveCart();
        },

        totalCount: function () {
            return cart.reduce((total, item) => total + item.count, 0);
        },

        totalCart: function () {
            return Number(cart.reduce((total, item) => total + item.price * item.count, 0).toFixed(2));
        },

        listCart: function () {
            return cart.map(item => ({
                id: item.id,
                name: item.name,
                price: item.price,
                count: item.count,
                imageUrl: item.imageUrl,
                total: (item.price * item.count).toFixed(2)
            }));
        }
    };
})();

// Initialize on document ready
$(document).ready(function () {
    $(document).on('click', '.add-to-cart', function (e) {
        e.preventDefault();

        var id = $(this).data('id');
        var name = $(this).data('name');
        var price = Number($(this).data('price'));
        var imageUrl = $(this).data('imageurl');

        shoppingCart.addItemToCart(id, name, price, 1, imageUrl);
        updateCartDisplay();
        $(this).closest('.modal').modal('hide');
        showNotification(name + " added to cart!");
    });

    function updateCartDisplay() {
        var cartItems = shoppingCart.listCart();
        var total = shoppingCart.totalCart();
        var count = shoppingCart.totalCount();

        $('.total-cart').text(total.toFixed(2));
        $('#cart-badge').text(count);

        var itemsHtml = '';
        if (cartItems.length === 0) {
            itemsHtml = '<tr><td colspan="5" class="text-center">Your cart is empty</td></tr>';
        } else {
            cartItems.forEach(function (item) {
                itemsHtml += `
<tr lang="en" dir="ltr">
    <td style="align-content: center;">
        <div class="d-flex align-items-center">
            <div>
                <h6 class="mb-0">${item.name}</h6>
            </div>
            <img src="${item.imageUrl}" alt="${item.name}" class="img-thumbnail ml-5" style="width:60px;height:60px;object-fit:cover;">
        </div>
    </td>
    <td style="align-content: center;" class=" mb-0">${item.price} EGP</td>
    <td style="align-content: center;">
        <div class="input-group" style="max-width: 170px;">
            <div class="input-group-prepend">
                <button class="btn btn-outline-secondary increase-item" type="button" data-id="${item.id}">+</button>
            </div>
            <input type="text" class="form-control text-center item-count" value="${item.count}" data-id="${item.id}">
            <div class="input-group-append">
                <button class="btn btn-outline-secondary decrease-item" type="button" data-id="${item.id}">-</button>
            </div>
        </div>
    </td>
    <td style="align-content: center;">${item.total} EGP</td>
    <td style="align-content: center;">
        <button class="close delete-item" data-id="${item.id}"><span aria-hidden="true">&times;</span></button>
    </td>
</tr>`;
            });
        }

        $('.cart-items').html(itemsHtml);
    }

    $(document).on('click', '.delete-item', function () {
        shoppingCart.removeItemById($(this).data('id'));
        updateCartDisplay();
    });

    $('.clear-cart').click(function () {
        shoppingCart.clearCart();
        updateCartDisplay();
    });

    $(document).on('click', '.increase-item', function () {
        var id = $(this).data('id');
        var count = parseInt($(this).closest('.input-group').find('.item-count').val());
        shoppingCart.setCountForItem(id, count + 1);
        updateCartDisplay();
    });

    $(document).on('click', '.decrease-item', function () {
        var id = $(this).data('id');
        var count = parseInt($(this).closest('.input-group').find('.item-count').val());
        if (count > 1) {
            shoppingCart.setCountForItem(id, count - 1);
            updateCartDisplay();
        }
    });

    $(document).on('change', '.item-count', function () {
        var id = $(this).data('id');
        var count = parseInt($(this).val());
        if (!isNaN(count) && count > 0) {
            shoppingCart.setCountForItem(id, count);
        }
        updateCartDisplay();
    });

    function showNotification(message) {
        var $notification = $('<div class="alert alert-success alert-dismissible fade show" role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 9999;">' +
            message +
            '<button type="button" class="close" data-dismiss="alert" aria-label="Close">' +
            '<span aria-hidden="true">&times;</span>' +
            '</button>' +
            '</div>');

        $('body').append($notification);
        setTimeout(function () {
            $notification.alert('close');
        }, 3000);
    }

    updateCartDisplay();
});
