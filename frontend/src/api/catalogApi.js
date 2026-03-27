import { apiRequest } from './client';

function toQueryString(params) {
  const searchParams = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      searchParams.append(key, String(value));
    }
  });

  const query = searchParams.toString();
  return query ? `?${query}` : '';
}

export const catalogApi = {
  getFeatured: () => apiRequest('/catalog/featured'),
  searchProducts: (params) => apiRequest(`/catalog/products${toQueryString(params)}`),
  getProductById: (id) => apiRequest(`/catalog/products/${id}`),
  createProduct: (token, payload) =>
    apiRequest('/catalog/admin/products', { method: 'POST', token, body: payload }),
  updateProduct: (token, id, payload) =>
    apiRequest(`/catalog/admin/products/${id}`, { method: 'PUT', token, body: payload }),
  updateStock: (token, id, stock) =>
    apiRequest(`/catalog/admin/products/${id}/stock`, { method: 'PUT', token, body: { stock } }),
  deleteProduct: (token, id) => apiRequest(`/catalog/admin/products/${id}`, { method: 'DELETE', token })
};
