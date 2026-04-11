import { useEffect, useRef, useState } from 'react';
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
  const googleButtonRef = useRef(null);
  const googleClientId = process.env.REACT_APP_GOOGLE_CLIENT_ID;

  useEffect(() => {
    if (!googleClientId || !window.google?.accounts?.id || !googleButtonRef.current) {
      return;
    }

    window.google.accounts.id.initialize({
      client_id: googleClientId,
      callback: async (response) => {
        const idToken = response?.credential;
        if (!idToken) {
          setError('Google sign-in failed. Please try again.');
          return;
        }

        setLoading(true);
        setError('');
        try {
          const authResponse = await authApi.googleLogin({ idToken });
          await setAuth({ token: authResponse.token, email: authResponse.email, role: authResponse.role });
          const target = location.state?.from || '/';
          navigate(target, { replace: true });
        } catch (err) {
          setError(err.message || 'Google sign-in failed.');
        } finally {
          setLoading(false);
        }
      }
    });

    googleButtonRef.current.innerHTML = '';
    window.google.accounts.id.renderButton(googleButtonRef.current, {
      theme: 'outline',
      size: 'large',
      shape: 'pill',
      text: 'signin_with',
      width: 260
    });
  }, [googleClientId, location.state, navigate, setAuth]);

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

        <p className="auth-inline-link">
          <Link to="/forgot-password">Forgot Password?</Link>
        </p>

        <button type="submit" className="btn btn-solid" disabled={loading}>
          {loading ? 'Signing in...' : 'Login'}
        </button>

        <div className="auth-divider"><span>or</span></div>
        <div className="google-signin-wrap" ref={googleButtonRef} />
        {!googleClientId && (
          <p className="hint">Google sign-in is not configured yet. Add REACT_APP_GOOGLE_CLIENT_ID.</p>
        )}

        <p className="muted">No account yet? <Link to="/signup">Create one</Link></p>
      </form>
    </section>
  );
}
