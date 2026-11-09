import axios from 'axios';

// Базова URL твого backend API
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5225/api';
// Створення axios instance з базовою конфігурацією
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});
// Interceptor для додавання JWT токену до кожного запиту
api.interceptors.request.use(
  (config) => {
    // Токен зберігається в localStorage ("Запам'ятати мене") або в sessionStorage (до закриття вкладки)
    const token = localStorage.getItem('token') || sessionStorage.getItem('token');
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
    // Якщо відповідь успішна і містить `data` та `success: true` - повернення тільки данних
    if (response.data && response.data.success) {
      return response.data;
    }
    // В іншому випадку повернення повної відповіді
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Токен невалідний або закінчився
      localStorage.removeItem('token');
      sessionStorage.removeItem('token');
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
  // GET товари (пагінація, фільтри, сортування)
  getAll: (params) => api.get('/Products', { params }),
  // GET товар за ID
  getById: (id) => api.get(`/Products/${id}`),
  // GET пошук товарів
  search: (query) => api.get(`/Products/search?query=${query}`),
  // GET рекомендовані/популярні товари
  getFeatured: (limit = 8) => api.get('/Products/featured', { params: { limit } }),
  // GET новинки каталогу
  getNew: (limit = 8) => api.get('/Products/new', { params: { limit } }),
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
  // POST зміна порядку відображення (drag-and-drop)
  reorder: (items) => api.post('/admin/categories/reorder', items),
  // POST масове видалення
  bulkDelete: (ids) => api.post('/admin/categories/bulk-delete', ids),
};
// API функції для Users
export const usersAPI = {
  // POST реєстрація
  register: (userData) => api.post('/Users/register', userData),
  // POST авторизація
  login: (credentials) => api.post('/Users/login', credentials),
  // GET поточний користувач (потрібен токен)
  getCurrentUser: () => api.get('/Users/me'),
  // GET користувач за ID
  getById: (id) => api.get(`/Users/${id}`),
  updateProfile: (data) => api.put('/Users/me', data),
  changePassword: (data) => api.post('/Users/change-password', data),
  // Збережені адреси доставки
  getAddresses: () => api.get('/Users/me/addresses'),
  createAddress: (data) => api.post('/Users/me/addresses', data),
  updateAddress: (id, data) => api.put(`/Users/me/addresses/${id}`, data),
  deleteAddress: (id) => api.delete(`/Users/me/addresses/${id}`),
  setDefaultAddress: (id) => api.post(`/Users/me/addresses/${id}/default`),
  // Повторна верифікація студентського статусу
  verifyStudentStatus: () => api.post('/Users/me/student/verify'),
};
// API функції для Cart (потребують авторизації)
export const cartAPI = {
  // GET отримати кошик
  getCart: () => api.get('/Cart'),
  // POST додати товар до кошика
  addToCart: (productId, quantity = 1) => api.post('/Cart/items', { productId, quantity }),
  // POST масове додавання товарів до кошика
  addBulk: (items) => api.post('/Cart/items/bulk', { items }),
  // PUT оновити кількість товару (quantity = 0 видаляє позицію)
  updateQuantity: (cartItemId, quantity) => api.put(`/Cart/items/${cartItemId}`, { quantity }),
  // DELETE видалити товар з кошика
  removeFromCart: (cartItemId) => api.delete(`/Cart/items/${cartItemId}`),
  // DELETE очистити кошик
  clearCart: () => api.delete('/Cart'),
};
// API функції для замовлень
export const ordersAPI = {
  checkout: (payload) => api.post('/Orders', payload),
  calculate: (payload) => api.post('/Orders/calculate', payload),
  verifyPayment: (sessionId) => api.get(`/Orders/verify-payment?sessionId=${sessionId}`),
  getOrders: () => api.get('/Orders/my'),
  getOrder: (id) => api.get(`/Orders/${id}`),
  // Деталі власного замовлення з історією зміни статусів
  getMyOrder: (id) => api.get(`/Users/me/orders/${id}`),
  // Методи адміністратора
  getAll: (params) => api.get('/admin/orders', { params }),
  getAdminOrder: (id) => api.get(`/admin/orders/${id}`),
  updateStatus: (id, data) => api.put(`/admin/orders/${id}/status`, data),
  cancel: (id, reason) => api.post(`/Orders/${id}/cancel`, { reason }),
};
// API функції для користувачів (адмін)
export const adminUsersAPI = {
  getAll: (params) => api.get('/admin/users', { params }),
  getById: (id) => api.get(`/admin/users/${id}`),
  getOrders: (id, params) => api.get(`/admin/users/${id}/orders`, { params }),
  updateRole: (id, data) => api.put(`/admin/users/${id}/role`, data),
  block: (id) => api.put(`/admin/users/${id}/block`),
  unblock: (id) => api.put(`/admin/users/${id}/unblock`),
  resetPassword: (id, data) => api.post(`/admin/users/${id}/reset-password`, data),
};
// API функції для складу (адмін, Manager+)
export const warehouseAPI = {
  getIncoming: (params) => api.get('/admin/incoming', { params }),
  getIncomingById: (id) => api.get(`/admin/incoming/${id}`),
  createIncoming: (data) => api.post('/admin/incoming', data),
  updateIncoming: (id, data) => api.put(`/admin/incoming/${id}`, data),
  deleteIncoming: (id) => api.delete(`/admin/incoming/${id}`),
  getOutgoing: (params) => api.get('/admin/outgoing', { params }),
  getOutgoingById: (id) => api.get(`/admin/outgoing/${id}`),
  createOutgoing: (data) => api.post('/admin/outgoing', data),
  updateOutgoing: (id, data) => api.put(`/admin/outgoing/${id}`, data),
  deleteOutgoing: (id) => api.delete(`/admin/outgoing/${id}`),
  getStock: () => api.get('/admin/stock'),
};
// API функції для компаній
export const companiesAPI = {
  getAll: (onlyActive) => api.get('/Companies', { params: { onlyActive } }),
  getById: (id) => api.get(`/Companies/${id}`),
  create: (data) => api.post('/Companies', data),
  update: (id, data) => api.put(`/Companies/${id}`, data),
  delete: (id) => api.delete(`/Companies/${id}`),
};
// API функції для інтеграцій
export const integrationsAPI = {
  getStatus: () => api.get('/Integrations/status'),
  testStripe: () => api.get('/Integrations/test/stripe'),
  testNovaPoshta: () => api.get('/Integrations/test/novaposhta'),
};
// Публічні API функції для фічефлагів
export const featuresAPI = {
  getStatus: () => api.get('/Features/status'),
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
  // POST масове видалення
  bulkDelete: (ids) => api.post('/admin/products/bulk-delete', ids),
  // POST завантаження зображення (multipart)
  uploadImage: (productId, formData, isPrimary = false) => api.post(`/admin/products/${productId}/images`, formData, {
    params: { isPrimary },
    headers: { 'Content-Type': 'multipart/form-data' }
  }),
  // DELETE видалення зображення
  deleteImage: (productId, imageId) => api.delete(`/admin/products/${productId}/images/${imageId}`),
  // PUT зміна порядку зображень
  reorderImages: (productId, items) => api.put(`/admin/products/${productId}/images/reorder`, items),
  // PUT встановлення головного зображення
  setPrimaryImage: (productId, imageId) => api.put(`/admin/products/${productId}/images/${imageId}/primary`),
};
// API функції для Nova Poshta
export const novaPoshtaAPI = {
  getCities: (search) => api.get(`/np/cities?search=${search || ''}`),
  getWarehouses: (cityRef, search) => api.get(`/np/warehouses?cityRef=${cityRef}&search=${search || ''}`),
  calculate: (payload) => api.post('/np/calculate', payload),
  getTracking: (trackingNumber) => api.get(`/np/tracking/${encodeURIComponent(trackingNumber)}`),
};
// API функції для акцій та промокодів
export const promotionsAPI = {
  // Публічний список активних загальних акцій
  getActive: () => api.get('/Promotions/active'),
  // Доступні поточному користувачу акції (персональні та за статусом)
  getMyPromotions: () => api.get('/Promotions/my'),
  // Історія призначених/використаних знижок користувача
  getMyPromotionsHistory: () => api.get('/Users/me/promotions/history'),
  // Валідація промокоду під час оформлення замовлення (POST { code })
  validatePromoCode: (code) => api.post('/Promotions/validate', { code }),
  // Методи адміністратора
  getAll: (params) => api.get('/admin/promotions', { params }),
  getById: (id) => api.get(`/admin/promotions/${id}`),
  create: (data) => api.post('/admin/promotions', data),
  update: (id, data) => api.put(`/admin/promotions/${id}`, data),
  patch: (id, data) => api.patch(`/admin/promotions/${id}`, data),
  delete: (id) => api.delete(`/admin/promotions/${id}`),
  bulkDelete: (ids) => api.post('/admin/promotions/bulk-delete', ids),
  toggleActive: (id) => api.patch(`/admin/promotions/${id}/toggle`),
  // Персональне призначення акції користувачу
  assign: (id, userId) => api.post(`/admin/promotions/${id}/assign/${userId}`),
  // Статистика ROI акції
  getStatistics: (id) => api.get(`/admin/promotions/${id}/statistics`),
  // Експорт статистики акції у CSV/PDF
  exportStatisticsCsv: (id) => api.get(`/admin/promotions/${id}/statistics/export/csv`, { responseType: 'blob' }),
  exportStatisticsPdf: (id) => api.get(`/admin/promotions/${id}/statistics/export/pdf`, { responseType: 'blob' }),
};

export default api;