import { Link, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { useTheme } from '../context/ThemeContext';

function NavItem({ to, children }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
    >
      {children}
    </NavLink>
  );
}

export default function Layout() {
  const {
    isAuthenticated,
    isAdmin,
    email,
    fullName,
    avatarUrl,
    logout
  } = useAuth();
  const { cart } = useCart();
  const { isDarkMode, toggleTheme } = useTheme();
  const avatarSourceName = (fullName || email || '').trim();
  const avatarInitial = (avatarSourceName || '?').charAt(0).toUpperCase();

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="container topbar-inner">
          <Link to="/" className="brand">
            <span className="brand-mark">C</span>
            <span>CapShop</span>
          </Link>

          <nav className="topnav">
            <NavItem to="/">Home</NavItem>
            <NavItem to="/catalog">Catalog</NavItem>
            {isAuthenticated && !isAdmin && <NavItem to="/orders">My Orders</NavItem>}
            {isAuthenticated && !isAdmin && <NavItem to="/cart">Cart ({cart.itemCount || 0})</NavItem>}
            {isAdmin && <NavItem to="/admin">Admin</NavItem>}
          </nav>

          <div className="auth-zone">
            <button
              type="button"
              className="btn btn-outline theme-toggle-btn"
              onClick={toggleTheme}
              aria-label={`Switch to ${isDarkMode ? 'light' : 'dark'} mode`}
              title={`Switch to ${isDarkMode ? 'light' : 'dark'} mode`}
            >
              <span aria-hidden="true" className="theme-toggle-symbol">
                {isDarkMode ? '\u2600' : '\u263D'}
              </span>
            </button>
            {isAuthenticated ? (
              <>
                <Link
                  to="/profile"
                  className="profile-avatar-link"
                  aria-label="Open profile"
                  title="Open profile"
                >
                  {avatarUrl ? (
                    <img src={avatarUrl} alt="Profile" className="profile-avatar profile-avatar-image" />
                  ) : (
                    <span className="profile-avatar" aria-hidden="true">{avatarInitial}</span>
                  )}
                </Link>
                <button type="button" className="btn btn-outline" onClick={logout}>
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="btn btn-outline">Login</Link>
                <Link to="/signup" className="btn btn-solid">Signup</Link>
              </>
            )}
          </div>
        </div>
      </header>

      <main className="container main-content">
        <Outlet />
      </main>

      <footer className="footer container">
        <p>CapShop Microservices Frontend</p>
      </footer>
    </div>
  );
}
