import { useEffect, useState } from 'react';
import { adminApi } from '../api/adminApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

export default function AdminOverviewPage() {
  const { token } = useAuth();
  const [summary, setSummary] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      try {
        const data = await adminApi.getSummary(token);
        setSummary(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [token]);

  if (loading) {
    return <LoadingSpinner label="Loading overview..." />;
  }

  return (
    <div>
      <h1>Admin Overview</h1>
      {error && <p className="message error">{error}</p>}

      {summary && (
        <>
          <div className="summary-grid">
            <article className="card metric">
              <h3>Total Orders</h3>
              <p>{summary.totalOrders}</p>
            </article>
            <article className="card metric">
              <h3>Orders Today</h3>
              <p>{summary.ordersToday}</p>
            </article>
            <article className="card metric">
              <h3>Total Revenue</h3>
              <p>Rs. {Number(summary.revenueTotal).toFixed(2)}</p>
            </article>
            <article className="card metric">
              <h3>Total Products</h3>
              <p>{summary.totalProducts}</p>
            </article>
          </div>

          <section className="card section">
            <div className="section-head">
              <h2>Recent Orders</h2>
            </div>

            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Order</th>
                    <th>Date</th>
                    <th>Total</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {summary.recentOrders?.map((order) => (
                    <tr key={order.id}>
                      <td>{order.orderNumber}</td>
                      <td>{new Date(order.createdAtUtc).toLocaleString()}</td>
                      <td>Rs. {Number(order.totalAmount).toFixed(2)}</td>
                      <td><StatusBadge status={order.status} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </div>
  );
}
