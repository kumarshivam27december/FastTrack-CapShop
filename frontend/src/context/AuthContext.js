import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { authApi } from '../api/authApi';

const AuthContext = createContext(null);

const TOKEN_KEY = 'capshop_token';
const ROLE_KEY = 'capshop_role';
const EMAIL_KEY = 'capshop_email';
const FULL_NAME_KEY = 'capshop_full_name';
const PHONE_KEY = 'capshop_phone';
const AVATAR_URL_KEY = 'capshop_avatar_url';
const AUTH_APP_ENABLED_KEY = 'capshop_auth_app_enabled';
const GOOGLE_ACCOUNT_KEY = 'capshop_is_google_account';

export function AuthProvider({ children }) {
  const [token, setToken] = useState(localStorage.getItem(TOKEN_KEY));
  const [role, setRole] = useState(localStorage.getItem(ROLE_KEY));
  const [email, setEmail] = useState(localStorage.getItem(EMAIL_KEY));
  const [fullName, setFullName] = useState(localStorage.getItem(FULL_NAME_KEY) || '');
  const [phone, setPhone] = useState(localStorage.getItem(PHONE_KEY) || '');
  const [avatarUrl, setAvatarUrl] = useState(localStorage.getItem(AVATAR_URL_KEY) || '');
  const [isAuthenticatorEnabled, setIsAuthenticatorEnabled] = useState(
    localStorage.getItem(AUTH_APP_ENABLED_KEY) === 'true'
  );
  const [isGoogleAccount, setIsGoogleAccount] = useState(
    localStorage.getItem(GOOGLE_ACCOUNT_KEY) === 'true'
  );
  const [roles, setRoles] = useState([]);
  const [initialized, setInitialized] = useState(false);

  const applyProfile = useCallback((profile) => {
    const roleList = Array.isArray(profile?.roles) ? profile.roles : [];
    const nextEmail = profile?.email || '';
    const nextName = profile?.fullName || '';
    const nextPhone = profile?.phone || '';
    const nextAvatarUrl = profile?.avatarUrl || '';
    const nextAuthenticatorEnabled = Boolean(profile?.isAuthenticatorEnabled);
    const nextIsGoogleAccount = Boolean(profile?.isGoogleAccount);

    setRoles(roleList);
    setEmail(nextEmail);
    setFullName(nextName);
    setPhone(nextPhone);
    setAvatarUrl(nextAvatarUrl);
    setIsAuthenticatorEnabled(nextAuthenticatorEnabled);
    setIsGoogleAccount(nextIsGoogleAccount);

    localStorage.setItem(EMAIL_KEY, nextEmail);
    localStorage.setItem(FULL_NAME_KEY, nextName);
    localStorage.setItem(PHONE_KEY, nextPhone);
    localStorage.setItem(AUTH_APP_ENABLED_KEY, String(nextAuthenticatorEnabled));
    localStorage.setItem(GOOGLE_ACCOUNT_KEY, String(nextIsGoogleAccount));

    if (nextAvatarUrl) {
      localStorage.setItem(AVATAR_URL_KEY, nextAvatarUrl);
    } else {
      localStorage.removeItem(AVATAR_URL_KEY);
    }

    if (roleList.length > 0) {
      setRole(roleList[0]);
      localStorage.setItem(ROLE_KEY, roleList[0]);
    }
  }, []);

  useEffect(() => {
    async function init() {
      if (!token) {
        setInitialized(true);
        return;
      }

      try {
        const me = await authApi.me(token);
        applyProfile(me);
      } catch {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(ROLE_KEY);
        localStorage.removeItem(EMAIL_KEY);
        localStorage.removeItem(FULL_NAME_KEY);
        localStorage.removeItem(PHONE_KEY);
        localStorage.removeItem(AVATAR_URL_KEY);
        localStorage.removeItem(AUTH_APP_ENABLED_KEY);
        localStorage.removeItem(GOOGLE_ACCOUNT_KEY);
        setToken(null);
        setRole(null);
        setEmail(null);
        setFullName('');
        setPhone('');
        setAvatarUrl('');
        setIsAuthenticatorEnabled(false);
        setIsGoogleAccount(false);
        setRoles([]);
      } finally {
        setInitialized(true);
      }
    }

    init();
  }, [token, applyProfile]);

  const login = useCallback(async (credentials) => {
    const response = await authApi.login(credentials);
    setToken(response.token);
    localStorage.setItem(TOKEN_KEY, response.token);

    const me = await authApi.me(response.token);
    applyProfile(me);

    return response;
  }, [applyProfile]);

  const signup = useCallback(async (payload) => {
    await authApi.signup(payload);
  }, []);

  const setAuth = useCallback(async (authData) => {
    // Direct method to set auth after 2FA verification
    setToken(authData.token);
    localStorage.setItem(TOKEN_KEY, authData.token);

    if (authData.token) {
      const me = await authApi.me(authData.token);
      applyProfile(me);
    }
  }, [applyProfile]);

  const refreshProfile = useCallback(async () => {
    if (!token) return null;
    const me = await authApi.me(token);
    applyProfile(me);
    return me;
  }, [token, applyProfile]);

  const updateProfile = useCallback(async (payload) => {
    if (!token) {
      throw new Error('Not authenticated.');
    }

    const me = await authApi.updateMe(token, payload);
    applyProfile(me);
    return me;
  }, [token, applyProfile]);

  const changePassword = useCallback(async (payload) => {
    if (!token) {
      throw new Error('Not authenticated.');
    }

    return authApi.changePassword(token, payload);
  }, [token]);

  const setupMyAuthenticator = useCallback(async () => {
    if (!token) {
      throw new Error('Not authenticated.');
    }

    return authApi.setupMyAuthenticator(token);
  }, [token]);

  const enableMyAuthenticator = useCallback(async (payload) => {
    if (!token) {
      throw new Error('Not authenticated.');
    }

    const me = await authApi.enableMyAuthenticator(token, payload);
    applyProfile(me);
    return me;
  }, [token, applyProfile]);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(ROLE_KEY);
    localStorage.removeItem(EMAIL_KEY);
    localStorage.removeItem(FULL_NAME_KEY);
    localStorage.removeItem(PHONE_KEY);
    localStorage.removeItem(AVATAR_URL_KEY);
    localStorage.removeItem(AUTH_APP_ENABLED_KEY);
    localStorage.removeItem(GOOGLE_ACCOUNT_KEY);
    setToken(null);
    setRole(null);
    setEmail(null);
    setFullName('');
    setPhone('');
    setAvatarUrl('');
    setIsAuthenticatorEnabled(false);
    setIsGoogleAccount(false);
    setRoles([]);
  }, []);

  const isAuthenticated = Boolean(token);
  const isAdmin = role === 'Admin' || roles.includes('Admin');

  const value = useMemo(() => ({
    token,
    role,
    email,
    fullName,
    phone,
    avatarUrl,
    isGoogleAccount,
    isAuthenticatorEnabled,
    roles,
    initialized,
    isAuthenticated,
    isAdmin,
    login,
    setAuth,
    refreshProfile,
    updateProfile,
    changePassword,
    setupMyAuthenticator,
    enableMyAuthenticator,
    signup,
    logout
  }), [
    token,
    role,
    email,
    fullName,
    phone,
    avatarUrl,
    isGoogleAccount,
    isAuthenticatorEnabled,
    roles,
    initialized,
    isAuthenticated,
    isAdmin,
    login,
    setAuth,
    refreshProfile,
    updateProfile,
    changePassword,
    setupMyAuthenticator,
    enableMyAuthenticator,
    signup,
    logout
  ]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }

  return context;
}
