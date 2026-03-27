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
    apiRequest(`/admin/reports/export/csv?from=${from}&to=${to}`, { token, responseType: 'blob' })
};
