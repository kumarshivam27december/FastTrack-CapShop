import { apiRequest } from './client';

export const reviewApi = {
  getProductReviews: (productId) => apiRequest(`/reviews/products/${productId}`),
  getProductSummary: (productId, token) =>
    apiRequest(`/reviews/products/${productId}/summary`, token ? { token } : {}),
  createReview: (token, productId, payload) =>
    apiRequest(`/reviews/products/${productId}`, { method: 'POST', token, body: payload }),
  updateReview: (token, reviewId, payload) =>
    apiRequest(`/reviews/${reviewId}`, { method: 'PUT', token, body: payload }),
  deleteReview: (token, reviewId) =>
    apiRequest(`/reviews/${reviewId}`, { method: 'DELETE', token }),
  getMyReviews: (token) => apiRequest('/reviews/my', { token }),
  getMyEligibilities: (token) => apiRequest('/reviews/my/eligibilities', { token }),
  getPendingReviews: (token) => apiRequest('/reviews/admin/pending', { token }),
  approveReview: (token, reviewId) =>
    apiRequest(`/reviews/admin/${reviewId}/approve`, { method: 'PUT', token }),
  rejectReview: (token, reviewId) =>
    apiRequest(`/reviews/admin/${reviewId}/reject`, { method: 'PUT', token })
};
