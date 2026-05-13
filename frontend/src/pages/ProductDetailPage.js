import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { catalogApi } from '../api/catalogApi';
import { reviewApi } from '../api/reviewApi';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import LoadingSpinner from '../components/LoadingSpinner';
import { useToast } from '../components/ToastProvider';

export default function ProductDetailPage() {
  const { id } = useParams();
  const { isAuthenticated, isAdmin, token } = useAuth();
  const { addToCart } = useCart();
  const { success, error: showError } = useToast();
  const [product, setProduct] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [reviewSummary, setReviewSummary] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [reviewError, setReviewError] = useState('');
  const [reviewSaving, setReviewSaving] = useState(false);
  const [reviewForm, setReviewForm] = useState({
    rating: 5,
    title: '',
    comment: ''
  });

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      setReviewError('');
      try {
        const [data, summary, productReviews] = await Promise.all([
          catalogApi.getProductById(id),
          reviewApi.getProductSummary(id, token),
          reviewApi.getProductReviews(id)
        ]);
        setProduct(data);
        setReviewSummary(summary);
        setReviews(productReviews || []);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [id, token]);

  async function reloadReviews() {
    const [summary, productReviews] = await Promise.all([
      reviewApi.getProductSummary(id, token),
      reviewApi.getProductReviews(id)
    ]);
    setReviewSummary(summary);
    setReviews(productReviews || []);
  }

  async function handleAdd() {
    try {
      await addToCart(product.id, quantity);
      success('Added to cart');
    } catch (err) {
      showError(err.message);
    }
  }

  async function handleReviewSubmit(e) {
    e.preventDefault();
    setReviewSaving(true);
    setReviewError('');

    try {
      await reviewApi.createReview(token, product.id, {
        rating: Number(reviewForm.rating),
        title: reviewForm.title,
        comment: reviewForm.comment
      });
      setReviewForm({ rating: 5, title: '', comment: '' });
      await reloadReviews();
    } catch (err) {
      setReviewError(err.message);
    } finally {
      setReviewSaving(false);
    }
  }

  if (loading) {
    return <LoadingSpinner label="Loading product..." />;
  }

  if (error) {
    return <p className="message error">{error}</p>;
  }

  if (!product) {
    return <p className="message">Product not found.</p>;
  }

  return (
    <section className="section">
      <div className="detail-grid card">
        <img src={product.imageUrl} alt={product.name} className="detail-image" />

        <div>
          <h1>{product.name}</h1>
          <p className="rating-summary">
            Rating: {Number(reviewSummary?.averageRating || 0).toFixed(1)} / 5
            {' '}({reviewSummary?.reviewCount || 0} reviews)
          </p>
          <p>{product.description}</p>
          <p className="meta">Category: {product.category?.name || 'Unknown'}</p>
          <p className="meta">Available stock: {product.stock}</p>
          <h2>Rs. {Number(product.price).toFixed(2)}</h2>

          {isAuthenticated && !isAdmin ? (
            <div className="inline-actions">
              <input
                type="number"
                min="1"
                max={Math.max(1, product.stock)}
                value={quantity}
                onChange={(e) => setQuantity(Number(e.target.value) || 1)}
              />
              <button type="button" className="btn btn-solid" onClick={handleAdd}>
                Add To Cart
              </button>
            </div>
          ) : !isAuthenticated ? (
            <p className="message">Login to add this item to your cart.</p>
          ) : null
          }
        </div>
      </div>

      <div className="reviews-panel">
        <div className="reviews-header">
          <div>
            <h2>Customer Reviews</h2>
            <p className="meta">
              {reviewSummary?.reviewCount || 0} approved reviews for this product.
            </p>
          </div>
          <div className="rating-pill">
            {Number(reviewSummary?.averageRating || 0).toFixed(1)} / 5
          </div>
        </div>

        {isAuthenticated && !isAdmin && reviewSummary?.canCurrentUserReview ? (
          <form className="review-form card" onSubmit={handleReviewSubmit}>
            <div className="review-form-grid">
              <label>
                Rating
                <select
                  value={reviewForm.rating}
                  onChange={(e) => setReviewForm((current) => ({ ...current, rating: Number(e.target.value) }))}
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
                  onChange={(e) => setReviewForm((current) => ({ ...current, title: e.target.value }))}
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
                onChange={(e) => setReviewForm((current) => ({ ...current, comment: e.target.value }))}
                required
              />
            </label>
            {reviewError && <p className="message error">{reviewError}</p>}
            <button type="submit" className="btn btn-solid" disabled={reviewSaving}>
              {reviewSaving ? 'Submitting...' : 'Submit Review'}
            </button>
          </form>
        ) : isAuthenticated && !isAdmin ? (
          <p className="message">You can review this product after a delivered purchase.</p>
        ) : !isAuthenticated ? (
          <p className="message">Login after purchase delivery to write a review.</p>
        ) : null}

        <div className="review-list">
          {reviews.length === 0 ? (
            <p className="message">No reviews yet.</p>
          ) : reviews.map((review) => (
            <article key={review.id} className="review-item">
              <div className="review-item-header">
                <div>
                  <h3>{review.title}</h3>
                  <p className="meta">
                    {review.userName} {review.isVerifiedPurchase ? '- Verified purchase' : ''}
                  </p>
                </div>
                <strong>{review.rating} / 5</strong>
              </div>
              <p>{review.comment}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
