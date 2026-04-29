import { useEffect, useState } from 'react';
import { adminApi } from '../api/adminApi';
import { useAuth } from '../context/AuthContext';
import LoadingSpinner from '../components/LoadingSpinner';

export default function AdminOverviewPage() {
  const { token } = useAuth();
  const [summary, setSummary] = useState(null);
  const [statusSplit, setStatusSplit] = useState([]);
  const [salesRows, setSalesRows] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError('');
      try {
        const today = new Date().toISOString().slice(0, 10);
        const weekAgo = new Date(Date.now() - 7 * 86400000).toISOString().slice(0, 10);
        const [summaryData, statusData, salesData] = await Promise.all([
          adminApi.getSummary(token),
          adminApi.getStatusSplit(token),
          adminApi.getSalesReport(token, weekAgo, today)
        ]);
        setSummary(summaryData);
        setStatusSplit(Array.isArray(statusData) ? statusData : []);
        setSalesRows(Array.isArray(salesData) ? salesData : []);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [token]);

  if (loading) {
    return <LoadingSpinner label="Loading overview..." />;
  }

  const totalStatusCount = statusSplit.reduce((sum, row) => sum + Number(row.count || 0), 0);
  const totalRevenue = salesRows.reduce((sum, row) => sum + Number(row.revenue || 0), 0);
  const maxRevenue = Math.max(...salesRows.map((row) => Number(row.revenue || 0)), 1);
  const maxOrders = Math.max(...salesRows.map((row) => Number(row.orderCount || 0)), 1);
  const revenueBuckets = [
    { label: 'Low', min: 0, max: maxRevenue * 0.33 },
    { label: 'Mid', min: maxRevenue * 0.33, max: maxRevenue * 0.66 },
    { label: 'High', min: maxRevenue * 0.66, max: Number.POSITIVE_INFINITY }
  ].map((bucket) => ({
    ...bucket,
    count: salesRows.filter((row) => {
      const revenue = Number(row.revenue || 0);
      return revenue >= bucket.min && revenue < bucket.max;
    }).length
  }));
  const maxBucketCount = Math.max(...revenueBuckets.map((bucket) => bucket.count), 1);
  const pieSegments = statusSplit.reduce((segments, row, index) => {
    const value = Number(row.count || 0);
    const previous = segments[index - 1]?.end || 0;
    const percent = totalStatusCount ? (value / totalStatusCount) * 100 : 0;
    const colors = ['#5e6d55', '#0d9488', '#f97316', '#3b82f6', '#dc2626', '#8b5cf6'];

    return [
      ...segments,
      {
        ...row,
        color: colors[index % colors.length],
        start: previous,
        end: previous + percent
      }
    ];
  }, []);
  const pieGradient = pieSegments.length
    ? pieSegments
      .map((segment) => `${segment.color} ${segment.start}% ${segment.end}%`)
      .join(', ')
    : 'var(--status-track-bg) 0% 100%';

  return (
    <div className="admin-dashboard-page">
      <div className="section-head">
        <div>
          <p className="eyebrow">Admin Mode</p>
          <h1>Dashboard</h1>
        </div>
      </div>
      {error && <p className="message error">{error}</p>}

      {summary && (
        <>
          <div className="summary-grid admin-dashboard-metrics">
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

          <div className="admin-dashboard-charts">
            <section className="card admin-chart-card">
              <div className="section-head">
                <h2>Order Status</h2>
              </div>

              <div className="status-bars admin-chart-bars">
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

            <section className="card admin-chart-card">
              <div className="section-head">
                <h2>Revenue Trend</h2>
              </div>

              <div className="admin-revenue-bars" aria-label="Revenue trend chart">
                {salesRows.map((row) => {
                  const maxRevenue = Math.max(...salesRows.map((x) => Number(x.revenue || 0)), 1);
                  const height = `${Math.max(12, (Number(row.revenue || 0) / maxRevenue) * 100)}%`;

                  return (
                    <div key={row.date} className="admin-revenue-bar">
                      <div className="admin-revenue-track">
                        <span style={{ height }} />
                      </div>
                      <small>{new Date(row.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}</small>
                    </div>
                  );
                })}
              </div>
            </section>

            <section className="card admin-chart-card">
              <div className="section-head">
                <h2>Status Pie</h2>
              </div>

              <div className="admin-pie-chart-wrap">
                <div
                  className="admin-pie-chart"
                  style={{ background: `conic-gradient(${pieGradient})` }}
                  aria-label="Order status pie chart"
                >
                  <div>
                    <strong>{totalStatusCount}</strong>
                    <span>Orders</span>
                  </div>
                </div>
                <div className="admin-chart-legend">
                  {pieSegments.map((segment) => (
                    <div key={segment.status} className="admin-chart-legend-row">
                      <span style={{ background: segment.color }} />
                      <p>{segment.status}</p>
                      <strong>{segment.count}</strong>
                    </div>
                  ))}
                </div>
              </div>
            </section>

            <section className="card admin-chart-card">
              <div className="section-head">
                <h2>Orders Bar Chart</h2>
              </div>

              <div className="admin-revenue-bars admin-orders-bars" aria-label="Daily orders bar chart">
                {salesRows.map((row) => {
                  const height = `${Math.max(12, (Number(row.orderCount || 0) / maxOrders) * 100)}%`;

                  return (
                    <div key={row.date} className="admin-revenue-bar">
                      <div className="admin-revenue-track">
                        <span style={{ height }} />
                      </div>
                      <small>{new Date(row.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}</small>
                    </div>
                  );
                })}
              </div>
            </section>

            <section className="card admin-chart-card">
              <div className="section-head">
                <h2>Revenue Histogram</h2>
              </div>

              <div className="admin-histogram" aria-label="Revenue histogram">
                {revenueBuckets.map((bucket) => {
                  const height = `${Math.max(10, (bucket.count / maxBucketCount) * 100)}%`;

                  return (
                    <div key={bucket.label} className="admin-histogram-column">
                      <div className="admin-histogram-track">
                        <span style={{ height }} />
                      </div>
                      <strong>{bucket.count}</strong>
                      <small>{bucket.label}</small>
                    </div>
                  );
                })}
              </div>
            </section>

            <section className="card admin-chart-card">
              <div className="section-head">
                <h2>Revenue Mix</h2>
              </div>

              <div className="admin-kpi-chart">
                <div>
                  <span>Total Revenue</span>
                  <strong>Rs. {totalRevenue.toFixed(2)}</strong>
                </div>
                <div>
                  <span>Average / Day</span>
                  <strong>Rs. {(totalRevenue / Math.max(salesRows.length, 1)).toFixed(2)}</strong>
                </div>
                <div>
                  <span>Best Day</span>
                  <strong>Rs. {maxRevenue.toFixed(2)}</strong>
                </div>
              </div>
            </section>
          </div>
        </>
      )}
    </div>
  );
}
