import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { authApi } from '../api/authApi';

const AuthContext = createContext(null);

const TOKEN_KEY = 'capshop_token';
const ROLE_KEY = 'capshop_role';
const EMAIL_KEY = 'capshop_email';

export function AuthProvider({ children }) {
  const [token, setToken] = useState(localStorage.getItem(TOKEN_KEY));
  const [role, setRole] = useState(localStorage.getItem(ROLE_KEY));
  const [email, setEmail] = useState(localStorage.getItem(EMAIL_KEY));
  const [roles, setRoles] = useState([]);
  const [initialized, setInitialized] = useState(false);

  useEffect(() => {
    async function init() {
      if (!token) {
        setInitialized(true);
        return;
      }

      try {
        const me = await authApi.me(token);
        const roleList = Array.isArray(me?.roles) ? me.roles : [];
        setRoles(roleList);
        if (me?.email) {
          setEmail(me.email);
          localStorage.setItem(EMAIL_KEY, me.email);
        }

        if (!role && roleList.length > 0) {
          setRole(roleList[0]);
          localStorage.setItem(ROLE_KEY, roleList[0]);
        }
      } catch {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(ROLE_KEY);
        localStorage.removeItem(EMAIL_KEY);
        setToken(null);
        setRole(null);
        setEmail(null);
        setRoles([]);
      } finally {
        setInitialized(true);
      }
    }

    init();
  }, [token, role]);

  async function login(credentials) {
    const response = await authApi.login(credentials);
    setToken(response.token);
    setRole(response.role);
    setEmail(response.email);
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(ROLE_KEY, response.role);
    localStorage.setItem(EMAIL_KEY, response.email);

    const me = await authApi.me(response.token);
    setRoles(Array.isArray(me?.roles) ? me.roles : []);

    return response;
  }

  async function signup(payload) {
    await authApi.signup(payload);
  }

  function logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(ROLE_KEY);
    localStorage.removeItem(EMAIL_KEY);
    setToken(null);
    setRole(null);
    setEmail(null);
    setRoles([]);
  }

  const isAuthenticated = Boolean(token);
  const isAdmin = role === 'Admin' || roles.includes('Admin');

  const value = useMemo(
    () => ({
      token,
      role,
      email,
      roles,
      initialized,
      isAuthenticated,
      isAdmin,
      login,
      signup,
      logout
    }),
    [token, role, email, roles, initialized, isAuthenticated, isAdmin]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }

  return context;
}
