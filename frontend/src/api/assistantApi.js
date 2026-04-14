import { apiRequest } from './client';

export const assistantApi = {
  queryCatalog: (token, message) =>
    apiRequest('/catalog/assistant/query', {
      method: 'POST',
      token,
      body: { message, pageSize: 20, returnAllMatches: true }
    })
};