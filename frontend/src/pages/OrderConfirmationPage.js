import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import InvoiceTemplate from '../components/InvoiceTemplate';
import LoadingSpinner from '../components/LoadingSpinner';

export default function OrderConfirmationPage() {
  const { orderId } = useParams();
  const { token } = useAuth();
  const [order, setOrder] = useState(null);
  const [address, setAddress] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      try {
        const data = await orderApi.getOrderById(token, orderId);
        setOrder(data);
        // Extract address from order if available, otherwise use defaults
        if (data.shippingAddress) {
          setAddress(data.shippingAddress);
        } else {
          setAddress({
            fullName: 'Customer',
            street: 'N/A',
            city: 'N/A',
            state: 'N/A',
            pincode: 'N/A',
            phone: 'N/A'
          });
        }
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    if (orderId && token) {
      load();
    }
  }, [orderId, token]);

  if (loading) {
    return <LoadingSpinner label="Preparing your invoice..." />;
  }

  if (error) {
    return (
      <section className="section card">
        <h1>Error</h1>
        <p className="message error">{error}</p>
        <Link to="/orders" className="btn btn-solid">View My Orders</Link>
      </section>
    );
  }

  if (!order) {
    return (
      <section className="section card">
        <h1>Order Not Found</h1>
        <p>The order you're looking for doesn't exist.</p>
        <Link to="/orders" className="btn btn-solid">View My Orders</Link>
      </section>
    );
  }

  return (
    <section className="section">
      <div className="confirmation-header">
        <div className="confirmation-success-icon">✓</div>
        <h1>Order Confirmed!</h1>
        <p className="confirmation-message">Thank you for your purchase. Your order has been successfully placed.</p>
      </div>

      <div className="confirmation-content">
        <div className="confirmation-summary card">
          <h2>Order Summary</h2>
          <div className="summary-row">
            <span>Order Number:</span>
            <strong>{order.orderNumber}</strong>
          </div>
          <div className="summary-row">
            <span>Order Date:</span>
            <span>{new Date(order.createdAtUtc).toLocaleString()}</span>
          </div>
          <div className="summary-row">
            <span>Total Amount:</span>
            <strong className="amount">Rs. {Number(order.totalAmount).toFixed(2)}</strong>
          </div>
          <div className="summary-row">
            <span>Status:</span>
            <span className="status-badge">{order.status}</span>
          </div>
          <div className="summary-row">
            <span>Items:</span>
            <span>{order.items?.length || 0} product(s)</span>
          </div>
        </div>

        <div className="invoice-section">
          <InvoiceTemplate order={order} address={address} />
        </div>

        <div className="confirmation-actions card">
          <h2>What's Next?</h2>
          <p>You can track your order status or return to browse more products.</p>
          <div className="action-buttons">
            <Link to={`/orders/${order.id}`} className="btn btn-solid">
              Track Order
            </Link>
            <Link to="/orders" className="btn btn-outline">
              View All Orders
            </Link>
            <Link to="/catalog" className="btn btn-outline">
              Continue Shopping
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
