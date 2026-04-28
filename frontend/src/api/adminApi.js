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

// Upload catalog file (CSV or JSON array) using multipart/form-data
adminApi.uploadCatalog = async (token, file) => {
  const { API_BASE_URL } = await import('./client');
  const url = `${API_BASE_URL}/admin/catalog/upload`;
  const fd = new FormData();
  fd.append('file', file);

  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;

  const resp = await fetch(url, { method: 'POST', headers, body: fd });
  if (!resp.ok) {
    const text = await resp.text();
    throw new Error(text || `Upload failed (${resp.status})`);
  }

  return resp.json();
};
