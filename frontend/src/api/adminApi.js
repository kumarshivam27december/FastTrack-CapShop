import { apiRequest } from './client';

export const adminApi = {
  getSummary: (token) => apiRequest('/admin/dashboard/summary', { token }),
  getOrders: (token) => apiRequest('/admin/orders', { token }),
  updateOrderStatus: (token, orderId, payload) =>
    apiRequest(`/admin/orders/${orderId}/status`, { method: 'PUT', token, body: payload }),
  getStatusSplit: (token) => apiRequest('/admin/reports/status-split', { token }),
  getSalesReport: (token, from, to) =>
    apiRequest(`/admin/reports/sales?from=${from}&to=${to}`, { token }),
  exportSalesCsv: (token, from, to) =>
    apiRequest(`/admin/reports/export/csv?from=${from}&to=${to}`, { token, responseType: 'blob' }),
  
  // Categories
  getCategories: () => apiRequest('/catalog/categories'),
  getCategoryById: (id) => apiRequest(`/catalog/categories/${id}`),
  createCategory: (token, payload) =>
    apiRequest('/catalog/categories', { method: 'POST', token, body: payload }),
  updateCategory: (token, id, payload) =>
    apiRequest(`/catalog/categories/${id}`, { method: 'PUT', token, body: payload }),
  deleteCategory: (token, id) =>
    apiRequest(`/catalog/categories/${id}`, { method: 'DELETE', token })
};
