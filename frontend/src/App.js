import { BrowserRouter, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import AdminRoute from './components/AdminRoute';
import HomePage from './pages/HomePage';
import CatalogPage from './pages/CatalogPage';
import ProductDetailPage from './pages/ProductDetailPage';
import LoginPage from './pages/LoginPage';
import SignupPage from './pages/SignupPage';
import CartPage from './pages/CartPage';
import CheckoutPage from './pages/CheckoutPage';
import OrdersPage from './pages/OrdersPage';
import OrderDetailsPage from './pages/OrderDetailsPage';
import CancelOrderConfirmPage from './pages/CancelOrderConfirmPage';
import AdminShell from './components/AdminShell';
import AdminOverviewPage from './pages/AdminOverviewPage';
import AdminOrdersPage from './pages/AdminOrdersPage';
import AdminProductsPage from './pages/AdminProductsPage';
import AdminReportsPage from './pages/AdminReportsPage';
import NotFoundPage from './pages/NotFoundPage';
import { AuthProvider } from './context/AuthContext';
import { CartProvider } from './context/CartContext';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <CartProvider>
          <Routes>
            <Route path="/" element={<Layout />}>
              <Route index element={<HomePage />} />
              <Route path="catalog" element={<CatalogPage />} />
              <Route path="products/:id" element={<ProductDetailPage />} />
              <Route path="login" element={<LoginPage />} />
              <Route path="signup" element={<SignupPage />} />
              <Route
                path="cart"
                element={(
                  <ProtectedRoute>
                    <CartPage />
                  </ProtectedRoute>
                )}
              />
              <Route
                path="checkout"
                element={(
                  <ProtectedRoute>
                    <CheckoutPage />
                  </ProtectedRoute>
                )}
              />
              <Route
                path="orders"
                element={(
                  <ProtectedRoute>
                    <OrdersPage />
                  </ProtectedRoute>
                )}
              />
              <Route
                path="orders/:id"
                element={(
                  <ProtectedRoute>
                    <OrderDetailsPage />
                  </ProtectedRoute>
                )}
              />
              <Route
                path="orders/:id/cancel"
                element={(
                  <ProtectedRoute>
                    <CancelOrderConfirmPage />
                  </ProtectedRoute>
                )}
              />
              <Route
                path="admin"
                element={(
                  <ProtectedRoute>
                    <AdminRoute>
                      <AdminShell />
                    </AdminRoute>
                  </ProtectedRoute>
                )}
              >
                <Route index element={<AdminOverviewPage />} />
                <Route path="orders" element={<AdminOrdersPage />} />
                <Route path="products" element={<AdminProductsPage />} />
                <Route path="reports" element={<AdminReportsPage />} />
              </Route>
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </CartProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
