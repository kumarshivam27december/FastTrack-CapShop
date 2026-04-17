import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { catalogApi } from '../api/catalogApi';
import LoadingSpinner from '../components/LoadingSpinner';
import { useAuth } from '../context/AuthContext';

export default function HomePage() {
  const [featured, setFeatured] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const { isAdmin } = useAuth();

  useEffect(() => {
    async function load() {
      try {
        const data = await catalogApi.getFeatured();
        setFeatured(Array.isArray(data) ? data : []);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    load();
  }, []);

  return (
    <section>
      <div className="hero">
        <p className="eyebrow">Gateway Powered Storefront</p>
        <h1>Shop Fast. Track Smart. Manage Everything.</h1>
        <p>
         Experience a unified commerce engine powered by distributed microservices. From secure identity management to real-time inventory synchronization, our architecture ensures a seamless, high-performance shopping journey through a single, secure gateway.
        </p>
        <div className="hero-actions">
          <Link to="/catalog" className="btn btn-solid">Browse Catalog</Link>
          {isAdmin && <Link to="/admin" className="btn btn-outline">Open Admin</Link>}
        </div>
      </div>

      <section className="section">
        <div className="section-head">
          <h2>Featured Products</h2>
          <Link to="/catalog">View all</Link>
        </div>

        {loading && <LoadingSpinner label="Loading featured products..." />}
        {error && <p className="message error">{error}</p>}

        <div className="product-grid">
          {featured.map((product) => (
            <article key={product.id} className="card product-card">
              <img src={product.imageUrl} alt={product.name} className="product-image" />
              <h3>{product.name}</h3>
              <p>{product.description}</p>
              <div className="card-row">
                <strong>Rs. {Number(product.price).toFixed(2)}</strong>
                <Link to={`/products/${product.id}`} className="btn btn-outline">Details</Link>
              </div>
            </article>
          ))}
        </div>
      </section>
    </section>
  );
}
