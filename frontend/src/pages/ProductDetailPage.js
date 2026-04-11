import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { catalogApi } from '../api/catalogApi';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import LoadingSpinner from '../components/LoadingSpinner';

export default function ProductDetailPage() {
  const { id } = useParams();
  const { isAuthenticated, isAdmin } = useAuth();
  const { addToCart } = useCart();
  const [product, setProduct] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      try {
        const data = await catalogApi.getProductById(id);
        setProduct(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [id]);

  async function handleAdd() {
    try {
      await addToCart(product.id, quantity);
      alert('Added to cart');
    } catch (err) {
      alert(err.message);
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
    </section>
  );
}
