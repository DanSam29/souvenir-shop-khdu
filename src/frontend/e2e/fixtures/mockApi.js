// Мокований backend для E2E-тестів (перехоплення запитів через Playwright route).
// Дефолтний режим: усі запити до /api/** обслуговуються тут, реальний сервер не потрібен.
// Стан ізольований на кожен тест (створюється в installMockApi).

const SEED_CATEGORIES = [
  { categoryId: 1, name: 'Студентське', nameEn: 'Student', description: 'Студентські сувеніри', subCategories: [] },
  { categoryId: 2, name: 'Одяг та аксесуари', nameEn: 'Apparel', description: 'Одяг та аксесуари ХДУ', subCategories: [] },
];

const SEED_PRODUCTS = [
  {
    productId: 1, name: 'Студентська чашка ХДУ', nameEn: 'KSU Student Mug',
    description: 'Керамічна чашка з логотипом університету', descriptionEn: 'Ceramic mug with university logo',
    price: 250, originalPrice: null, weight: 0.4, stock: 10, categoryId: 1,
    category: { categoryId: 1, name: 'Студентське', nameEn: 'Student' },
    images: [], createdAt: '2026-01-10T10:00:00Z',
  },
  {
    productId: 2, name: 'Блокнот ХДУ', nameEn: 'KSU Notebook',
    description: 'Блокнот у твердій обкладинці', descriptionEn: 'Hardcover notebook',
    price: 120, originalPrice: null, weight: 0.3, stock: 6, categoryId: 1,
    category: { categoryId: 1, name: 'Студентське', nameEn: 'Student' },
    images: [], createdAt: '2026-01-09T10:00:00Z',
  },
  {
    productId: 3, name: 'Футболка ХДУ', nameEn: 'KSU T-Shirt',
    description: 'Футболка з принтом університету', descriptionEn: 'T-shirt with university print',
    price: 450, originalPrice: 500, weight: 0.2, stock: 3, categoryId: 2,
    category: { categoryId: 2, name: 'Одяг та аксесуари', nameEn: 'Apparel' },
    images: [], createdAt: '2026-01-08T10:00:00Z',
  },
  {
    productId: 4, name: 'Ручка з логотипом', nameEn: 'Logo Pen',
    description: 'Кулькова ручка з гравіюванням', descriptionEn: 'Ballpoint pen with engraving',
    price: 45, originalPrice: null, weight: 0.05, stock: 0, categoryId: 2,
    category: { categoryId: 2, name: 'Одяг та аксесуари', nameEn: 'Apparel' },
    images: [], createdAt: '2026-01-07T10:00:00Z',
  },
  {
    productId: 5, name: 'Шопер ХДУ', nameEn: 'KSU Tote Bag',
    description: 'Тканинна сумка-шопер', descriptionEn: 'Cotton tote bag',
    price: 300, originalPrice: null, weight: 0.25, stock: 7, categoryId: 2,
    category: { categoryId: 2, name: 'Одяг та аксесуари', nameEn: 'Apparel' },
    images: [], createdAt: '2026-01-06T10:00:00Z',
  },
];

// Тестові користувачі (узгоджені з src/backend/Data/DbSeeder.cs: пароль Password123!)
const SEED_USERS = {
  'customer@ksu.edu.ua': {
    userId: 101, firstName: 'Олена', lastName: 'Ковальчук', email: 'customer@ksu.edu.ua',
    password: 'Password123!', role: 'Customer', phone: '+380501112233',
    studentStatus: 'REGULAR', studentExpiresAt: '2026-12-31T00:00:00Z', preferredLanguage: 'ua',
  },
  'manager@ksu.edu.ua': {
    userId: 102, firstName: 'Ігор', lastName: 'Петренко', email: 'manager@ksu.edu.ua',
    password: 'Password123!', role: 'Manager', phone: '+380502223344',
    studentStatus: 'NONE', studentExpiresAt: null, preferredLanguage: 'ua',
  },
  'admin@ksu.edu.ua': {
    userId: 103, firstName: 'Адмін', lastName: 'Системний', email: 'admin@ksu.edu.ua',
    password: 'Password123!', role: 'Administrator', phone: '+380503334455',
    studentStatus: 'NONE', studentExpiresAt: null, preferredLanguage: 'ua',
  },
};

// Промокоди: валідний та (за замовчуванням) невалідні
const SEED_PROMOS = {
  STUDENT10: { code: 'STUDENT10', name: 'Студентська знижка 10%', type: 'PERCENTAGE', value: 10 },
};

// Початкове COD-замовлення у статусі Processing (для адмін-сценарію)
const SEED_ORDERS = [
  {
    orderId: 5001, orderNumber: 'ORD-2026-0001', userId: 101,
    userName: 'Олена Ковальчук', userEmail: 'customer@ksu.edu.ua',
    status: 'Processing', totalAmount: 620, createdAt: '2026-02-01T12:00:00Z',
    paymentMethod: 'CashOnDelivery', paymentStatus: 'Pending',
    trackingNumber: null,
    shipping: { recipientName: 'Олена Ковальчук', recipientPhone: '+380501112233', city: 'Київ', warehouseNumber: '12' },
    items: [{ productId: 1, productName: 'Студентська чашка ХДУ', quantity: 2, price: 250, finalPrice: 250, originalPrice: 250 }],
    history: [{ historyId: 1, newStatus: 'Processing', oldStatus: null, changedAt: '2026-02-01T12:00:00Z' }],
  },
];

const SEED_COMPANIES = [
  { companyId: 1, name: 'ТОВ «Сувеніри Опт»', contactPerson: 'Марія Іваненко', phone: '+380441112233', email: 'opt@souvenirs.ua', isActive: true },
];

const clone = (value) => JSON.parse(JSON.stringify(value));

// Створення ізольованого стану моку для одного тесту
function createState(options = {}) {
  const users = {};
  Object.entries(clone(SEED_USERS)).forEach(([email, user]) => { users[email] = user; });
  return {
    users,
    products: clone(SEED_PRODUCTS),
    categories: clone(SEED_CATEGORIES),
    promos: clone(SEED_PROMOS),
    orders: clone(SEED_ORDERS),
    companies: clone(SEED_COMPANIES),
    incoming: [],
    carts: {},
    stripeEnabled: options.stripeEnabled ?? false,
    novaPoshtaEnabled: options.novaPoshtaEnabled ?? false,
    nextOrderId: 6000,
    nextCartItemId: 9000,
    nextProductId: 900,
    nextCategoryId: 900,
    nextCompanyId: 900,
  };
}

const json = (route, status, body) =>
  route.fulfill({ status, contentType: 'application/json; charset=utf-8', body: JSON.stringify(body) });
const ok = (route, data) => json(route, 200, { success: true, data });
const fail = (route, status, message, errors = []) =>
  json(route, status, { success: false, message, errors });

// Визначення користувача за Bearer-токеном (формат mock-token:<userId>)
function authUser(state, request) {
  const header = request.headers()['authorization'] || '';
  const token = header.replace(/^Bearer\s+/i, '');
  if (!token.startsWith('mock-token:')) return null;
  const userId = Number(token.split(':')[1]);
  return Object.values(state.users).find((u) => u.userId === userId) || null;
}

const publicUser = (user) => {
  const { password, ...rest } = user;
  return rest;
};

function cartOf(state, user) {
  if (!state.carts[user.userId]) state.carts[user.userId] = { items: [] };
  return state.carts[user.userId];
}

// Перетворення внутрішнього кошика у формат CartResponse
function cartResponse(state, cart) {
  const items = cart.items.map((it) => {
    const product = state.products.find((p) => p.productId === it.productId);
    const price = product?.price ?? 0;
    return {
      cartItemId: it.cartItemId,
      productId: it.productId,
      productName: product?.name ?? '',
      productNameEn: product?.nameEn ?? '',
      productImage: null,
      productPrice: price,
      originalPrice: product?.originalPrice ?? price,
      quantity: it.quantity,
      weight: product?.weight ?? 0.5,
      subtotal: Number((price * it.quantity).toFixed(2)),
    };
  });
  const totalAmount = Number(items.reduce((sum, i) => sum + i.subtotal, 0).toFixed(2));
  const itemCount = items.reduce((sum, i) => sum + i.quantity, 0);
  return { items, itemCount, totalAmount };
}

const orderSummary = (order) => ({
  orderId: order.orderId,
  orderNumber: order.orderNumber,
  userName: order.userName,
  userEmail: order.userEmail,
  status: order.status,
  totalAmount: order.totalAmount,
  createdAt: order.createdAt,
  trackingNumber: order.trackingNumber,
});

const orderDetail = (order) => ({
  orderId: order.orderId,
  orderNumber: order.orderNumber,
  status: order.status,
  totalAmount: order.totalAmount,
  createdAt: order.createdAt,
  shippingCost: 0,
  shipping: order.shipping,
  payment: { method: order.paymentMethod, amount: order.totalAmount, status: order.paymentStatus },
  items: order.items,
  history: order.history,
  appliedDiscounts: [],
});

function paginate(items, pageNumber, pageSize) {
  const page = Number(pageNumber) || 1;
  const size = Number(pageSize) || 10;
  return {
    items: items.slice((page - 1) * size, page * size),
    pageNumber: page,
    pageSize: size,
    totalItems: items.length,
    totalPages: Math.max(1, Math.ceil(items.length / size)),
  };
}

/**
 * Встановлює мокований API на сторінку та повертає стан моку.
 * @param {import('@playwright/test').Page} page
 * @param {{ stripeEnabled?: boolean }} [options]
 */
async function installMockApi(page, options = {}) {
  const state = createState(options);

  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const method = request.method();
    const url = new URL(request.url());
    const path = url.pathname.replace(/^\/api/, '');
    const params = url.searchParams;
    let body = {};
    try { body = JSON.parse(request.postData() || '{}'); } catch { body = {}; }
    const user = authUser(state, request);

    // ---------------- Публічні довідники ----------------
    if (method === 'GET' && path === '/Features/status') {
      return ok(route, {
        stripeEnabled: state.stripeEnabled,
        novaPoshtaEnabled: state.novaPoshtaEnabled,
        universityEnabled: false,
      });
    }

    if (method === 'GET' && path === '/Categories') return ok(route, state.categories);
    if (method === 'GET' && /^\/Categories\/\d+$/.test(path)) {
      const id = Number(path.split('/')[2]);
      return ok(route, state.categories.find((c) => c.categoryId === id) || null);
    }

    // ---------------- Товари ----------------
    if (method === 'GET' && path === '/Products/search') {
      const query = (params.get('query') || '').toLowerCase();
      const found = state.products.filter((p) => p.name.toLowerCase().includes(query));
      return ok(route, found);
    }
    if (method === 'GET' && (path === '/Products/featured' || path === '/Products/new')) {
      const limit = Number(params.get('limit')) || 8;
      return ok(route, state.products.slice(0, limit));
    }
    if (method === 'GET' && path.startsWith('/Products/category/')) {
      const id = Number(path.split('/').pop());
      return ok(route, state.products.filter((p) => p.categoryId === id));
    }
    if (method === 'GET' && path === '/Products') {
      const size = Number(params.get('pageSize')) || 12;
      return ok(route, paginate(state.products, params.get('pageNumber'), size));
    }
    if (method === 'GET' && /^\/Products\/\d+$/.test(path)) {
      const id = Number(path.split('/')[2]);
      const product = state.products.find((p) => p.productId === id);
      if (!product) return fail(route, 404, 'Товар не знайдено');
      return ok(route, { ...product, relatedProducts: [] });
    }

    // ---------------- Акції та промокоди ----------------
    if (method === 'GET' && path === '/Promotions/active') return ok(route, []);
    if (method === 'GET' && path === '/Promotions/my') return ok(route, []);
    if (method === 'POST' && path === '/Promotions/validate') {
      const code = (body.code || '').trim().toUpperCase();
      const promo = state.promos[code];
      if (!promo) return ok(route, { valid: false, error: 'Промокод недійсний' });
      return ok(route, { valid: true, promotion: promo });
    }

    // ---------------- Авторизація ----------------
    if (method === 'POST' && path === '/Users/login') {
      const email = (body.email || '').trim().toLowerCase();
      const entry = Object.entries(state.users).find(([, u]) => u.email.toLowerCase() === email);
      if (!entry || entry[1].password !== body.password) {
        return fail(route, 401, 'Невірний email або пароль', ['InvalidCredentials']);
      }
      const u = entry[1];
      return ok(route, { token: `mock-token:${u.userId}`, refreshToken: `mock-refresh:${u.userId}`, ...publicUser(u) });
    }
    if (method === 'POST' && path === '/Users/register') {
      const email = (body.email || '').trim().toLowerCase();
      if (Object.values(state.users).some((u) => u.email.toLowerCase() === email)) {
        return fail(route, 409, 'Email вже зареєстрований', ['EmailAlreadyExists']);
      }
      const newUser = {
        userId: 700 + Object.keys(state.users).length,
        firstName: body.firstName || '', lastName: body.lastName || '', email,
        password: body.password || '', role: 'Customer', phone: body.phone || '',
        studentStatus: 'NONE', studentExpiresAt: null, preferredLanguage: body.language || 'ua',
      };
      state.users[email] = newUser;
      return ok(route, { token: `mock-token:${newUser.userId}`, ...publicUser(newUser) });
    }
    if (method === 'GET' && path === '/Users/me') {
      if (!user) return fail(route, 401, 'Unauthorized');
      return ok(route, publicUser(user));
    }
    if (method === 'PUT' && path === '/Users/me') {
      if (!user) return fail(route, 401, 'Unauthorized');
      Object.assign(user, { firstName: body.firstName ?? user.firstName, lastName: body.lastName ?? user.lastName, phone: body.phone ?? user.phone });
      return ok(route, publicUser(user));
    }
    if (method === 'PUT' && path === '/Users/me/language') {
      if (!user) return fail(route, 401, 'Unauthorized');
      user.preferredLanguage = body.language || user.preferredLanguage;
      return ok(route, publicUser(user));
    }
    if (method === 'POST' && path === '/Users/change-password') return ok(route, true);
    if (method === 'GET' && path === '/Users/me/addresses') return ok(route, []);
    if (method === 'GET' && path === '/Users/me/promotions/history') return ok(route, []);
    if (method === 'POST' && path === '/Users/me/student-status/refresh') {
      if (!user) return fail(route, 401, 'Unauthorized');
      return ok(route, publicUser(user));
    }
    if (method === 'GET' && /^\/Users\/me\/orders\/\d+$/.test(path)) {
      const id = Number(path.split('/').pop());
      const order = state.orders.find((o) => o.orderId === id && o.userId === user?.userId);
      if (!order) return fail(route, 404, 'Замовлення не знайдено');
      return ok(route, orderDetail(order));
    }

    // ---------------- Кошик ----------------
    if (path === '/Cart' || path.startsWith('/Cart/items')) {
      if (!user) return fail(route, 401, 'Unauthorized');
      const cart = cartOf(state, user);

      if (method === 'GET' && path === '/Cart') return ok(route, cartResponse(state, cart));
      if (method === 'DELETE' && path === '/Cart') {
        cart.items = [];
        return ok(route, true);
      }
      if (method === 'POST' && path === '/Cart/items') {
        const product = state.products.find((p) => p.productId === Number(body.productId));
        if (!product) return fail(route, 404, 'Товар не знайдено');
        if (product.stock <= 0) return fail(route, 400, 'Недостатньо товару на складі', ['InsufficientStock']);
        const existing = cart.items.find((i) => i.productId === product.productId);
        if (existing) existing.quantity += Number(body.quantity) || 1;
        else cart.items.push({ cartItemId: state.nextCartItemId++, productId: product.productId, quantity: Number(body.quantity) || 1 });
        return ok(route, cartResponse(state, cart));
      }
      if (method === 'PUT' && /^\/Cart\/items\/\d+$/.test(path)) {
        const cartItemId = Number(path.split('/').pop());
        const item = cart.items.find((i) => i.cartItemId === cartItemId);
        if (!item) return fail(route, 404, 'Позицію кошика не знайдено');
        const product = state.products.find((p) => p.productId === item.productId);
        const quantity = Number(body.quantity);
        // Негативний сценарій: кількість перевищує залишок на складі
        if (quantity > (product?.stock ?? 0)) {
          return fail(route, 400, 'Недостатньо товару на складі', ['InsufficientStock']);
        }
        if (quantity <= 0) cart.items = cart.items.filter((i) => i.cartItemId !== cartItemId);
        else item.quantity = quantity;
        return ok(route, cartResponse(state, cart));
      }
      if (method === 'DELETE' && /^\/Cart\/items\/\d+$/.test(path)) {
        const cartItemId = Number(path.split('/').pop());
        cart.items = cart.items.filter((i) => i.cartItemId !== cartItemId);
        return ok(route, cartResponse(state, cart));
      }
    }

    // ---------------- Замовлення ----------------
    if (method === 'POST' && path === '/Orders/calculate') {
      const cart = user ? cartOf(state, user) : { items: [] };
      const base = cartResponse(state, cart);
      const code = (body.promoCode || '').trim().toUpperCase();
      const promo = state.promos[code];
      let totalAmount = base.totalAmount;
      let promoCodeError = null;
      if (code && !promo) {
        totalAmount = base.totalAmount;
        promoCodeError = 'Промокод недійсний';
      } else if (promo) {
        totalAmount = Number((base.totalAmount * (1 - promo.value / 100)).toFixed(2));
      }
      return ok(route, { items: base.items, totalAmount, promoCodeError });
    }
    if (method === 'POST' && path === '/Orders') {
      if (!user) return fail(route, 401, 'Unauthorized');
      const cart = cartOf(state, user);
      if (cart.items.length === 0) return fail(route, 400, 'Кошик порожній');
      const base = cartResponse(state, cart);
      const code = (body.promoCode || '').trim().toUpperCase();
      const promo = state.promos[code];
      if (code && !promo) return fail(route, 400, 'Промокод недійсний', ['InvalidPromoCode']);
      const totalAmount = promo ? Number((base.totalAmount * (1 - promo.value / 100)).toFixed(2)) : base.totalAmount;
      const paymentMethod = body.paymentMethod === 'Card' && state.stripeEnabled ? 'Card' : 'CashOnDelivery';
      const order = {
        orderId: state.nextOrderId++,
        orderNumber: `ORD-2026-${String(state.nextOrderId).padStart(4, '0')}`,
        userId: user.userId,
        userName: `${user.firstName} ${user.lastName}`.trim(),
        userEmail: user.email,
        status: paymentMethod === 'Card' ? 'Pending' : 'Processing',
        totalAmount,
        createdAt: new Date().toISOString(),
        paymentMethod,
        paymentStatus: 'Pending',
        trackingNumber: null,
        shipping: {
          recipientName: body.recipientName || `${user.firstName} ${user.lastName}`.trim(),
          recipientPhone: body.recipientPhone || user.phone,
          city: body.city || '',
          warehouseNumber: body.warehouseNumber || '',
        },
        items: base.items.map((i) => ({ productId: i.productId, productName: i.productName, quantity: i.quantity, price: i.productPrice, finalPrice: i.productPrice, originalPrice: i.originalPrice })),
        history: [{ historyId: 1, newStatus: paymentMethod === 'Card' ? 'Pending' : 'Processing', oldStatus: null, changedAt: new Date().toISOString() }],
      };
      state.orders.push(order);
      cart.items = [];
      if (paymentMethod === 'Card') {
        // Мокований редирект на сторінку оплати (Stripe CLI не потрібен)
        return ok(route, { orderId: order.orderId, orderNumber: order.orderNumber, totalAmount, paymentUrl: '/checkout/cancel' });
      }
      return ok(route, { orderId: order.orderId, orderNumber: order.orderNumber, totalAmount, status: order.status });
    }
    if (method === 'GET' && path === '/Orders/my') {
      if (!user) return fail(route, 401, 'Unauthorized');
      return ok(route, state.orders.filter((o) => o.userId === user.userId).map(orderDetail));
    }
    if (method === 'GET' && /^\/Orders\/\d+$/.test(path)) {
      const id = Number(path.split('/')[2]);
      const order = state.orders.find((o) => o.orderId === id);
      if (!order) return fail(route, 404, 'Замовлення не знайдено');
      return ok(route, orderDetail(order));
    }

    // ---------------- Nova Poshta (заглушка) ----------------
    // Увага: перевірка /np/tracking/ мусить бути ДО загального /np/
    if (method === 'GET' && path.startsWith('/np/tracking/')) return ok(route, { available: false });
    if (method === 'POST' && path === '/np/calculate') return ok(route, { cost: 0, estimatedDays: null });
    if (method === 'GET' && path.startsWith('/np/')) return ok(route, []);

    // ---------------- Адмін: замовлення ----------------
    if (method === 'GET' && path === '/admin/orders') {
      const size = Number(params.get('pageSize')) || 10;
      return ok(route, paginate(state.orders.map(orderSummary).reverse(), params.get('pageNumber'), size));
    }
    if (method === 'PUT' && /^\/admin\/orders\/\d+\/status$/.test(path)) {
      const id = Number(path.split('/')[3]);
      const order = state.orders.find((o) => o.orderId === id);
      if (!order) return fail(route, 404, 'Замовлення не знайдено');
      order.status = body.status;
      if (body.trackingNumber) order.trackingNumber = body.trackingNumber;
      // Для післяплати (COD) доставка завершує оплату
      if (body.status === 'Delivered' && order.paymentMethod === 'CashOnDelivery') {
        order.paymentStatus = 'Completed';
      }
      order.history.push({ historyId: order.history.length + 1, newStatus: body.status, oldStatus: null, changedAt: new Date().toISOString() });
      return ok(route, orderSummary(order));
    }

    // ---------------- Адмін: склад ----------------
    if (method === 'GET' && path === '/admin/incoming') {
      return ok(route, paginate(state.incoming, params.get('pageNumber'), params.get('pageSize')));
    }
    if (method === 'POST' && path === '/admin/incoming') {
      const product = state.products.find((p) => p.productId === Number(body.productId));
      const company = state.companies.find((c) => c.companyId === Number(body.companyId));
      const doc = {
        documentId: state.incoming.length + 1,
        productId: Number(body.productId),
        product: product ? { productId: product.productId, name: product.name } : null,
        quantity: Number(body.quantity),
        purchasePrice: Number(body.purchasePrice),
        reason: body.reason || 'INCOMING',
        companyId: body.companyId ? Number(body.companyId) : null,
        company: company ? { companyId: company.companyId, name: company.name } : null,
        documentDate: body.documentDate || new Date().toISOString(),
        notes: body.notes || null,
      };
      state.incoming.push(doc);
      if (product) product.stock += Number(body.quantity);
      return ok(route, doc);
    }
    if (method === 'PUT' && /^\/admin\/incoming\/\d+$/.test(path)) return ok(route, {});
    if (method === 'DELETE' && /^\/admin\/incoming\/\d+$/.test(path)) return ok(route, true);
    if (method === 'GET' && path === '/admin/outgoing') return ok(route, paginate([], params.get('pageNumber'), params.get('pageSize')));
    if (method === 'GET' && path === '/admin/stock/current') {
      const rows = state.products.map((p) => {
        const incoming = state.incoming.filter((d) => d.productId === p.productId).reduce((s, d) => s + d.quantity, 0);
        return {
          productId: p.productId,
          productName: p.name,
          totalIncoming: incoming,
          totalOutgoing: 0,
          currentStock: p.stock,
          lowStock: p.stock <= 3,
        };
      });
      return ok(route, rows);
    }
    if (method === 'GET' && path === '/admin/stock') return ok(route, []);
    if (method === 'GET' && path === '/admin/companies') return ok(route, state.companies);
    if (method === 'POST' && path === '/admin/companies') {
      const company = { companyId: state.nextCompanyId++, isActive: true, ...body };
      state.companies.push(company);
      return ok(route, company);
    }

    // ---------------- Адмін: товари та категорії ----------------
    if (method === 'POST' && path === '/admin/products') {
      // Клієнт надсилає categoryId (див. ProductsAdmin.handleSave)
      const category = state.categories.find((c) => c.categoryId === Number(body.categoryId));
      const product = {
        productId: state.nextProductId++,
        name: body.name,
        nameEn: body.nameEn || body.name,
        description: body.description,
        descriptionEn: body.descriptionEn || body.description,
        price: Number(body.price),
        originalPrice: null,
        weight: Number(body.weight),
        stock: Number(body.stock) || 0,
        categoryId: Number(body.categoryId) || null,
        category: category ? { categoryId: category.categoryId, name: category.name, nameEn: category.nameEn } : null,
        images: [],
        createdAt: new Date().toISOString(),
      };
      state.products.push(product);
      return ok(route, product);
    }
    if (method === 'POST' && path === '/admin/categories') {
      const category = {
        categoryId: state.nextCategoryId++,
        name: body.name,
        nameEn: body.nameEn || body.name,
        description: body.description || null,
        subCategories: [],
      };
      if (body.parentCategoryId) {
        const parent = state.categories.find((c) => c.categoryId === Number(body.parentCategoryId));
        if (parent) { parent.subCategories = parent.subCategories || []; parent.subCategories.push(category); }
      } else {
        state.categories.push(category);
      }
      return ok(route, category);
    }

    // ---------------- Аналітика (заглушка) ----------------
    if (method === 'GET' && path.startsWith('/Analytics/')) return ok(route, {});

    // ---------------- Невідомий ендпоінт ----------------
    console.warn(`[mockApi] Не оброблено: ${method} ${path}`);
    return fail(route, 404, `Mock API: невідомий ендпоінт ${method} ${path}`);
  });

  return state;
}

module.exports = { installMockApi };
