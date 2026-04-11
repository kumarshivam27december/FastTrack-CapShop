import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { orderApi } from '../api/orderApi';
import { useAuth } from './AuthContext';

const CartContext = createContext(null);

const emptyCart = {
  cartId: 0,
  items: [],
  totalAmount: 0,
  itemCount: 0
};

export function CartProvider({ children }) {
  const { token, isAuthenticated, isAdmin, initialized } = useAuth();
  const [cart, setCart] = useState(emptyCart);
  const [loading, setLoading] = useState(false);

  const refreshCart = useCallback(async () => {
    if (!initialized) {
      return null;
    }

    if (!isAuthenticated || !token || isAdmin) {
      setCart(emptyCart);
      return null;
    }

    setLoading(true);
    try {
      const response = await orderApi.getCart(token);
      setCart(response || emptyCart);
      return response;
    } catch (err) {
      if (err?.status === 401 || err?.status === 403) {
        setCart(emptyCart);
        return null;
      }

      throw err;
    } finally {
      setLoading(false);
    }
  }, [initialized, isAuthenticated, token, isAdmin]);

  useEffect(() => {
    refreshCart().catch(() => {
      setCart(emptyCart);
    });
  }, [refreshCart]);

  const addToCart = useCallback(async (productId, quantity) => {
    if (!token || isAdmin) {
      throw new Error('Cart is not available for admin accounts.');
    }

    const response = await orderApi.addToCart(token, { productId, quantity });
    setCart(response);
    return response;
  }, [token, isAdmin]);

  const updateCartItem = useCallback(async (itemId, quantity) => {
    if (!token || isAdmin) {
      throw new Error('Cart is not available for admin accounts.');
    }

    const response = await orderApi.updateCartItem(token, itemId, { quantity });
    setCart(response);
    return response;
  }, [token, isAdmin]);

  const removeCartItem = useCallback(async (itemId) => {
    if (!token || isAdmin) {
      throw new Error('Cart is not available for admin accounts.');
    }

    await orderApi.removeCartItem(token, itemId);
    await refreshCart();
  }, [token, isAdmin, refreshCart]);

  const value = useMemo(
    () => ({
      cart,
      loading,
      refreshCart,
      addToCart,
      updateCartItem,
      removeCartItem
    }),
    [cart, loading, refreshCart, addToCart, updateCartItem, removeCartItem]
  );

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used inside CartProvider');
  }

  return context;
}
