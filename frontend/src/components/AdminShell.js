import { NavLink, Outlet } from 'react-router-dom';

function SideItem({ to, children, end = false }) {
  return (
    <NavLink end={end} to={to} className={({ isActive }) => `admin-side-link ${isActive ? 'active' : ''}`}>
      {children}
    </NavLink>
  );
}

export default function AdminShell() {
  return (
    <section className="section admin-shell-wrap">
      <aside className="card admin-sidebar">
        <div className="admin-sidebar-header">
          <span className="admin-sidebar-chip">Admin Mode</span>
          <h2>Dashboard</h2>
        </div>
        <nav className="admin-side-nav">
          <SideItem end to="/admin">Dashboard</SideItem>
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
