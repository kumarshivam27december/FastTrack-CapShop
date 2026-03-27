const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:5041/gateway';

function buildHeaders(token, hasBody) {
  const headers = {};

  if (hasBody) {
    headers['Content-Type'] = 'application/json';
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  return headers;
}

async function parseResponse(response, responseType) {
  if (responseType === 'blob') {
    return response.blob();
  }

  const text = await response.text();
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

export async function apiRequest(path, options = {}) {
  const {
    method = 'GET',
    body,
    token,
    responseType = 'json'
  } = options;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers: buildHeaders(token, body !== undefined),
    body: body !== undefined ? JSON.stringify(body) : undefined
  });

  const payload = await parseResponse(response, responseType);

  if (!response.ok) {
    const message =
      (payload && typeof payload === 'object' && payload.message) ||
      (typeof payload === 'string' && payload) ||
      `Request failed (${response.status})`;

    const error = new Error(message);
    error.status = response.status;
    error.payload = payload;
    throw error;
  }

  return payload;
}

export { API_BASE_URL };
