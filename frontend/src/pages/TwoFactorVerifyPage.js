import { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { authApi } from '../api/authApi';
import { useAuth } from '../context/AuthContext';

export default function TwoFactorVerifyPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { setAuth } = useAuth();
  const [otp, setOtp] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [resendDisabled, setResendDisabled] = useState(true);
  const [resendCountdown, setResendCountdown] = useState(0);

  const twoFactorData = location.state;

  useEffect(() => {
    if (!twoFactorData) {
      navigate('/login', { replace: true });
    }
  }, [twoFactorData, navigate]);

  useEffect(() => {
    if (resendCountdown > 0) {
      const timer = setTimeout(() => setResendCountdown(resendCountdown - 1), 1000);
      return () => clearTimeout(timer);
    } else {
      setResendDisabled(false);
    }
  }, [resendCountdown]);

  async function handleVerifyOtp(event) {
    event.preventDefault();
    setLoading(true);
    setError('');

    if (!otp || otp.length !== 6) {
      setError('Please enter a valid 6-digit OTP');
      setLoading(false);
      return;
    }

    try {
      const response = await authApi.verifyOtp({
        email: twoFactorData.email,
        sessionToken: twoFactorData.sessionToken,
        otp: otp,
        method: twoFactorData.method
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

  async function handleResendOtp() {
    setLoading(true);
    setError('');

    try {
      await authApi.sendOtp({
        email: twoFactorData.email,
        sessionToken: twoFactorData.sessionToken,
        method: twoFactorData.method
      });

      setResendDisabled(true);
      setResendCountdown(60);
      setError('');
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
      <form className="card auth-card" onSubmit={handleVerifyOtp}>
        <h1>Verify OTP</h1>
        <p className="muted">
          Enter the {twoFactorData.method === 'SMS' ? 'SMS' : 'email'} code sent to {twoFactorData.destination}
        </p>

        {error && <p className="message error">{error}</p>}

        <label>
          Verification Code
          <input
            type="text"
            inputMode="numeric"
            maxLength="6"
            placeholder="000000"
            value={otp}
            onChange={(e) => setOtp(e.target.value.replace(/\D/g, ''))}
            required
            className="otp-input"
          />
        </label>

        <button type="submit" className="btn btn-solid" disabled={loading || otp.length !== 6}>
          {loading ? 'Verifying...' : 'Verify'}
        </button>

        <div className="otp-actions">
          <button
            type="button"
            className="btn btn-link"
            onClick={handleResendOtp}
            disabled={resendDisabled || loading}
          >
            {resendDisabled && resendCountdown > 0
              ? `Resend code in ${resendCountdown}s`
              : "Didn't receive the code? Resend"}
          </button>

          <button
            type="button"
            className="btn btn-link"
            onClick={() => navigate('/login', { replace: true })}
            disabled={loading}
          >
            Back to Login
          </button>
        </div>
      </form>

      <style>{`
        .otp-input {
          font-size: 32px;
          letter-spacing: 8px;
          text-align: center;
          font-weight: bold;
          font-family: 'Courier New', monospace;
        }

        .otp-actions {
          display: flex;
          flex-direction: column;
          gap: 8px;
          margin-top: 16px;
        }

        .btn-link {
          background: none;
          border: none;
          color: #007bff;
          text-decoration: none;
          cursor: pointer;
          padding: 0;
          font-size: 14px;
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
