import { useCallback, useEffect, useState } from 'react';
import { catalogApi } from '../api/catalogApi';
import { adminApi } from '../api/adminApi';
import { useAuth } from '../context/AuthContext';
import LoadingSpinner from '../components/LoadingSpinner';

const INITIAL_PRODUCT_FORM = {
  id: null,
  name: '',
  description: '',
  price: '',
  stock: '',
  imageUrl: '',
  categoryId: 1,
  isActive: true
};

export default function AdminProductsPage() {
  const { token } = useAuth();
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [productForm, setProductForm] = useState(INITIAL_PRODUCT_FORM);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadProducts = useCallback(async () => {
    setLoading(true);
    setError('');

    try {
      const [productsData, categoriesData] = await Promise.all([
        catalogApi.searchProducts({ page: 1, pageSize: 200 }),
        adminApi.getCategories()
      ]);
      setProducts(Array.isArray(productsData?.products) ? productsData.products : []);
      setCategories(Array.isArray(categoriesData) ? categoriesData : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  function populateProduct(product) {
    setProductForm({
      id: product.id,
      name: product.name,
      description: product.description,
      price: String(product.price),
      stock: String(product.stock),
      imageUrl: product.imageUrl,
      categoryId: product.category?.id || 1,
      isActive: true
    });
  }

  function clearProductForm() {
    setProductForm(INITIAL_PRODUCT_FORM);
  }

  async function handleProductSubmit(event) {
    event.preventDefault();

    const payload = {
      name: productForm.name,
      description: productForm.description,
      price: Number(productForm.price),
      stock: Number(productForm.stock),
      imageUrl: productForm.imageUrl,
      categoryId: Number(productForm.categoryId),
      isActive: productForm.isActive
    };

    try {
      if (productForm.id) {
        await catalogApi.updateProduct(token, productForm.id, payload);
      } else {
        await catalogApi.createProduct(token, payload);
      }

      clearProductForm();
      await loadProducts();
    } catch (err) {
      alert(err.message);
    }
  }

  async function handleDeleteProduct(id) {
    if (!window.confirm('Delete this product?')) {
      return;
    }

    try {
      await catalogApi.deleteProduct(token, id);
      await loadProducts();
    } catch (err) {
      alert(err.message);
    }
  }

  async function handleStockQuickUpdate(id, stock) {
    const next = window.prompt('New stock value', String(stock));
    if (next === null) return;

    try {
      await catalogApi.updateStock(token, id, Number(next));
      await loadProducts();
    } catch (err) {
      alert(err.message);
    }
  }

  if (loading) {
    return <LoadingSpinner label="Loading products..." />;
  }

  return (
    <div>
      <h1>Catalog Product Management</h1>
      {error && <p className="message error">{error}</p>}

      <section className="card section">
        <div className="section-head">
          <h2>{productForm.id ? 'Edit Product' : 'Add New Product'}</h2>
          {productForm.id ? (
            <button type="button" className="btn btn-outline" onClick={clearProductForm}>Cancel Edit</button>
          ) : null}
        </div>

        <form className="product-form" onSubmit={handleProductSubmit}>
          <input
            placeholder="Name"
            value={productForm.name}
            onChange={(e) => setProductForm((prev) => ({ ...prev, name: e.target.value }))}
            required
          />
          <input
            placeholder="Description"
            value={productForm.description}
            onChange={(e) => setProductForm((prev) => ({ ...prev, description: e.target.value }))}
            required
          />
          <input
            type="number"
            min="0"
            step="0.01"
            placeholder="Price"
            value={productForm.price}
            onChange={(e) => setProductForm((prev) => ({ ...prev, price: e.target.value }))}
            required
          />
          <input
            type="number"
            min="0"
            placeholder="Stock"
            value={productForm.stock}
            onChange={(e) => setProductForm((prev) => ({ ...prev, stock: e.target.value }))}
            required
          />
          <input
            placeholder="Image URL"
            value={productForm.imageUrl}
            onChange={(e) => setProductForm((prev) => ({ ...prev, imageUrl: e.target.value }))}
            required
          />
          <select
            value={productForm.categoryId}
            onChange={(e) => setProductForm((prev) => ({ ...prev, categoryId: Number(e.target.value) }))}
          >
            {categories.map((cat) => (
              <option key={cat.id} value={cat.id}>{cat.name}</option>
            ))}
          </select>
          <label className="inline-actions">
            <input
              type="checkbox"
              checked={productForm.isActive}
              onChange={(e) => setProductForm((prev) => ({ ...prev, isActive: e.target.checked }))}
            />
            Active
          </label>
          <button type="submit" className="btn btn-solid">
            {productForm.id ? 'Update Product' : 'Add Product'}
          </button>
        </form>
      </section>

      <section className="card section">
        <h2>Existing Products</h2>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Category</th>
                <th>Price</th>
                <th>Stock</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr key={product.id}>
                  <td>{product.name}</td>
                  <td>{product.category?.name || '-'}</td>
                  <td>Rs. {Number(product.price).toFixed(2)}</td>
                  <td>{product.stock}</td>
                  <td>
                    <div className="inline-actions">
                      <button type="button" className="btn btn-outline" onClick={() => populateProduct(product)}>
                        Edit
                      </button>
                      <button
                        type="button"
                        className="btn btn-outline"
                        onClick={() => handleStockQuickUpdate(product.id, product.stock)}
                      >
                        Stock
                      </button>
                      <button
                        type="button"
                        className="btn btn-outline"
                        onClick={() => handleDeleteProduct(product.id)}
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
