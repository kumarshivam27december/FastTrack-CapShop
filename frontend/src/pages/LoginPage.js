import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { authApi } from '../api/authApi';

export default function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { setAuth } = useAuth();
  const [form, setForm] = useState({ email: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(event) {
    event.preventDefault();
    setLoading(true);
    setError('');

    try {
      // Try login with 2FA support
      const response = await authApi.loginStep1(form);

      // If token is already present, no 2FA is required
      if (response.token) {
        await setAuth({ token: response.token, email: response.email, role: response.role });
        const target = location.state?.from || '/';
        navigate(target, { replace: true });
      } else {
        // 2FA is required
        navigate('/two-factor-method', {
          state: { twoFactorData: response },
          replace: true
        });
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="auth-page">
      <form className="card auth-card" onSubmit={handleSubmit}>
        <h1>Login</h1>
        <p className="muted">Access your account and continue shopping.</p>

        {error && <p className="message error">{error}</p>}

        <label>
          Email
          <input
            type="email"
            value={form.email}
            onChange={(e) => setForm((prev) => ({ ...prev, email: e.target.value }))}
            required
          />
        </label>

        <label>
          Password
          <input
            type="password"
            value={form.password}
            onChange={(e) => setForm((prev) => ({ ...prev, password: e.target.value }))}
            required
          />
        </label>

        <button type="submit" className="btn btn-solid" disabled={loading}>
          {loading ? 'Signing in...' : 'Login'}
        </button>

        <p className="muted">No account? <Link to="/signup">Create one</Link></p>
        <p className="hint">Admin seed login: admin@capshop.com / Admin@123</p>
      </form>
    </section>
  );
}
