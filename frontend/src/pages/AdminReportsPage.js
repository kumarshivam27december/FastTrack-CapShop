import { useCallback, useEffect, useMemo, useState } from 'react';
import { adminApi } from '../api/adminApi';
import { useAuth } from '../context/AuthContext';
import LoadingSpinner from '../components/LoadingSpinner';

export default function AdminReportsPage() {
  const { token } = useAuth();
  const [statusSplit, setStatusSplit] = useState([]);
  const [salesRows, setSalesRows] = useState([]);
  const [salesFrom, setSalesFrom] = useState(new Date(Date.now() - 7 * 86400000).toISOString().slice(0, 10));
  const [salesTo, setSalesTo] = useState(new Date().toISOString().slice(0, 10));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const totalSales = useMemo(
    () => salesRows.reduce((sum, row) => sum + Number(row.revenue || 0), 0),
    [salesRows]
  );

  const loadStatusSplit = useCallback(async () => {
    const data = await adminApi.getStatusSplit(token);
    setStatusSplit(Array.isArray(data) ? data : []);
  }, [token]);

  const loadSales = useCallback(async () => {
    const data = await adminApi.getSalesReport(token, salesFrom, salesTo);
    setSalesRows(Array.isArray(data) ? data : []);
  }, [token, salesFrom, salesTo]);

  const loadAll = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      await Promise.all([loadStatusSplit(), loadSales()]);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [loadStatusSplit, loadSales]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

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

  if (loading) {
    return <LoadingSpinner label="Loading reports..." />;
  }

  return (
    <div>
      <h1>Reports</h1>
      {error && <p className="message error">{error}</p>}

      <section className="card section">
        <div className="section-head">
          <h2>Order Status Split</h2>
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

      <section className="card section">
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
          <button type="button" className="btn btn-outline" onClick={loadSales}>Refresh</button>
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
  );
}
