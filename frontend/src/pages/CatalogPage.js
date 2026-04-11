import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { catalogApi } from '../api/catalogApi';
import { adminApi } from '../api/adminApi';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import LoadingSpinner from '../components/LoadingSpinner';

export default function CatalogPage() {
  const { isAuthenticated, isAdmin } = useAuth();
  const { addToCart } = useCart();
  const [query, setQuery] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [categories, setCategories] = useState([]);
  const [minPrice, setMinPrice] = useState('');
  const [maxPrice, setMaxPrice] = useState('');
  const [sortBy, setSortBy] = useState('');
  const [page, setPage] = useState(1);

  const [result, setResult] = useState({ products: [], total: 0, page: 1, pageSize: 10, totalPages: 1 });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const filters = useMemo(
    () => ({
      query,
      categoryId: categoryId || undefined,
      minPrice: minPrice || undefined,
      maxPrice: maxPrice || undefined,
      sortBy: sortBy || undefined,
      page,
      pageSize: 9
    }),
    [query, categoryId, minPrice, maxPrice, sortBy, page]
  );

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      try {
        const [productsData, categoriesData] = await Promise.all([
          catalogApi.searchProducts(filters),
          adminApi.getCategories()
        ]);
        setResult(productsData);
        setCategories(Array.isArray(categoriesData) ? categoriesData : []);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [filters]);

  async function handleAdd(productId) {
    try {
      await addToCart(productId, 1);
      alert('Added to cart');
    } catch (err) {
      alert(err.message);
    }
  }

  return (
    <section className="section">
      <div className="section-head">
        <h1>Catalog</h1>
      </div>

      <div className="filters card">
        <input
          value={query}
          onChange={(e) => {
            setPage(1);
            setQuery(e.target.value);
          }}
          placeholder="Search products"
        />

        <select
          value={categoryId}
          onChange={(e) => {
            setPage(1);
            setCategoryId(e.target.value);
          }}
        >
          <option value="">All categories</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>{category.name}</option>
          ))}
        </select>

        <input
          type="number"
          min="0"
          value={minPrice}
          onChange={(e) => {
            setPage(1);
            setMinPrice(e.target.value);
          }}
          placeholder="Min price"
        />

        <input
          type="number"
          min="0"
          value={maxPrice}
          onChange={(e) => {
            setPage(1);
            setMaxPrice(e.target.value);
          }}
          placeholder="Max price"
        />

        <select
          value={sortBy}
          onChange={(e) => {
            setPage(1);
            setSortBy(e.target.value);
          }}
        >
          <option value="">Default sort</option>
          <option value="price_asc">Price: low to high</option>
          <option value="price_desc">Price: high to low</option>
          <option value="name_asc">Name: A to Z</option>
          <option value="name_desc">Name: Z to A</option>
        </select>
      </div>

      {loading && <LoadingSpinner label="Loading products..." />}
      {error && <p className="message error">{error}</p>}

      <div className="product-grid">
        {result.products?.map((product) => (
          <article key={product.id} className="card product-card">
            <img src={product.imageUrl} alt={product.name} className="product-image" />
            <h3>{product.name}</h3>
            <p>{product.description}</p>
            <p className="meta">Category: {product.category?.name || 'Unknown'}</p>
            <p className="meta">Stock: {product.stock}</p>
            <div className="card-row">
              <strong>Rs. {Number(product.price).toFixed(2)}</strong>
              <div className="inline-actions">
                <Link to={`/products/${product.id}`} className="btn btn-outline">View</Link>
                {isAuthenticated && !isAdmin && (
                  <button
                    type="button"
                    className="btn btn-solid"
                    onClick={() => handleAdd(product.id)}
                  >
                    Add
                  </button>
                )}
              </div>
            </div>
          </article>
        ))}
      </div>

      <div className="pagination">
        <button
          type="button"
          className="btn btn-outline"
          disabled={page <= 1}
          onClick={() => setPage((p) => p - 1)}
        >
          Previous
        </button>
        <span>
          Page {result.page || page} / {result.totalPages || 1}
        </span>
        <button
          type="button"
          className="btn btn-outline"
          disabled={(result.totalPages || 1) <= page}
          onClick={() => setPage((p) => p + 1)}
        >
          Next
        </button>
      </div>
    </section>
  );
}
