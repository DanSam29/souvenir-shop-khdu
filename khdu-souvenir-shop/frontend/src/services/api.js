import axios from 'axios';

// Базова URL твого backend API
const API_BASE_URL = 'http://localhost:5225/api';

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
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Токен невалідний або закінчився
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

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

// API функції для Orders (потребують авторизації)
export const ordersAPI = {
  checkout: (payload) => api.post('/Orders/checkout', payload),
  getMy: () => api.get('/Orders/my'),
  getById: (id) => api.get(`/Orders/${id}`),
};

// Адмін API для Products
export const adminProductsAPI = {
  create: (data) => api.post('/admin/products', data),
  update: (id, data) => api.put(`/admin/products/${id}`, data),
  delete: (id) => api.delete(`/admin/products/${id}`),
};

export default api;
