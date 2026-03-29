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
    refreshProfile
  } = useAuth();

  const [form, setForm] = useState({
    fullName: '',
    phone: '',
    avatarUrl: ''
  });
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

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
    </section>
  );
}
