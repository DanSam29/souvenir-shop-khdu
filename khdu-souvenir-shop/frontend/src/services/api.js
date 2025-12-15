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

// API функції для Users
export const usersAPI = {
  // POST реєстрація
  register: (userData) => api.post('/Users/register', userData),
  
  // GET користувач за ID
  getById: (id) => api.get(`/Users/${id}`),
};

export default api;