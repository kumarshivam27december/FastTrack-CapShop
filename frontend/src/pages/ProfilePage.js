import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';

function toInitials(value) {
  const safe = String(value || '').trim();
  if (!safe) return '?';

  const parts = safe.split(/\s+/).filter(Boolean);
  if (parts.length === 1) {
    return parts[0].slice(0, 1).toUpperCase();
  }

  return `${parts[0].slice(0, 1)}${parts[1].slice(0, 1)}`.toUpperCase();
}

export default function ProfilePage() {
  const {
    email,
    role,
    roles,
    fullName,
    phone,
    avatarUrl,
    updateProfile,
    changePassword,
    isAuthenticatorEnabled,
    setupMyAuthenticator,
    enableMyAuthenticator,
    refreshProfile
  } = useAuth();

  const [form, setForm] = useState({
    fullName: '',
    phone: '',
    avatarUrl: ''
  });
  const [saving, setSaving] = useState(false);
  const [changingPassword, setChangingPassword] = useState(false);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [passwordMessage, setPasswordMessage] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [authenticatorSetup, setAuthenticatorSetup] = useState(null);
  const [authenticatorCode, setAuthenticatorCode] = useState('');
  const [authenticatorBusy, setAuthenticatorBusy] = useState(false);
  const [authenticatorError, setAuthenticatorError] = useState('');
  const [authenticatorMessage, setAuthenticatorMessage] = useState('');

  useEffect(() => {
    let isMounted = true;

    async function load() {
      setLoading(true);
      setError('');
      try {
        await refreshProfile();
      } catch (err) {
        if (isMounted) {
          setError(err.message || 'Could not load profile.');
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    load();

    return () => {
      isMounted = false;
    };
  }, [refreshProfile]);

  useEffect(() => {
    setForm({
      fullName: fullName || '',
      phone: phone || '',
      avatarUrl: avatarUrl || ''
    });
  }, [fullName, phone, avatarUrl]);

  function updateField(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSave(event) {
    event.preventDefault();
    setError('');
    setMessage('');
    setSaving(true);

    try {
      await updateProfile(form);
      setMessage('Profile updated successfully.');
    } catch (err) {
      setError(err.message || 'Could not save profile. Please try again.');
    } finally {
      setSaving(false);
    }
  }

  function updatePasswordField(field, value) {
    setPasswordForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleChangePassword(event) {
    event.preventDefault();
    setPasswordMessage('');
    setPasswordError('');

    const currentPassword = passwordForm.currentPassword.trim();
    const newPassword = passwordForm.newPassword.trim();
    const confirmPassword = passwordForm.confirmPassword.trim();

    if (!currentPassword || !newPassword || !confirmPassword) {
      setPasswordError('Please fill all password fields.');
      return;
    }

    if (newPassword.length < 6) {
      setPasswordError('New password must be at least 6 characters.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setPasswordError('New password and confirm password do not match.');
      return;
    }

    setChangingPassword(true);
    try {
      await changePassword({ currentPassword, newPassword });
      setPasswordMessage('Password changed successfully.');
      setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
    } catch (err) {
      setPasswordError(err.message || 'Could not change password. Please try again.');
    } finally {
      setChangingPassword(false);
    }
  }

  async function handleSetupAuthenticator() {
    setAuthenticatorBusy(true);
    setAuthenticatorError('');
    setAuthenticatorMessage('');

    try {
      const response = await setupMyAuthenticator();
      setAuthenticatorSetup(response);
      setAuthenticatorMessage('QR generated. Scan it in Microsoft Authenticator and verify with a 6-digit code.');
    } catch (err) {
      setAuthenticatorError(err.message || 'Could not generate authenticator QR code.');
    } finally {
      setAuthenticatorBusy(false);
    }
  }

  async function handleEnableAuthenticator(event) {
    event.preventDefault();
    setAuthenticatorError('');
    setAuthenticatorMessage('');

    const code = authenticatorCode.trim();
    if (!code || code.length !== 6) {
      setAuthenticatorError('Enter a valid 6-digit authenticator code.');
      return;
    }

    setAuthenticatorBusy(true);
    try {
      await enableMyAuthenticator({ code });
      setAuthenticatorCode('');
      setAuthenticatorSetup(null);
      setAuthenticatorMessage('Authenticator enabled. You will now see Authenticator as a login OTP method.');
    } catch (err) {
      setAuthenticatorError(err.message || 'Could not enable authenticator.');
    } finally {
      setAuthenticatorBusy(false);
    }
  }

  function handleAvatarUpload(event) {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      setError('Please choose an image file.');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      setError('');
      updateField('avatarUrl', typeof reader.result === 'string' ? reader.result : '');
    };
    reader.onerror = () => setError('Could not read selected image.');
    reader.readAsDataURL(file);
  }

  const effectiveAvatar = form.avatarUrl;
  const effectiveName = form.fullName || email;
  const effectiveRole = role || (Array.isArray(roles) && roles.length > 0 ? roles[0] : 'Customer');

  if (loading) {
    return (
      <section className="section profile-page">
        <div className="card">
          <p>Loading profile...</p>
        </div>
      </section>
    );
  }

  return (
    <section className="section profile-page">
      <div className="section-head">
        <h1>My Profile</h1>
      </div>

      <form className="card profile-card" onSubmit={handleSave}>
        <div className="profile-header-row">
          <div className="profile-avatar-preview-wrap">
            {effectiveAvatar ? (
              <img src={effectiveAvatar} alt="Profile avatar" className="profile-avatar-preview" />
            ) : (
              <div className="profile-avatar-fallback">{toInitials(effectiveName)}</div>
            )}
          </div>

          <div className="profile-meta">
            <p><strong>{effectiveName || 'User'}</strong></p>
            <p className="muted">{email}</p>
            <p className="muted">Role: {effectiveRole}</p>
          </div>
        </div>

        {error && <p className="message error">{error}</p>}
        {message && <p className="message success">{message}</p>}

        <label>
          Full Name
          <input
            value={form.fullName}
            onChange={(event) => updateField('fullName', event.target.value)}
            placeholder="Enter your full name"
          />
        </label>

        <label>
          Phone
          <input
            value={form.phone}
            onChange={(event) => updateField('phone', event.target.value)}
            placeholder="Enter your phone number"
          />
        </label>

        <label>
          Email
          <input value={email || ''} disabled readOnly />
        </label>

        <label>
          Profile Photo
          <input type="file" accept="image/*" onChange={handleAvatarUpload} />
        </label>

        <div className="inline-actions">
          <button type="submit" className="btn btn-solid" disabled={saving}>
            {saving ? 'Saving...' : 'Save Profile'}
          </button>
          {form.avatarUrl && (
            <button
              type="button"
              className="btn btn-outline"
              onClick={() => updateField('avatarUrl', '')}
            >
              Remove Photo
            </button>
          )}
        </div>
      </form>

      <form className="card profile-card section" onSubmit={handleChangePassword}>
        <div className="section-head">
          <h2>Change Password</h2>
        </div>

        {passwordError && <p className="message error">{passwordError}</p>}
        {passwordMessage && <p className="message success">{passwordMessage}</p>}

        <label>
          Current Password
          <input
            type="password"
            value={passwordForm.currentPassword}
            onChange={(event) => updatePasswordField('currentPassword', event.target.value)}
            autoComplete="current-password"
          />
        </label>

        <label>
          New Password
          <input
            type="password"
            value={passwordForm.newPassword}
            onChange={(event) => updatePasswordField('newPassword', event.target.value)}
            autoComplete="new-password"
          />
        </label>

        <label>
          Confirm New Password
          <input
            type="password"
            value={passwordForm.confirmPassword}
            onChange={(event) => updatePasswordField('confirmPassword', event.target.value)}
            autoComplete="new-password"
          />
        </label>

        <div className="inline-actions">
          <button type="submit" className="btn btn-solid" disabled={changingPassword}>
            {changingPassword ? 'Changing...' : 'Change Password'}
          </button>
        </div>
      </form>

      <section className="card profile-card section">
        <div className="section-head">
          <h2>Authenticator App (QR)</h2>
        </div>

        {isAuthenticatorEnabled ? (
          <p className="message success">Authenticator is enabled for your account.</p>
        ) : (
          <p className="muted">Enable Microsoft Authenticator to use app-based OTP at login.</p>
        )}

        {authenticatorError && <p className="message error">{authenticatorError}</p>}
        {authenticatorMessage && <p className="message success">{authenticatorMessage}</p>}

        {!isAuthenticatorEnabled && (
          <div className="stack-actions">
            <button
              type="button"
              className="btn btn-outline"
              onClick={handleSetupAuthenticator}
              disabled={authenticatorBusy}
            >
              {authenticatorBusy ? 'Preparing QR...' : 'Generate QR Code'}
            </button>

            {authenticatorSetup?.qrCodeImage && (
              <form className="profile-authenticator-box" onSubmit={handleEnableAuthenticator}>
                <img
                  src={authenticatorSetup.qrCodeImage}
                  alt="Authenticator QR code"
                  className="profile-authenticator-qr"
                />
                <p className="hint">If scanning fails, manually enter this key in Microsoft Authenticator:</p>
                <p className="profile-authenticator-secret">{authenticatorSetup.secretKey}</p>

                <label>
                  6-digit app code
                  <input
                    type="text"
                    inputMode="numeric"
                    maxLength="6"
                    value={authenticatorCode}
                    onChange={(event) => setAuthenticatorCode(event.target.value.replace(/\D/g, ''))}
                    placeholder="000000"
                  />
                </label>

                <button type="submit" className="btn btn-solid" disabled={authenticatorBusy || authenticatorCode.length !== 6}>
                  {authenticatorBusy ? 'Enabling...' : 'Enable Authenticator'}
                </button>
              </form>
            )}
          </div>
        )}
      </section>
    </section>
  );
}
