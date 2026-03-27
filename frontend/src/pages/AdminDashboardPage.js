import { useCallback, useEffect, useMemo, useState } from 'react';
import { adminApi } from '../api/adminApi';
import { catalogApi } from '../api/catalogApi';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import LoadingSpinner from '../components/LoadingSpinner';

const STATUS_OPTIONS = ['Paid', 'Packed', 'Shipped', 'Delivered', 'Cancelled'];

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

export default function AdminDashboardPage() {
  const { token } = useAuth();

  const [summary, setSummary] = useState(null);
  const [orders, setOrders] = useState([]);
  const [statusSplit, setStatusSplit] = useState([]);
  const [salesRows, setSalesRows] = useState([]);
  const [salesFrom, setSalesFrom] = useState(new Date(Date.now() - 7 * 86400000).toISOString().slice(0, 10));
  const [salesTo, setSalesTo] = useState(new Date().toISOString().slice(0, 10));

  const [products, setProducts] = useState([]);
  const [productForm, setProductForm] = useState(INITIAL_PRODUCT_FORM);

  const [orderStatusById, setOrderStatusById] = useState({});
  const [orderNotesById, setOrderNotesById] = useState({});

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const totalSales = useMemo(
    () => salesRows.reduce((sum, row) => sum + Number(row.revenue || 0), 0),
    [salesRows]
  );

  const loadSummary = useCallback(async () => {
    const data = await adminApi.getSummary(token);
    setSummary(data);
  }, [token]);

  const loadOrders = useCallback(async () => {
    const data = await adminApi.getOrders(token);
    setOrders(Array.isArray(data) ? data : []);
  }, [token]);

  const loadStatusSplit = useCallback(async () => {
    const data = await adminApi.getStatusSplit(token);
    setStatusSplit(Array.isArray(data) ? data : []);
  }, [token]);

  const loadSales = useCallback(async () => {
    const data = await adminApi.getSalesReport(token, salesFrom, salesTo);
    setSalesRows(Array.isArray(data) ? data : []);
  }, [token, salesFrom, salesTo]);

  const loadProducts = useCallback(async () => {
    const data = await catalogApi.searchProducts({ page: 1, pageSize: 200 });
    setProducts(Array.isArray(data?.products) ? data.products : []);
  }, []);

  const loadAll = useCallback(async () => {
    setLoading(true);
    setError('');

    try {
      await Promise.all([loadSummary(), loadOrders(), loadStatusSplit(), loadSales(), loadProducts()]);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [loadSummary, loadOrders, loadStatusSplit, loadSales, loadProducts]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  async function handleOrderStatusUpdate(orderId) {
    const newStatus = orderStatusById[orderId];
    if (!newStatus) {
      alert('Please choose a status');
      return;
    }

    try {
      await adminApi.updateOrderStatus(token, orderId, {
        newStatus,
        notes: orderNotesById[orderId] || ''
      });
      await Promise.all([loadOrders(), loadSummary(), loadStatusSplit()]);
    } catch (err) {
      alert(err.message);
    }
  }

  async function handleSalesRefresh() {
    try {
      await loadSales();
    } catch (err) {
      alert(err.message);
    }
  }

  async function handleSalesDownload() {
    try {
      const blob = await adminApi.exportSalesCsv(token, salesFrom, salesTo);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `sales-${salesFrom}-${salesTo}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      alert(err.message);
    }
  }

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
    return <LoadingSpinner label="Loading admin dashboard..." />;
  }

  return (
    <section className="section admin">
      <h1>Admin Dashboard</h1>
      {error && <p className="message error">{error}</p>}

      {summary && (
        <div className="summary-grid">
          <article className="card metric">
            <h3>Total Orders</h3>
            <p>{summary.totalOrders}</p>
          </article>
          <article className="card metric">
            <h3>Orders Today</h3>
            <p>{summary.ordersToday}</p>
          </article>
          <article className="card metric">
            <h3>Total Revenue</h3>
            <p>Rs. {Number(summary.revenueTotal).toFixed(2)}</p>
          </article>
          <article className="card metric">
            <h3>Total Products</h3>
            <p>{summary.totalProducts}</p>
          </article>
        </div>
      )}

      <div className="admin-grid">
        <section className="card">
          <div className="section-head">
            <h2>Orders Management</h2>
          </div>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Order</th>
                  <th>Total</th>
                  <th>Status</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr key={order.id}>
                    <td>
                      <div>{order.orderNumber}</div>
                      <small>{new Date(order.createdAtUtc).toLocaleString()}</small>
                    </td>
                    <td>Rs. {Number(order.totalAmount).toFixed(2)}</td>
                    <td><StatusBadge status={order.status} /></td>
                    <td>
                      <div className="stack-actions">
                        <select
                          value={orderStatusById[order.id] || ''}
                          onChange={(e) =>
                            setOrderStatusById((prev) => ({ ...prev, [order.id]: e.target.value }))
                          }
                        >
                          <option value="">Select status</option>
                          {STATUS_OPTIONS.map((status) => (
                            <option key={status} value={status}>{status}</option>
                          ))}
                        </select>

                        <input
                          placeholder="Notes"
                          value={orderNotesById[order.id] || ''}
                          onChange={(e) =>
                            setOrderNotesById((prev) => ({ ...prev, [order.id]: e.target.value }))
                          }
                        />

                        <button
                          type="button"
                          className="btn btn-outline"
                          onClick={() => handleOrderStatusUpdate(order.id)}
                        >
                          Update
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="card">
          <div className="section-head">
            <h2>Status Split</h2>
          </div>

          <div className="status-bars">
            {statusSplit.map((row) => {
              const max = Math.max(...statusSplit.map((x) => x.count), 1);
              const width = `${Math.max(12, (row.count / max) * 100)}%`;

              return (
                <div key={row.status} className="status-row">
                  <span>{row.status}</span>
                  <div className="status-track">
                    <div className="status-fill" style={{ width }} />
                  </div>
                  <strong>{row.count}</strong>
                </div>
              );
            })}
          </div>
        </section>

        <section className="card">
          <div className="section-head">
            <h2>Sales Report</h2>
          </div>

          <div className="inline-actions">
            <label>
              From
              <input type="date" value={salesFrom} onChange={(e) => setSalesFrom(e.target.value)} />
            </label>
            <label>
              To
              <input type="date" value={salesTo} onChange={(e) => setSalesTo(e.target.value)} />
            </label>
            <button type="button" className="btn btn-outline" onClick={handleSalesRefresh}>Refresh</button>
            <button type="button" className="btn btn-solid" onClick={handleSalesDownload}>Download CSV</button>
          </div>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Orders</th>
                  <th>Revenue</th>
                </tr>
              </thead>
              <tbody>
                {salesRows.map((row) => (
                  <tr key={row.date}>
                    <td>{row.date}</td>
                    <td>{row.orderCount}</td>
                    <td>Rs. {Number(row.revenue).toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <p className="message success">Range Revenue: Rs. {totalSales.toFixed(2)}</p>
        </section>
      </div>

      <section className="card section">
        <div className="section-head">
          <h2>Catalog Product Management</h2>
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
            <option value={1}>Electronics</option>
            <option value={2}>Clothing</option>
            <option value={3}>Books</option>
            <option value={4}>Home</option>
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
            {productForm.id ? 'Update Product' : 'Create Product'}
          </button>
        </form>

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
    </section>
  );
}
