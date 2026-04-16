import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import AdminRoute from './AdminRoute';
import { useAuth } from '../context/AuthContext';

jest.mock('../context/AuthContext', () => ({
  useAuth: jest.fn()
}));

function renderAdminRoute(initialPath = '/admin') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route path="/" element={<div>Home Page</div>} />
        <Route
          path="/admin"
          element={(
            <AdminRoute>
              <div>Admin Content</div>
            </AdminRoute>
          )}
        />
      </Routes>
    </MemoryRouter>
  );
}

describe('AdminRoute', () => {
  test('redirects non-admin users to home page', () => {
    useAuth.mockReturnValue({
      isAdmin: false
    });

    renderAdminRoute('/admin');

    expect(screen.getByText(/Home Page/i)).toBeInTheDocument();
  });

  test('renders admin content for admin users', () => {
    useAuth.mockReturnValue({
      isAdmin: true
    });

    renderAdminRoute('/admin');

    expect(screen.getByText(/Admin Content/i)).toBeInTheDocument();
  });
});