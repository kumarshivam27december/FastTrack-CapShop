import { useCallback, useEffect, useState } from 'react';
import { adminApi } from '../api/adminApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

const STATUS_OPTIONS = ['Paid', 'Packed', 'Shipped', 'Delivered', 'Cancelled'];

export default function AdminOrdersPage() {
  const { token } = useAuth();
  const [orders, setOrders] = useState([]);
  const [trackingHubs, setTrackingHubs] = useState([]);
  const [orderStatusById, setOrderStatusById] = useState({});
  const [trackingHubById, setTrackingHubById] = useState({});
  const [orderNotesById, setOrderNotesById] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadOrders = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [data, hubs] = await Promise.all([
        adminApi.getOrders(token),
        adminApi.getTrackingHubs(token)
      ]);
      setOrders(Array.isArray(data) ? data : []);
      setTrackingHubs(Array.isArray(hubs) ? hubs : []);
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
    const effectiveStatus = newStatus || orders.find((order) => order.id === orderId)?.status;
    const trackingCheckpointCode = trackingHubById[orderId] || '';

    if (!effectiveStatus && !trackingCheckpointCode) {
      alert('Please choose a status or tracking hub');
      return;
    }

    try {
      await adminApi.updateOrderStatus(token, orderId, {
        newStatus: effectiveStatus,
        notes: orderNotesById[orderId] || '',
        trackingCheckpointCode
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

                      <select
                        value={trackingHubById[order.id] || ''}
                        onChange={(e) =>
                          setTrackingHubById((prev) => ({ ...prev, [order.id]: e.target.value }))
                        }
                      >
                        <option value="">Select current hub</option>
                        {trackingHubs.map((hub) => (
                          <option key={hub.code} value={hub.code}>
                            {hub.name}
                          </option>
                        ))}
                      </select>

                      <input
                        placeholder="Notes for customer"
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
