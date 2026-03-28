import { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { authApi } from '../api/authApi';
import { useAuth } from '../context/AuthContext';

export default function TwoFactorAuthenticatorPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { setAuth } = useAuth();
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const twoFactorData = location.state;

  useEffect(() => {
    if (!twoFactorData) {
      navigate('/login', { replace: true });
    }
  }, [twoFactorData, navigate]);

  async function handleVerifyAuthenticator(event) {
    event.preventDefault();
    setLoading(true);
    setError('');

    if (!code || code.length !== 6) {
      setError('Please enter a valid 6-digit code');
      setLoading(false);
      return;
    }

    try {
      const response = await authApi.verifyAuthenticator({
        email: twoFactorData.email,
        sessionToken: twoFactorData.sessionToken,
        code: code
      });

      setAuth({
        token: response.token,
        email: response.email,
        role: response.role
      });

      navigate('/', { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  if (!twoFactorData) {
    return <div className="auth-page"><p>Redirecting...</p></div>;
  }

  return (
    <section className="auth-page">
      <form className="card auth-card" onSubmit={handleVerifyAuthenticator}>
        <h1>Authenticator Verification</h1>
        <p className="muted">Enter the 6-digit code from your authenticator app</p>

        {error && <p className="message error">{error}</p>}

        <label>
          Authenticator Code
          <input
            type="text"
            inputMode="numeric"
            maxLength="6"
            placeholder="000000"
            value={code}
            onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
            required
            className="otp-input"
          />
        </label>

        <button type="submit" className="btn btn-solid" disabled={loading || code.length !== 6}>
          {loading ? 'Verifying...' : 'Verify'}
        </button>

        <button
          type="button"
          className="btn btn-link"
          onClick={() => navigate('/two-factor-method', {
            state: { twoFactorData },
            replace: true
          })}
          disabled={loading}
        >
          Try Another Method
        </button>

        <button
          type="button"
          className="btn btn-link"
          onClick={() => navigate('/login', { replace: true })}
          disabled={loading}
        >
          Back to Login
        </button>
      </form>

      <style>{`
        .otp-input {
          font-size: 32px;
          letter-spacing: 8px;
          text-align: center;
          font-weight: bold;
          font-family: 'Courier New', monospace;
        }

        .btn-link {
          background: none;
          border: none;
          color: #007bff;
          text-decoration: none;
          cursor: pointer;
          padding: 0;
          font-size: 14px;
          margin-top: 8px;
        }

        .btn-link:hover:not(:disabled) {
          text-decoration: underline;
        }

        .btn-link:disabled {
          color: #ccc;
          cursor: not-allowed;
        }
      `}</style>
    </section>
  );
}
