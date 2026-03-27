import { apiRequest } from './client';

export const orderApi = {
  getCart: (token) => apiRequest('/orders/cart', { token }),
  addToCart: (token, payload) => apiRequest('/orders/cart/items', { method: 'POST', token, body: payload }),
  updateCartItem: (token, itemId, payload) =>
    apiRequest(`/orders/cart/items/${itemId}`, { method: 'PUT', token, body: payload }),
  removeCartItem: (token, itemId) => apiRequest(`/orders/cart/items/${itemId}`, { method: 'DELETE', token }),
  startCheckout: (token, payload) =>
    apiRequest('/orders/checkout/start', { method: 'POST', token, body: payload }),
  simulatePayment: (token, payload) =>
    apiRequest('/orders/payment/simulate', { method: 'POST', token, body: payload }),
  placeOrder: (token, orderId) => apiRequest('/orders/place', { method: 'POST', token, body: { orderId } }),
  cancelOrder: (token, id) => apiRequest(`/orders/${id}/cancel`, { method: 'PUT', token }),
  getMyOrders: (token) => apiRequest('/orders/my', { token }),
  getOrderById: (token, id) => apiRequest(`/orders/${id}`, { token })
};
