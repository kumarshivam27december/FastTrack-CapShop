import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

function toCsvValue(value) {
  if (value === null || value === undefined) {
    return '';
  }

  const str = String(value);
  if (/[",\n\r]/.test(str)) {
    return `"${str.replace(/"/g, '""')}"`;
  }

  return str;
}

export default function OrdersPage() {
  const { token } = useAuth();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [downloading, setDownloading] = useState(false);

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

  function handleDownloadAllOrders() {
    if (!orders.length || downloading) {
      return;
    }

    setDownloading(true);

    try {
      const headers = ['Order Number', 'Order Id', 'Date', 'Amount', 'Status'];
      const rows = orders.map((order) => [
        order.orderNumber || '',
        order.id || '',
        order.createdAtUtc ? new Date(order.createdAtUtc).toLocaleString() : '',
        Number(order.totalAmount).toFixed(2),
        order.status || ''
      ]);

      const csvContent = [headers, ...rows].map((row) => row.map(toCsvValue).join(',')).join('\n');
      const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      const datePart = new Date().toISOString().slice(0, 10);

      link.href = url;
      link.download = `my-orders-${datePart}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (err) {
      setError(err.message || 'Failed to download orders.');
    } finally {
      setDownloading(false);
    }
  }

  return (
    <section className="section">
      <div className="section-head">
        <h1>My Orders</h1>
        <button
          type="button"
          className="btn btn-outline"
          onClick={handleDownloadAllOrders}
          disabled={loading || !orders.length || downloading}
        >
          {downloading ? 'Preparing download...' : 'Download All Orders'}
        </button>
      </div>

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
                      <Link to={`/orders/${order.id}/cancel`} className="btn btn-outline">Cancel</Link>
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
