import { NavLink, Outlet } from 'react-router-dom';

function SideItem({ to, children }) {
  return (
    <NavLink to={to} className={({ isActive }) => `admin-side-link ${isActive ? 'active' : ''}`}>
      {children}
    </NavLink>
  );
}

export default function AdminShell() {
  return (
    <section className="section admin-shell-wrap">
      <aside className="card admin-sidebar">
        <h2>Admin Panel</h2>
        <nav className="admin-side-nav">
          <SideItem to="/admin">Overview</SideItem>
          <SideItem to="/admin/orders">Orders</SideItem>
          <SideItem to="/admin/products">Products</SideItem>
          <SideItem to="/admin/categories">Categories</SideItem>
          <SideItem to="/admin/reports">Reports</SideItem>
        </nav>
      </aside>

      <div className="admin-main">
        <Outlet />
      </div>
    </section>
  );
}
