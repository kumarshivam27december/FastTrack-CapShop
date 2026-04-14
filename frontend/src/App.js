import { BrowserRouter, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import AdminRoute from './components/AdminRoute';
import CustomerRoute from './components/CustomerRoute';
import HomePage from './pages/HomePage';
import CatalogPage from './pages/CatalogPage';
import CatalogAssistantPage from './pages/CatalogAssistantPage';
import ProductDetailPage from './pages/ProductDetailPage';
import LoginPage from './pages/LoginPage';
import SignupPage from './pages/SignupPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import TwoFactorMethodPage from './pages/TwoFactorMethodPage';
import TwoFactorVerifyPage from './pages/TwoFactorVerifyPage';
import TwoFactorAuthenticatorPage from './pages/TwoFactorAuthenticatorPage';
import CartPage from './pages/CartPage';
import CheckoutPage from './pages/CheckoutPage';
import OrdersPage from './pages/OrdersPage';
import OrderDetailsPage from './pages/OrderDetailsPage';
import CancelOrderConfirmPage from './pages/CancelOrderConfirmPage';
import ProfilePage from './pages/ProfilePage';
import AdminShell from './components/AdminShell';
import AdminOverviewPage from './pages/AdminOverviewPage';
import AdminOrdersPage from './pages/AdminOrdersPage';
import AdminProductsPage from './pages/AdminProductsPage';
import AdminCategoriesPage from './pages/AdminCategoriesPage';
import AdminReportsPage from './pages/AdminReportsPage';
import NotFoundPage from './pages/NotFoundPage';
import { AuthProvider } from './context/AuthContext';
import { CartProvider } from './context/CartContext';
import { ThemeProvider } from './context/ThemeContext';

function App() {
  return (
    <ThemeProvider>
      <BrowserRouter>
        <AuthProvider>
          <CartProvider>
            <Routes>
              <Route path="/" element={<Layout />}>
                <Route index element={<HomePage />} />
                <Route path="catalog" element={<CatalogPage />} />
                <Route
                  path="assistant"
                  element={(
                    <ProtectedRoute>
                      <CatalogAssistantPage />
                    </ProtectedRoute>
                  )}
                />
                <Route path="products/:id" element={<ProductDetailPage />} />
                <Route path="login" element={<LoginPage />} />
                <Route path="signup" element={<SignupPage />} />
                <Route path="forgot-password" element={<ForgotPasswordPage />} />
                <Route path="two-factor-method" element={<TwoFactorMethodPage />} />
                <Route path="two-factor-verify" element={<TwoFactorVerifyPage />} />
                <Route path="two-factor-authenticator" element={<TwoFactorAuthenticatorPage />} />
                <Route
                  path="cart"
                  element={(
                    <CustomerRoute>
                      <CartPage />
                    </CustomerRoute>
                  )}
                />
                <Route
                  path="checkout"
                  element={(
                    <CustomerRoute>
                      <CheckoutPage />
                    </CustomerRoute>
                  )}
                />
                <Route
                  path="orders"
                  element={(
                    <CustomerRoute>
                      <OrdersPage />
                    </CustomerRoute>
                  )}
                />
                <Route
                  path="orders/:id"
                  element={(
                    <CustomerRoute>
                      <OrderDetailsPage />
                    </CustomerRoute>
                  )}
                />
                <Route
                  path="orders/:id/cancel"
                  element={(
                    <CustomerRoute>
                      <CancelOrderConfirmPage />
                    </CustomerRoute>
                  )}
                />
                <Route
                  path="profile"
                  element={(
                    <ProtectedRoute>
                      <ProfilePage />
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
                  <Route path="categories" element={<AdminCategoriesPage />} />
                  <Route path="reports" element={<AdminReportsPage />} />
                </Route>
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </Routes>
          </CartProvider>
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
