// script.js
document.addEventListener('DOMContentLoaded', function() {
    // Дані про товари (без фото та іконок)
    const products = [
        {
            id: 1,
            name: "Футболка ХДУ",
            description: "Чоловіча футболка з логотипом університету. 100% бавовна, різні розміри.",
            price: 450,
            category: "Одяг"
        },
        {
            id: 2,
            name: "Світшот з гербом",
            description: "Теплий світшот з гербом Херсонського державного університету. Зручний та стильний.",
            price: 850,
            category: "Одяг"
        },
        {
            id: 3,
            name: "Кепка ХДУ",
            description: "Бейсболка з вишитим логотипом університету. Регульований розмір.",
            price: 320,
            category: "Аксесуари"
        },
        {
            id: 4,
            name: "Рюкзак студента",
            description: "Стильний рюкзак для студентів з символікою ХДУ. Водонепроникний матеріал.",
            price: 1200,
            category: "Аксесуари"
        },
        {
            id: 5,
            name: "Кружка з логотипом",
            description: "Керамічна кружка з логотипом університета, 350 мл. Мікрохвильова піч безпечна.",
            price: 250,
            category: "Канцелярія"
        },
        {
            id: 6,
            name: "Блокнот ХДУ",
            description: "Якісний блокнот в твердій палітурці з символікою університету. 120 сторінок.",
            price: 180,
            category: "Канцелярія"
        },
        {
            id: 7,
            name: "Еко-сумка",
            description: "Екологічна сумка з принтом університету. Вміщує до 10 кг, складається компактно.",
            price: 150,
            category: "Аксесуари"
        },
        {
            id: 8,
            name: "Толстовка з написом",
            description: "Тепла толстовка з написом 'ХДУ - моя альма-матер'. 80% бавовна, 20% поліестер.",
            price: 950,
            category: "Одяг"
        },
        {
            id: 9,
            name: "Брелок ХДУ",
            description: "Металевий брелок з логотипом університету. Ідеальний сувенір.",
            price: 120,
            category: "Аксесуари"
        },
        {
            id: 10,
            name: "Зошит студента",
            description: "Зошит з обкладинкою ХДУ. 96 аркушів, клітинка.",
            price: 90,
            category: "Канцелярія"
        }
    ];

    // Кошик
    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    
    // Поточний вид перегляду (grid/list)
    let currentView = 'grid';
    
    // Елементи DOM
    const productsGrid = document.getElementById('productsGrid');
    const productsList = document.getElementById('productsList');
    const cartIcon = document.getElementById('cartIcon');
    const cartModal = document.getElementById('cartModal');
    const cartItems = document.getElementById('cartItems');
    const cartCount = document.getElementById('cartCount');
    const cartSubtotal = document.getElementById('cartSubtotal');
    const cartShipping = document.getElementById('cartShipping');
    const cartTotal = document.getElementById('cartTotal');
    const searchInput = document.getElementById('searchInput');
    const searchBtn = document.getElementById('searchBtn');
    const priceSlider = document.getElementById('priceSlider');
    const currentPrice = document.getElementById('currentPrice');
    const sortSelect = document.getElementById('sortSelect');
    const applyFilters = document.getElementById('applyFilters');
    const resetFilters = document.getElementById('resetFilters');
    const gridViewBtn = document.getElementById('gridViewBtn');
    const listViewBtn = document.getElementById('listViewBtn');
    const categoryFilters = document.querySelectorAll('.category-filter');
    const loginBtn = document.getElementById('loginBtn');
    const registerBtn = document.getElementById('registerBtn');
    const loginPage = document.getElementById('loginPage');
    const registerPage = document.getElementById('registerPage');
    const mainContent = document.querySelector('main');
    const footer = document.querySelector('.footer');
    const backButtons = document.querySelectorAll('.back-to-main');
    const switchToRegister = document.querySelector('.switch-to-register');
    const switchToLogin = document.querySelector('.switch-to-login');
    const submitLogin = document.getElementById('submitLogin');
    const submitRegister = document.getElementById('submitRegister');
    
    // Ініціалізація
    renderProducts(products, currentView);
    updateCartCount();
    
    // Рендер товарів
    function renderProducts(productsToRender, viewType) {
        if (viewType === 'grid') {
            renderGridView(productsToRender);
        } else {
            renderListView(productsToRender);
        }
    }
    
    // Рендер у вигляді сітки
    function renderGridView(productsToRender) {
        productsGrid.innerHTML = '';
        productsList.style.display = 'none';
        productsGrid.style.display = 'grid';
        
        if (productsToRender.length === 0) {
            productsGrid.innerHTML = `
                <div style="grid-column: 1/-1; text-align: center; padding: 50px;">
                    <i class="fas fa-search" style="font-size: 3rem; color: #ccc; margin-bottom: 20px;"></i>
                    <h3 style="color: #666;">Товари не знайдено</h3>
                    <p>Спробуйте змінити параметри пошуку або фільтри</p>
                </div>
            `;
            return;
        }
        
        productsToRender.forEach(function(product) {
            const productCard = document.createElement('div');
            productCard.className = 'product-card';
            productCard.innerHTML = `
                <div class="product-image">
                    <div class="product-image-placeholder"></div>
                </div>
                <div class="product-info">
                    <h3 class="product-title">${product.name}</h3>
                    <p class="product-description">${product.description}</p>
                    <div class="product-footer">
                        <div class="product-price">${product.price} грн</div>
                        <button class="add-to-cart" data-id="${product.id}">
                            <i class="fas fa-cart-plus"></i> Додати
                        </button>
                    </div>
                </div>
            `;
            
            productsGrid.appendChild(productCard);
            
            // Обробник кліку по картці товару
            productCard.addEventListener('click', function(e) {
                if (!e.target.classList.contains('add-to-cart')) {
                    showProductModal(product);
                }
            });
        });
        
        // Додавання обробників для кнопок "Додати в кошик"
        document.querySelectorAll('.add-to-cart').forEach(function(button) {
            button.addEventListener('click', function(e) {
                e.stopPropagation();
                const productId = parseInt(this.getAttribute('data-id'));
                addToCart(productId);
            });
        });
    }
    
    // Рендер у вигляді списку
    function renderListView(productsToRender) {
        productsList.innerHTML = '';
        productsGrid.style.display = 'none';
        productsList.style.display = 'flex';
        
        if (productsToRender.length === 0) {
            productsList.innerHTML = `
                <div style="width: 100%; text-align: center; padding: 50px;">
                    <i class="fas fa-search" style="font-size: 3rem; color: #ccc; margin-bottom: 20px;"></i>
                    <h3 style="color: #666;">Товари не знайдено</h3>
                    <p>Спробуйте змінити параметри пошуку або фільтри</p>
                </div>
            `;
            return;
        }
        
        productsToRender.forEach(function(product) {
            const listItem = document.createElement('div');
            listItem.className = 'product-list-item';
            listItem.innerHTML = `
                <div class="list-item-image">
                    <div class="product-image-placeholder"></div>
                </div>
                <div class="list-item-info">
                    <div class="list-item-header">
                        <h3 class="list-item-title">${product.name}</h3>
                        <span class="list-item-category">${product.category}</span>
                        <p class="list-item-description">${product.description}</p>
                    </div>
                    <div class="list-item-footer">
                        <div class="list-item-price">${product.price} грн</div>
                        <button class="add-to-cart" data-id="${product.id}">
                            <i class="fas fa-cart-plus"></i> Додати до кошика
                        </button>
                    </div>
                </div>
            `;
            
            productsList.appendChild(listItem);
            
            // Обробник кліку по елементу списку
            listItem.addEventListener('click', function(e) {
                if (!e.target.classList.contains('add-to-cart')) {
                    showProductModal(product);
                }
            });
        });
        
        // Додавання обробників для кнопок "Додати в кошик"
        document.querySelectorAll('.add-to-cart').forEach(function(button) {
            button.addEventListener('click', function(e) {
                e.stopPropagation();
                const productId = parseInt(this.getAttribute('data-id'));
                addToCart(productId);
            });
        });
    }
    
    // Показати модальне вікно товару
    function showProductModal(product) {
        const modal = document.getElementById('productModal');
        const content = document.getElementById('modalProductContent');
        
        content.innerHTML = `
            <h2>${product.name}</h2>
            <div class="product-modal-image" style="text-align: center; margin: 20px 0;">
                <div style="width: 100%; height: 200px; background: linear-gradient(135deg, #f0f5ff 0%, #e6f0ff 100%); border-radius: 12px;"></div>
            </div>
            <p><strong>Категорія:</strong> ${product.category}</p>
            <p><strong>Опис:</strong> ${product.description}</p>
            <p><strong>Ціна:</strong> <span style="font-size: 1.5rem; color: #0066cc; font-weight: bold;">${product.price} грн</span></p>
            <p><strong>Наявність:</strong> <span style="color: green;">В наявності</span></p>
            <div style="margin-top: 30px; display: flex; gap: 10px; flex-wrap: wrap;">
                <button class="btn btn-primary" id="modalAddToCart" data-id="${product.id}" style="flex: 1; min-width: 200px;">
                    <i class="fas fa-cart-plus"></i> Додати до кошика
                </button>
                <button class="btn btn-outline" style="flex: 1; min-width: 200px; border-color: #0066cc; color: #0066cc;" onclick="this.closest('.modal').style.display='none'">
                    <i class="fas fa-times"></i> Закрити
                </button>
            </div>
        `;
        
        modal.style.display = 'flex';
        
        // Додавання обробника для кнопки в модальному вікні
        document.getElementById('modalAddToCart').addEventListener('click', function() {
            const productId = parseInt(this.getAttribute('data-id'));
            addToCart(productId);
            modal.style.display = 'none';
        });
    }
    
    // Додати товар до кошика
    function addToCart(productId) {
        const product = products.find(function(p) { return p.id === productId; });
        
        if (!product) return;
        
        const existingItem = cart.find(function(item) { return item.id === productId; });
        
        if (existingItem) {
            existingItem.quantity += 1;
        } else {
            cart.push({
                id: product.id,
                name: product.name,
                price: product.price,
                quantity: 1
            });
        }
        
        updateCart();
        showNotification('Товар додано до кошика!');
    }
    
    // Оновити кошик
    function updateCart() {
        localStorage.setItem('cart', JSON.stringify(cart));
        updateCartCount();
        renderCartItems();
    }
    
    // Оновити кількість товарів у кошику
    function updateCartCount() {
        const totalItems = cart.reduce(function(total, item) {
            return total + item.quantity;
        }, 0);
        
        cartCount.textContent = totalItems;
    }
    
    // Відобразити товари в кошику
    function renderCartItems() {
        cartItems.innerHTML = '';
        
        if (cart.length === 0) {
            cartItems.innerHTML = '<p style="text-align: center; color: #666;">Кошик порожній</p>';
            cartSubtotal.textContent = '0 грн';
            cartShipping.textContent = '0 грн';
            cartTotal.textContent = '0 грн';
            return;
        }
        
        let subtotal = 0;
        
        cart.forEach(function(item) {
            const itemTotal = item.price * item.quantity;
            subtotal += itemTotal;
            
            const cartItem = document.createElement('div');
            cartItem.className = 'cart-item';
            cartItem.innerHTML = `
                <div class="cart-item-info">
                    <h4>${item.name}</h4>
                    <div class="cart-item-price">${item.price} грн × ${item.quantity} = ${itemTotal} грн</div>
                </div>
                <div class="cart-item-actions">
                    <div class="quantity-control">
                        <button class="decrease-quantity" data-id="${item.id}">-</button>
                        <span>${item.quantity}</span>
                        <button class="increase-quantity" data-id="${item.id}">+</button>
                    </div>
                    <button class="remove-item" data-id="${item.id}">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            `;
            
            cartItems.appendChild(cartItem);
        });
        
        // Розрахунок доставки
        const shipping = subtotal > 1000 ? 0 : 50;
        const total = subtotal + shipping;
        
        cartSubtotal.textContent = subtotal + ' грн';
        cartShipping.textContent = shipping + ' грн';
        cartTotal.textContent = total + ' грн';
        
        // Додавання обробників для кнопок в кошику
        document.querySelectorAll('.decrease-quantity').forEach(function(button) {
            button.addEventListener('click', function() {
                const productId = parseInt(this.getAttribute('data-id'));
                updateCartItemQuantity(productId, -1);
            });
        });
        
        document.querySelectorAll('.increase-quantity').forEach(function(button) {
            button.addEventListener('click', function() {
                const productId = parseInt(this.getAttribute('data-id'));
                updateCartItemQuantity(productId, 1);
            });
        });
        
        document.querySelectorAll('.remove-item').forEach(function(button) {
            button.addEventListener('click', function() {
                const productId = parseInt(this.getAttribute('data-id'));
                removeFromCart(productId);
            });
        });
    }
    
    // Оновити кількість товару в кошику
    function updateCartItemQuantity(productId, change) {
        const item = cart.find(function(item) { return item.id === productId; });
        
        if (!item) return;
        
        item.quantity += change;
        
        if (item.quantity <= 0) {
            cart = cart.filter(function(item) { return item.id !== productId; });
        }
        
        updateCart();
    }
    
    // Видалити товар з кошика
    function removeFromCart(productId) {
        cart = cart.filter(function(item) { return item.id !== productId; });
        updateCart();
    }
    
    // Показати сповіщення
    function showNotification(message) {
        const notification = document.createElement('div');
        notification.className = 'notification';
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background-color: #0066cc;
            color: white;
            padding: 15px 20px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.2);
            z-index: 1001;
            animation: slideIn 0.3s;
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(function() {
            notification.style.animation = 'slideOut 0.3s';
            setTimeout(function() {
                document.body.removeChild(notification);
            }, 300);
        }, 3000);
    }
    
    // Фільтрація та сортування
    function filterAndSortProducts() {
        const searchTerm = searchInput.value.toLowerCase();
        const maxPrice = parseInt(priceSlider.value);
        const sortBy = sortSelect.value;
        
        // Отримати вибрані категорії
        const selectedCategories = [];
        categoryFilters.forEach(function(checkbox) {
            if (checkbox.checked && checkbox.value !== 'all') {
                selectedCategories.push(checkbox.value);
            }
        });
        
        // Перевірити, чи вибрано "Всі товари"
        const allChecked = document.querySelector('.category-filter[value="all"]').checked;
        
        let filteredProducts = products.filter(function(product) {
            const matchesSearch = product.name.toLowerCase().includes(searchTerm) || 
                                 product.description.toLowerCase().includes(searchTerm) ||
                                 product.category.toLowerCase().includes(searchTerm);
            const matchesPrice = product.price <= maxPrice;
            const matchesCategory = allChecked || selectedCategories.length === 0 || selectedCategories.includes(product.category);
            
            return matchesSearch && matchesPrice && matchesCategory;
        });
        
        // Сортування
        if (sortBy === 'price-asc') {
            filteredProducts.sort(function(a, b) { return a.price - b.price; });
        } else if (sortBy === 'price-desc') {
            filteredProducts.sort(function(a, b) { return b.price - a.price; });
        } else if (sortBy === 'newest') {
            filteredProducts.sort(function(a, b) { return b.id - a.id; });
        }
        
        renderProducts(filteredProducts, currentView);
    }
    
    // Живий пошук при введенні тексту
    function liveSearch() {
        filterAndSortProducts();
    }
    
    // Скинути фільтри
    function resetAllFilters() {
        searchInput.value = '';
        priceSlider.value = 2500;
        currentPrice.textContent = '2500';
        sortSelect.value = 'popularity';
        
        // Скинути всі чекбокси категорій
        categoryFilters.forEach(function(checkbox) {
            checkbox.checked = checkbox.value === 'all';
        });
        
        filterAndSortProducts();
    }
    
    // Переключення між сторінками
    function showLoginPage() {
        mainContent.style.display = 'none';
        footer.style.display = 'none';
        loginPage.style.display = 'block';
        registerPage.style.display = 'none';
    }
    
    function showRegisterPage() {
        mainContent.style.display = 'none';
        footer.style.display = 'none';
        registerPage.style.display = 'block';
        loginPage.style.display = 'none';
    }
    
    function showMainPage() {
        mainContent.style.display = 'block';
        footer.style.display = 'block';
        loginPage.style.display = 'none';
        registerPage.style.display = 'none';
    }
    
    // Обробники подій
    cartIcon.addEventListener('click', function() {
        renderCartItems();
        cartModal.style.display = 'flex';
    });
    
    // Живий пошук при введенні тексту
    searchInput.addEventListener('input', liveSearch);
    searchBtn.addEventListener('click', filterAndSortProducts);
    
    priceSlider.addEventListener('input', function() {
        currentPrice.textContent = this.value;
        filterAndSortProducts();
    });
    
    applyFilters.addEventListener('click', filterAndSortProducts);
    resetFilters.addEventListener('click', resetAllFilters);
    
    // Обробники для перемикання виду
    gridViewBtn.addEventListener('click', function() {
        if (currentView !== 'grid') {
            currentView = 'grid';
            gridViewBtn.classList.add('active');
            listViewBtn.classList.remove('active');
            filterAndSortProducts();
        }
    });
    
    listViewBtn.addEventListener('click', function() {
        if (currentView !== 'list') {
            currentView = 'list';
            listViewBtn.classList.add('active');
            gridViewBtn.classList.remove('active');
            filterAndSortProducts();
        }
    });
    
    // Обробники для авторизації
    loginBtn.addEventListener('click', showLoginPage);
    registerBtn.addEventListener('click', showRegisterPage);
    
    backButtons.forEach(function(button) {
        button.addEventListener('click', showMainPage);
    });
    
    if (switchToRegister) {
        switchToRegister.addEventListener('click', function(e) {
            e.preventDefault();
            showRegisterPage();
        });
    }
    
    if (switchToLogin) {
        switchToLogin.addEventListener('click', function(e) {
            e.preventDefault();
            showLoginPage();
        });
    }
    
    submitLogin.addEventListener('click', function() {
        const email = document.getElementById('loginEmail').value;
        const password = document.getElementById('loginPassword').value;
        
        if (!email || !password) {
            alert('Будь ласка, заповніть всі поля');
            return;
        }
        
        // Симуляція входу
        showNotification('Вхід успішний!');
        setTimeout(function() {
            showMainPage();
        }, 1500);
    });
    
    submitRegister.addEventListener('click', function() {
        const firstName = document.getElementById('regFirstName').value;
        const lastName = document.getElementById('regLastName').value;
        const email = document.getElementById('regEmail').value;
        const phone = document.getElementById('regPhone').value;
        const password = document.getElementById('regPassword').value;
        const confirmPassword = document.getElementById('regConfirmPassword').value;
        const terms = document.getElementById('regTerms').checked;
        
        if (!firstName || !lastName || !email || !password || !confirmPassword) {
            alert('Будь ласка, заповніть обов\'язкові поля');
            return;
        }
        
        if (password !== confirmPassword) {
            alert('Паролі не співпадають');
            return;
        }
        
        if (password.length < 8) {
            alert('Пароль має містити мінімум 8 символів');
            return;
        }
        
        if (!terms) {
            alert('Будь ласка, прийміть умови використання');
            return;
        }
        
        // Симуляція реєстрації
        showNotification('Реєстрація успішна!');
        setTimeout(function() {
            showMainPage();
        }, 1500);
    });
    
    // Обробники закриття модальних вікон
    document.querySelectorAll('.close-modal').forEach(function(closeBtn) {
        closeBtn.addEventListener('click', function() {
            this.closest('.modal').style.display = 'none';
        });
    });
    
    // Закриття модальних вікон при кліку поза ними
    window.addEventListener('click', function(e) {
        if (e.target.classList.contains('modal')) {
            e.target.style.display = 'none';
        }
    });
    
    // Обробник для чекбоксу "Всі товари"
    document.querySelector('.category-filter[value="all"]').addEventListener('change', function() {
        if (this.checked) {
            categoryFilters.forEach(function(checkbox) {
                if (checkbox.value !== 'all') {
                    checkbox.checked = false;
                }
            });
        }
        filterAndSortProducts();
    });
    
    // Обробники для інших чекбоксів категорій
    categoryFilters.forEach(function(checkbox) {
        if (checkbox.value !== 'all') {
            checkbox.addEventListener('change', function() {
                if (this.checked) {
                    document.querySelector('.category-filter[value="all"]').checked = false;
                }
                filterAndSortProducts();
            });
        }
    });
    
    // Обробник для зміни сортування
    sortSelect.addEventListener('change', filterAndSortProducts);
    
    // Додавання стилів для анімації сповіщень
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from { transform: translateX(100%); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
        
        @keyframes slideOut {
            from { transform: translateX(0); opacity: 1; }
            to { transform: translateX(100%); opacity: 0; }
        }
        
        .notification {
            font-family: inherit;
        }
    `;
    document.head.appendChild(style);
});