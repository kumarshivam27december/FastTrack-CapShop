import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

export default function CancelOrderConfirmPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { token } = useAuth();

  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
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

  async function handleConfirmCancel() {
    setBusy(true);
    setError('');
    try {
      await orderApi.cancelOrder(token, id);
      navigate('/orders', { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  if (loading) {
    return <LoadingSpinner label="Loading order for confirmation..." />;
  }

  if (!order) {
    return (
      <section className="section card">
        <h1>Order Not Found</h1>
        <Link to="/orders" className="btn btn-outline">Back to Orders</Link>
      </section>
    );
  }

  const cannotCancel = ['Packed', 'Shipped', 'Delivered', 'Cancelled'].includes(order.status);

  return (
    <section className="section card cancel-confirm-card">
      <h1>Confirm Order Cancellation</h1>
      {error && <p className="message error">{error}</p>}

      <p>You are about to cancel this order:</p>
      <p><strong>{order.orderNumber}</strong></p>
      <p>Status: <StatusBadge status={order.status} /></p>
      <p>Total: Rs. {Number(order.totalAmount).toFixed(2)}</p>

      {cannotCancel ? (
        <p className="message error">This order cannot be cancelled in its current status.</p>
      ) : (
        <p className="message">This action cannot be undone.</p>
      )}

      <div className="inline-actions">
        <Link to={`/orders/${order.id}`} className="btn btn-outline">Back to Order</Link>
        <button
          type="button"
          className="btn btn-danger"
          onClick={handleConfirmCancel}
          disabled={cannotCancel || busy}
        >
          {busy ? 'Cancelling...' : 'Yes, Cancel Order'}
        </button>
      </div>
    </section>
  );
}
