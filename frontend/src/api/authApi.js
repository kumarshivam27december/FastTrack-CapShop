import { apiRequest } from './client';

export const authApi = {
  signup: (payload) => apiRequest('/auth/signup', { method: 'POST', body: payload }),
  login: (payload) => apiRequest('/auth/login', { method: 'POST', body: payload }),
  loginStep1: (payload) => apiRequest('/auth/login-step1', { method: 'POST', body: payload }),
  sendOtp: (payload) => apiRequest('/auth/send-otp', { method: 'POST', body: payload }),
  verifyOtp: (payload) => apiRequest('/auth/verify-otp', { method: 'POST', body: payload }),
  setupAuthenticator: (payload) => apiRequest('/auth/setup-authenticator', { method: 'POST', body: payload }),
  verifyAuthenticator: (payload) => apiRequest('/auth/verify-authenticator', { method: 'POST', body: payload }),
  me: (token) => apiRequest('/auth/me', { token }),
  updateMe: (token, payload) => apiRequest('/auth/me', { method: 'PUT', token, body: payload })
};
