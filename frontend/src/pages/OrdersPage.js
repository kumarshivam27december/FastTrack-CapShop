import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

export default function OrdersPage() {
  const { token } = useAuth();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadOrders = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await orderApi.getMyOrders(token);
      setOrders(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  async function handleCancel(id) {
    try {
      await orderApi.cancelOrder(token, id);
      await loadOrders();
    } catch (err) {
      alert(err.message);
    }
  }

  return (
    <section className="section">
      <h1>My Orders</h1>

      {loading && <LoadingSpinner label="Loading your orders..." />}
      {error && <p className="message error">{error}</p>}

      {!loading && !orders.length && <p className="message">No orders found.</p>}

      <div className="table-wrap card">
        <table>
          <thead>
            <tr>
              <th>Order</th>
              <th>Date</th>
              <th>Amount</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((order) => (
              <tr key={order.id}>
                <td>{order.orderNumber}</td>
                <td>{new Date(order.createdAtUtc).toLocaleString()}</td>
                <td>Rs. {Number(order.totalAmount).toFixed(2)}</td>
                <td><StatusBadge status={order.status} /></td>
                <td>
                  <div className="inline-actions">
                    <Link to={`/orders/${order.id}`} className="btn btn-outline">View</Link>
                    {order.status !== 'Delivered' && order.status !== 'Cancelled' && (
                      <button
                        type="button"
                        className="btn btn-outline"
                        onClick={() => handleCancel(order.id)}
                      >
                        Cancel
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
