import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/authApi';

export default function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [step, setStep] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [form, setForm] = useState({
    email: '',
    otp: '',
    newPassword: '',
    confirmPassword: ''
  });

  async function handleSendOtp(event) {
    event.preventDefault();
    setLoading(true);
    setError('');
    setSuccess('');

    try {
      const response = await authApi.forgotPassword({ email: form.email });
      setSuccess(response?.message || 'If this email exists, OTP has been sent.');
      setStep(2);
    } catch (err) {
      setError(err.message || 'Failed to send OTP.');
    } finally {
      setLoading(false);
    }
  }

  async function handleResetPassword(event) {
    event.preventDefault();
    setLoading(true);
    setError('');
    setSuccess('');

    if (form.newPassword.length < 6) {
      setLoading(false);
      setError('New password must be at least 6 characters.');
      return;
    }

    if (form.newPassword !== form.confirmPassword) {
      setLoading(false);
      setError('Password and confirm password do not match.');
      return;
    }

    try {
      await authApi.resetPassword({
        email: form.email,
        otp: form.otp,
        newPassword: form.newPassword
      });
      setSuccess('Password reset successful. You can now log in with your new password.');
      setTimeout(() => navigate('/login'), 1200);
    } catch (err) {
      setError(err.message || 'Failed to reset password.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="auth-page">
      <form className="card auth-card" onSubmit={step === 1 ? handleSendOtp : handleResetPassword}>
        <h1>Forgot Password</h1>
        <p className="muted">
          {step === 1
            ? 'Enter your email and we will send an OTP to reset your password.'
            : 'Enter the OTP sent to your email and choose a new password.'}
        </p>

        {error && <p className="message error">{error}</p>}
        {success && <p className="message success">{success}</p>}

        <label>
          Email
          <input
            type="email"
            value={form.email}
            onChange={(e) => setForm((prev) => ({ ...prev, email: e.target.value }))}
            required
            disabled={step === 2}
          />
        </label>

        {step === 2 && (
          <>
            <label>
              OTP
              <input
                type="text"
                value={form.otp}
                onChange={(e) => setForm((prev) => ({ ...prev, otp: e.target.value }))}
                required
                maxLength={6}
                inputMode="numeric"
              />
            </label>

            <label>
              New Password
              <input
                type="password"
                value={form.newPassword}
                onChange={(e) => setForm((prev) => ({ ...prev, newPassword: e.target.value }))}
                required
              />
            </label>

            <label>
              Confirm Password
              <input
                type="password"
                value={form.confirmPassword}
                onChange={(e) => setForm((prev) => ({ ...prev, confirmPassword: e.target.value }))}
                required
              />
            </label>
          </>
        )}

        <button type="submit" className="btn btn-solid" disabled={loading}>
          {loading ? 'Please wait...' : step === 1 ? 'Send OTP' : 'Reset Password'}
        </button>

        {step === 2 && (
          <button
            type="button"
            className="btn btn-outline"
            disabled={loading}
            onClick={() => setStep(1)}
          >
            Change Email
          </button>
        )}

        <p className="muted">Remember your password? <Link to="/login">Back to login</Link></p>
      </form>
    </section>
  );
}
