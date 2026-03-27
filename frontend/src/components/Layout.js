import { Link, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';

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
  const { isAuthenticated, isAdmin, email, logout } = useAuth();
  const { cart } = useCart();

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
            {isAuthenticated && <NavItem to="/orders">My Orders</NavItem>}
            {isAuthenticated && <NavItem to="/cart">Cart ({cart.itemCount || 0})</NavItem>}
            {isAdmin && <NavItem to="/admin">Admin</NavItem>}
          </nav>

          <div className="auth-zone">
            {isAuthenticated ? (
              <>
                <span className="user-chip">{email}</span>
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
