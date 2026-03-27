import { apiRequest } from './client';

export const authApi = {
  signup: (payload) => apiRequest('/auth/signup', { method: 'POST', body: payload }),
  login: (payload) => apiRequest('/auth/login', { method: 'POST', body: payload }),
  me: (token) => apiRequest('/auth/me', { token })
};
