import { useCallback, useEffect, useState } from 'react';
import { adminApi } from '../api/adminApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

const STATUS_OPTIONS = ['Paid', 'Packed', 'Shipped', 'Delivered', 'Cancelled'];

export default function AdminOrdersPage() {
  const { token } = useAuth();
  const [orders, setOrders] = useState([]);
  const [orderStatusById, setOrderStatusById] = useState({});
  const [orderNotesById, setOrderNotesById] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadOrders = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await adminApi.getOrders(token);
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

  async function handleOrderStatusUpdate(orderId) {
    const newStatus = orderStatusById[orderId];
    if (!newStatus) {
      alert('Please choose a status');
      return;
    }

    try {
      await adminApi.updateOrderStatus(token, orderId, {
        newStatus,
        notes: orderNotesById[orderId] || ''
      });
      await loadOrders();
    } catch (err) {
      alert(err.message);
    }
  }

  if (loading) {
    return <LoadingSpinner label="Loading admin orders..." />;
  }

  return (
    <div>
      <h1>Order Management</h1>
      {error && <p className="message error">{error}</p>}

      <section className="card section">
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Order</th>
                <th>Total</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <tr key={order.id}>
                  <td>
                    <div>{order.orderNumber}</div>
                    <small>{new Date(order.createdAtUtc).toLocaleString()}</small>
                  </td>
                  <td>Rs. {Number(order.totalAmount).toFixed(2)}</td>
                  <td><StatusBadge status={order.status} /></td>
                  <td>
                    <div className="stack-actions">
                      <select
                        value={orderStatusById[order.id] || ''}
                        onChange={(e) =>
                          setOrderStatusById((prev) => ({ ...prev, [order.id]: e.target.value }))
                        }
                      >
                        <option value="">Select status</option>
                        {STATUS_OPTIONS.map((status) => (
                          <option key={status} value={status}>{status}</option>
                        ))}
                      </select>

                      <input
                        placeholder="Notes"
                        value={orderNotesById[order.id] || ''}
                        onChange={(e) =>
                          setOrderNotesById((prev) => ({ ...prev, [order.id]: e.target.value }))
                        }
                      />

                      <button
                        type="button"
                        className="btn btn-outline"
                        onClick={() => handleOrderStatusUpdate(order.id)}
                      >
                        Update
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
