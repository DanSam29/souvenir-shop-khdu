import axios from 'axios';

// Базова URL твого backend API
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5225/api';

// Створюємо axios instance з базовою конфігурацією
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor для додавання JWT токену до кожного запиту
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Interceptor для обробки помилок (наприклад, 401 - неавторизовано)
api.interceptors.response.use(
  (response) => {
    // Якщо відповідь успішна і містить `data` та `success: true`, повертаємо тільки дані
    if (response.data && response.data.success) {
      return response.data;
    }
    // В іншому випадку повертаємо повну відповідь
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Токен невалідний або закінчився
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Побудова повного URL для зображень, якщо у БД зберігається відносний шлях
const IMAGES_BASE_URL = API_BASE_URL.replace(/\/api\/?$/, '');
export const buildImageUrl = (path) => {
  if (!path) return null;
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  if (path.startsWith('/')) return `${IMAGES_BASE_URL}${path}`;
  return `${IMAGES_BASE_URL}/${path}`;
};

// API функції для Products
export const productsAPI = {
  // GET всі товари
  getAll: () => api.get('/Products'),
  
  // GET товар за ID
  getById: (id) => api.get(`/Products/${id}`),
  
  // GET пошук товарів
  search: (query) => api.get(`/Products/search?query=${query}`),
  
  // GET товари за категорією
  getByCategory: (categoryId) => api.get(`/Products/category/${categoryId}`),
};

// API функції для Categories
export const categoriesAPI = {
  // GET всі категорії
  getAll: () => api.get('/Categories'),
  
  // GET категорія за ID
  getById: (id) => api.get(`/Categories/${id}`),
};

// Адмін API для Categories
export const adminCategoriesAPI = {
  create: (data) => api.post('/admin/categories', data),
  update: (id, data) => api.put(`/admin/categories/${id}`, data),
  delete: (id) => api.delete(`/admin/categories/${id}`),
};

// API функції для Users
export const usersAPI = {
  // POST реєстрація
  register: (userData) => api.post('/Users/register', userData),
  
  // POST авторизація
  login: (credentials) => api.post('/Users/login', credentials),
  
  // GET поточний користувач (потребує токену)
  getCurrentUser: () => api.get('/Users/me'),
  
  // GET користувач за ID
  getById: (id) => api.get(`/Users/${id}`),
  
  updateProfile: (data) => api.put('/Users/me', data),
  changePassword: (data) => api.post('/Users/change-password', data),
};

// API функції для Cart (потребують авторизації)
export const cartAPI = {
  // GET отримати кошик
  getCart: () => api.get('/Cart'),
  
  // POST додати товар до кошика
  addToCart: (productId, quantity = 1) => api.post('/Cart/add', { productId, quantity }),
  
  // PUT оновити кількість товару
  updateQuantity: (cartItemId, quantity) => api.put(`/Cart/update/${cartItemId}`, { quantity }),
  
  // DELETE видалити товар з кошика
  removeFromCart: (cartItemId) => api.delete(`/Cart/remove/${cartItemId}`),
  
  // DELETE очистити кошик
  clearCart: () => api.delete('/Cart/clear'),
};

// API функції для замовлень
export const ordersAPI = {
  checkout: (payload) => api.post('/Orders/checkout', payload),
  calculate: (payload) => api.post('/Orders/calculate', payload),
  getOrders: () => api.get('/Orders/my'),
  getOrder: (id) => api.get(`/Orders/${id}`),
  // Адмінські методи
  getAll: (params) => api.get('/Orders/admin', { params }),
  updateStatus: (id, data) => api.patch(`/Orders/${id}/status`, data),
  cancel: (id, reason) => api.post(`/Orders/${id}/cancel`, reason),
};

// API функції для користувачів (адмін)
export const adminUsersAPI = {
  getAll: (params) => api.get('/Users', { params }),
  updateRole: (id, role) => api.patch(`/Users/${id}/role`, { role }),
  toggleBlock: (id) => api.post(`/Users/${id}/toggle-block`),
};

// API функції для складу
export const warehouseAPI = {
  getIncoming: () => api.get('/WarehouseDocuments/incoming'),
  getOutgoing: () => api.get('/WarehouseDocuments/outgoing'),
  getStock: () => api.get('/WarehouseDocuments/stock'),
  createIncoming: (data) => api.post('/WarehouseDocuments/incoming', data),
  createOutgoing: (data) => api.post('/WarehouseDocuments/outgoing', data),
  getCompanies: (onlyActive) => api.get('/Companies', { params: { onlyActive } }),
  createCompany: (data) => api.post('/Companies', data),
  updateCompany: (id, data) => api.put(`/Companies/${id}`, data),
  deleteCompany: (id) => api.delete(`/Companies/${id}`),
};

// API функції для інтеграцій
export const integrationsAPI = {
  getStatus: () => api.get('/Integrations/status'),
  testStripe: () => api.get('/Integrations/test/stripe'),
  testNovaPoshta: () => api.get('/Integrations/test/novaposhta'),
};

// API функції для аналітики
export const analyticsAPI = {
  getSummary: (params) => api.get('/Analytics/summary', { params }),
  exportSales: (params) => api.get('/Analytics/export/sales', { params, responseType: 'blob' }),
};

// Адмін API для Products
export const adminProductsAPI = {
  create: (data) => api.post('/admin/products', data),
  update: (id, data) => api.put(`/admin/products/${id}`, data),
  delete: (id) => api.delete(`/admin/products/${id}`),
};

// API функції для Nova Poshta
export const novaPoshtaAPI = {
  getCities: (q) => api.get(`/NovaPoshta/cities?q=${q || ''}`),
  getWarehouses: (cityRef, q) => api.get(`/NovaPoshta/warehouses?cityRef=${cityRef}&q=${q || ''}`),
  calculate: (cityRef, weight, totalAmount) => api.get(`/NovaPoshta/calculate?cityRef=${cityRef}&weight=${weight}&totalAmount=${totalAmount}`),
};

// API функції для акцій та промокодів
export const promotionsAPI = {
  // Отримати доступні акції для користувача
  getMyPromotions: () => api.get('/Promotions/my'),
  
  // Перевірити промокод
  validatePromoCode: (code, totalAmount) => api.get(`/Promotions/validate?code=${code}&totalAmount=${totalAmount}`),
  
  // Адмінські методи
  getAll: (params) => api.get('/Promotions', { params }),
  getById: (id) => api.get(`/Promotions/${id}`),
  create: (data) => api.post('/Promotions', data),
  update: (id, data) => api.put(`/Promotions/${id}`, data),
  delete: (id) => api.delete(`/Promotions/${id}`),
  toggleActive: (id) => api.patch(`/Promotions/${id}/toggle`),
};

export default api;
