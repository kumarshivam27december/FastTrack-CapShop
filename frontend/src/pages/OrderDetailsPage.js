import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

export default function OrderDetailsPage() {
  const { id } = useParams();
  const { token } = useAuth();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      try {
        const data = await orderApi.getOrderById(token, id);
        setOrder(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [token, id]);

  if (loading) {
    return <LoadingSpinner label="Loading order..." />;
  }

  if (error) {
    return <p className="message error">{error}</p>;
  }

  if (!order) {
    return <p className="message">Order not found.</p>;
  }

  return (
    <section className="section">
      <div className="section-head">
        <h1>Order {order.orderNumber}</h1>
        <Link to="/orders" className="btn btn-outline">Back</Link>
      </div>

      <div className="card">
        <p>Status: <StatusBadge status={order.status} /></p>
        <p>Total: Rs. {Number(order.totalAmount).toFixed(2)}</p>
        <p>Placed: {new Date(order.createdAtUtc).toLocaleString()}</p>
        {order.status !== 'Delivered' && order.status !== 'Cancelled' && (
          <div className="inline-actions">
            <Link to={`/orders/${order.id}/cancel`} className="btn btn-outline">Cancel This Order</Link>
          </div>
        )}
      </div>

      <div className="table-wrap card">
        <table>
          <thead>
            <tr>
              <th>Product</th>
              <th>Quantity</th>
              <th>Unit Price</th>
              <th>Total</th>
            </tr>
          </thead>
          <tbody>
            {order.items?.map((item) => (
              <tr key={`${item.productId}-${item.productName}`}>
                <td>{item.productName}</td>
                <td>{item.quantity}</td>
                <td>Rs. {Number(item.unitPrice).toFixed(2)}</td>
                <td>Rs. {Number(item.totalPrice).toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
