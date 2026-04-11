import { Navigate, useLocation } from 'react-router-dom';
import LoadingSpinner from './LoadingSpinner';
import { useAuth } from '../context/AuthContext';

export default function CustomerRoute({ children }) {
  const { initialized, isAuthenticated, isAdmin } = useAuth();
  const location = useLocation();

  if (!initialized) {
    return <LoadingSpinner label="Checking session..." />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  if (isAdmin) {
    return <Navigate to="/" replace />;
  }

  return children;
}
