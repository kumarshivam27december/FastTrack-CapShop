import { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { authApi } from '../api/authApi';

export default function TwoFactorMethodPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const twoFactorData = location.state?.twoFactorData;

  useEffect(() => {
    if (!twoFactorData) {
      navigate('/login', { replace: true });
    }
  }, [twoFactorData, navigate]);

  async function handleMethodSelect(method) {
    setLoading(true);
    setError('');

    try {
      const response = await authApi.sendOtp({
        email: twoFactorData.email,
        sessionToken: twoFactorData.sessionToken,
        method: method
      });

      navigate('/two-factor-verify', {
        state: {
          method: method,
          email: twoFactorData.email,
          sessionToken: twoFactorData.sessionToken,
          destination: response.destination
        },
        replace: true
      });
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
      <div className="card auth-card">
        <h1>Two-Factor Authentication</h1>
        <p className="muted">Choose your authentication method</p>

        {error && <p className="message error">{error}</p>}

        <div className="twofa-methods">
          {twoFactorData.availableMethods?.includes('SMS') && (
            <button
              className="btn btn-2fa-method"
              onClick={() => handleMethodSelect('SMS')}
              disabled={loading}
            >
              <div className="method-icon">SMS</div>
              <div className="method-name">SMS</div>
              <div className="method-detail">Code sent to {twoFactorData.phoneNumber}</div>
            </button>
          )}

          {twoFactorData.availableMethods?.includes('EMAIL') && (
            <button
              className="btn btn-2fa-method"
              onClick={() => handleMethodSelect('EMAIL')}
              disabled={loading}
            >
              <div className="method-icon">MAIL</div>
              <div className="method-name">Email</div>
              <div className="method-detail">Code sent to your email</div>
            </button>
          )}

          {twoFactorData.availableMethods?.includes('AUTHENTICATOR') && (
            <button
              className="btn btn-2fa-method"
              onClick={() => navigate('/two-factor-authenticator', {
                state: twoFactorData,
                replace: true
              })}
              disabled={loading}
            >
              <div className="method-icon">APP</div>
              <div className="method-name">Authenticator App</div>
              <div className="method-detail">Use your authenticator app</div>
            </button>
          )}
        </div>

        <button
          className="btn btn-secondary"
          onClick={() => navigate('/login', { replace: true })}
          disabled={loading}
        >
          Back to Login
        </button>
      </div>

      <style>{`
        .twofa-methods {
          display: flex;
          flex-direction: column;
          gap: 12px;
          margin: 24px 0;
        }

        .btn-2fa-method {
          display: flex;
          align-items: center;
          gap: 16px;
          padding: 16px;
          border: 2px solid #e0e0e0;
          border-radius: 8px;
          background: white;
          cursor: pointer;
          transition: all 0.3s ease;
          text-align: left;
        }

        .btn-2fa-method:hover:not(:disabled) {
          border-color: #007bff;
          background: #f8f9ff;
        }

        .btn-2fa-method:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }

        .method-icon {
          font-size: 24px;
        }

        .method-name {
          font-weight: 600;
          font-size: 16px;
          color: #333;
        }

        .method-detail {
          font-size: 12px;
          color: #999;
        }

        .btn-secondary {
          margin-top: 16px;
          background: #f0f0f0;
          color: #333;
        }

        .btn-secondary:hover:not(:disabled) {
          background: #e0e0e0;
        }
      `}</style>
    </section>
  );
}
