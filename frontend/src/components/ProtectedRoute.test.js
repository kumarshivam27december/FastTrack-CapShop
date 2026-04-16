import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProtectedRoute from './ProtectedRoute';
import { useAuth } from '../context/AuthContext';

jest.mock('../context/AuthContext', () => ({
  useAuth: jest.fn()
}));

function renderProtectedRoute(initialPath = '/orders') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route path="/login" element={<div>Login Page</div>} />
        <Route
          path="*"
          element={(
            <ProtectedRoute>
              <div>Protected Content</div>
            </ProtectedRoute>
          )}
        />
      </Routes>
    </MemoryRouter>
  );
}

describe('ProtectedRoute', () => {
  test('shows loading while auth is initializing', () => {
    useAuth.mockReturnValue({
      initialized: false,
      isAuthenticated: false
    });

    renderProtectedRoute();

    expect(screen.getByText(/Checking session.../i)).toBeInTheDocument();
  });

  test('redirects to login when user is not authenticated', () => {
    useAuth.mockReturnValue({
      initialized: true,
      isAuthenticated: false
    });

    renderProtectedRoute('/orders');

    expect(screen.getByText(/Login Page/i)).toBeInTheDocument();
  });

  test('renders children when user is authenticated', () => {
    useAuth.mockReturnValue({
      initialized: true,
      isAuthenticated: true
    });

    renderProtectedRoute('/orders');

    expect(screen.getByText(/Protected Content/i)).toBeInTheDocument();
  });
});