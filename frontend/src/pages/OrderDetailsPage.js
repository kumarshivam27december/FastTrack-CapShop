import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { reviewApi } from '../api/reviewApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

export default function OrderDetailsPage() {
  const { id } = useParams();
  const { token } = useAuth();
  const [order, setOrder] = useState(null);
  const [tracking, setTracking] = useState(null);
  const [eligibilities, setEligibilities] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [trackingError, setTrackingError] = useState('');
  const [reviewOpenProductId, setReviewOpenProductId] = useState(null);
  const [reviewForm, setReviewForm] = useState({
    rating: 5,
    title: '',
    comment: ''
  });
  const [reviewSaving, setReviewSaving] = useState(false);
  const [reviewError, setReviewError] = useState('');
  const [reviewMessage, setReviewMessage] = useState('');

  useEffect(() => {
    let isMounted = true;

    async function load() {
      setLoading(true);
      setError('');
      setReviewError('');
      setReviewMessage('');
      try {
        const orderData = await orderApi.getOrderById(token, id);
        if (isMounted) {
          setOrder(orderData);
        }

        try {
          const trackingData = await orderApi.getOrderTracking(token, id);
          if (isMounted) {
            setTracking(trackingData);
            setTrackingError('');
          }
        } catch (trackingErr) {
          if (isMounted) {
            setTracking(null);
            setTrackingError(`Tracking is not available yet: ${trackingErr.message}`);
          }
        }

        try {
          const reviewEligibilities = await reviewApi.getMyEligibilities(token);
          if (isMounted) {
            setEligibilities(Array.isArray(reviewEligibilities) ? reviewEligibilities : []);
          }
        } catch {
          if (isMounted) {
            setEligibilities([]);
          }
        }
      } catch (err) {
        if (isMounted) {
          setError(err.message);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    load();

    return () => {
      isMounted = false;
    };
  }, [token, id]);

  function openReviewForm(item) {
    setReviewOpenProductId(item.productId);
    setReviewForm({ rating: 5, title: '', comment: '' });
    setReviewError('');
    setReviewMessage('');
  }

  function closeReviewForm() {
    setReviewOpenProductId(null);
    setReviewError('');
  }

  function getEligibility(item) {
    return eligibilities.find((entry) => entry.orderId === order?.id && entry.productId === item.productId) || null;
  }

  async function handleReviewSubmit(event, item) {
    event.preventDefault();
    setReviewSaving(true);
    setReviewError('');
    setReviewMessage('');

    try {
      await reviewApi.createReview(token, item.productId, {
        rating: Number(reviewForm.rating),
        title: reviewForm.title,
        comment: reviewForm.comment,
        orderId: order.id
      });

      setReviewMessage(`${item.productName} was reviewed successfully.`);
      setReviewOpenProductId(null);
      setReviewForm({ rating: 5, title: '', comment: '' });

      try {
        const reviewEligibilities = await reviewApi.getMyEligibilities(token);
        setEligibilities(Array.isArray(reviewEligibilities) ? reviewEligibilities : []);
      } catch {
        setEligibilities([]);
      }
    } catch (err) {
      setReviewError(err.message);
    } finally {
      setReviewSaving(false);
    }
  }

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

      <div className="order-review-section card" id="order-reviews">
        <div className="section-head">
          <div>
            <h2>Rate Your Delivered Items</h2>
            <p className="meta">Leave a rating from My Orders without opening the product page.</p>
          </div>
        </div>

        {reviewMessage && <p className="message success">{reviewMessage}</p>}

        {order.status !== 'Delivered' ? (
          <p className="message">Reviews unlock automatically after the order is delivered.</p>
        ) : (
          <div className="order-review-grid">
            {order.items?.map((item) => {
              const eligibility = getEligibility(item);
              const isAlreadyReviewed = Boolean(eligibility?.hasReviewed);
              const canReview = Boolean(eligibility && !eligibility.hasReviewed);
              const isOpen = reviewOpenProductId === item.productId;

              return (
                <article className="order-review-card" key={`${item.productId}-${item.productName}`}>
                  <div className="order-review-card-header">
                    <div>
                      <h3>{item.productName}</h3>
                      <p className="meta">
                        Qty {item.quantity} | Rs. {Number(item.totalPrice).toFixed(2)} total
                      </p>
                    </div>

                    {isAlreadyReviewed ? (
                      <span className="review-status-pill reviewed">Reviewed</span>
                    ) : canReview ? (
                      <span className="review-status-pill ready">Ready</span>
                    ) : (
                      <span className="review-status-pill muted">Syncing</span>
                    )}
                  </div>

                  {canReview && isOpen ? (
                    <form className="review-form order-review-form" onSubmit={(event) => handleReviewSubmit(event, item)}>
                      <div className="review-form-grid">
                        <label>
                          Rating
                          <select
                            value={reviewForm.rating}
                            onChange={(event) => setReviewForm((current) => ({ ...current, rating: Number(event.target.value) }))}
                          >
                            <option value="5">5 - Excellent</option>
                            <option value="4">4 - Good</option>
                            <option value="3">3 - Okay</option>
                            <option value="2">2 - Poor</option>
                            <option value="1">1 - Bad</option>
                          </select>
                        </label>
                        <label>
                          Title
                          <input
                            value={reviewForm.title}
                            maxLength="120"
                            onChange={(event) => setReviewForm((current) => ({ ...current, title: event.target.value }))}
                            required
                          />
                        </label>
                      </div>

                      <label>
                        Comment
                        <textarea
                          value={reviewForm.comment}
                          maxLength="2000"
                          rows="4"
                          onChange={(event) => setReviewForm((current) => ({ ...current, comment: event.target.value }))}
                          required
                        />
                      </label>

                      {reviewError && <p className="message error">{reviewError}</p>}

                      <div className="inline-actions">
                        <button type="submit" className="btn btn-solid" disabled={reviewSaving}>
                          {reviewSaving ? 'Submitting...' : 'Submit Review'}
                        </button>
                        <button type="button" className="btn btn-outline" onClick={closeReviewForm} disabled={reviewSaving}>
                          Cancel
                        </button>
                      </div>
                    </form>
                  ) : canReview ? (
                    <div className="inline-actions">
                      <button type="button" className="btn btn-solid" onClick={() => openReviewForm(item)}>
                        Write Review
                      </button>
                    </div>
                  ) : isAlreadyReviewed ? (
                    <p className="message">You have already reviewed this item.</p>
                  ) : (
                    <p className="message">This item will appear here once review eligibility syncs.</p>
                  )}
                </article>
              );
            })}
          </div>
        )}
      </div>
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
