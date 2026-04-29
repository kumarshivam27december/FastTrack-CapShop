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
  const [tracking, setTracking] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [trackingError, setTrackingError] = useState('');

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      try {
        const orderData = await orderApi.getOrderById(token, id);
        setOrder(orderData);

        try {
          const trackingData = await orderApi.getOrderTracking(token, id);
          setTracking(trackingData);
          setTrackingError('');
        } catch (trackingErr) {
          setTracking(null);
          setTrackingError(`Tracking is not available yet: ${trackingErr.message}`);
        }
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

      <OrderTrackingPanel tracking={tracking} error={trackingError} />

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

function OrderTrackingPanel({ tracking, error }) {
  if (error) {
    return <p className="message error">{error}</p>;
  }

  if (!tracking) {
    return null;
  }

  const minutes = Number(tracking.estimatedMinutesRemaining || 0);
  const etaText = minutes <= 0
    ? 'Completed'
    : minutes < 60
      ? `${minutes} min`
      : `${Math.floor(minutes / 60)}h ${minutes % 60}m`;
  const current = tracking.currentLocation || {};
  const destination = tracking.destination || {};
  const mapSrc = `https://www.openstreetmap.org/export/embed.html?bbox=${Number(current.longitude) - 0.08}%2C${Number(current.latitude) - 0.08}%2C${Number(current.longitude) + 0.08}%2C${Number(current.latitude) + 0.08}&layer=mapnik&marker=${current.latitude}%2C${current.longitude}`;

  return (
    <div className="order-tracking card">
      <div className="tracking-header">
        <div>
          <p className="eyebrow">Live order tracking</p>
          <h2>{tracking.trackingStatus}</h2>
        </div>
        <div className="tracking-eta">
          <span>ETA</span>
          <strong>{etaText}</strong>
        </div>
      </div>

      <div className="tracking-summary-grid">
        <div>
          <span>Current location</span>
          <strong>{tracking.currentLocationName}</strong>
        </div>
        <div>
          <span>Nearest delivery hub</span>
          <strong>{tracking.nearestHubName}</strong>
        </div>
        <div>
          <span>Expected by</span>
          <strong>{new Date(tracking.estimatedDeliveryUtc).toLocaleString()}</strong>
        </div>
        <div>
          <span>Destination</span>
          <strong>{destination.name}</strong>
        </div>
      </div>

      <div className="tracking-map-grid">
        <div className="tracking-map-frame">
          <iframe
            title={`Map for order ${tracking.orderNumber}`}
            src={mapSrc}
            loading="lazy"
          />
        </div>

        <div className="route-board">
          {(tracking.route || []).map((point, index) => (
            <div
              className={`route-stop ${point.isCompleted ? 'done' : ''} ${point.isCurrent ? 'current' : ''}`}
              key={`${point.name}-${index}`}
            >
              <span className="route-dot" />
              <div>
                <strong>{point.name}</strong>
                <small>{formatPointKind(point.kind)}</small>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="tracking-timeline">
        {(tracking.events || []).map((event, index) => (
          <div
            className={`tracking-event ${event.isCompleted ? 'done' : ''} ${event.isCurrent ? 'current' : ''}`}
            key={`${event.title}-${index}`}
          >
            <span className="event-dot" />
            <div>
              <div className="event-title-row">
                <strong>{event.title}</strong>
                <span>{event.status}</span>
              </div>
              <p>{event.locationName}</p>
              <small>{new Date(event.occurredAtUtc).toLocaleString()}</small>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function formatPointKind(kind) {
  return String(kind || '')
    .split('-')
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
}
