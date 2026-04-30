import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import ProgressIndicator from '../components/ProgressIndicator';

export default function CheckoutPage() {
  const navigate = useNavigate();
  const { token } = useAuth();
  const { cart } = useCart();

  const [address, setAddress] = useState({
    fullName: '',
    street: '',
    city: '',
    state: '',
    pincode: '',
    phone: ''
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const canStart = useMemo(() => {
    return Object.values(address).every((value) => String(value).trim().length > 0);
  }, [address]);

  if (!cart.items?.length) {
    return (
      <section className="section card">
        <h1>Cart is empty</h1>
        <p>Add products before checkout.</p>
        <Link to="/catalog" className="btn btn-solid">Back to Catalog</Link>
      </section>
    );
  }

  async function handleStartCheckout(event) {
    event.preventDefault();
    setBusy(true);
    setError('');

    try {
      const response = await orderApi.startCheckout(token, { address });
      // Navigate to payment page with order ID
      navigate(`/payment/${response.orderId}`, { state: { address, checkoutInfo: response } });
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="section">
      <div className="checkout-header">
        <h1>Secure Checkout</h1>
        <p>Enter your shipping address</p>
      </div>

      <ProgressIndicator currentStep={0} />

      {error && <div className="message error"><strong>Error:</strong> {error}</div>}

      <div className="checkout-address-container">
        <form className="card checkout-card address-form" onSubmit={handleStartCheckout}>
          <div className="step-header">
            <div className="step-number">1</div>
            <h2>Shipping Address</h2>
          </div>

          <div className="form-grid">
            <div className="form-group">
              <label htmlFor="fullName">Full Name *</label>
              <input
                id="fullName"
                value={address.fullName}
                onChange={(e) => setAddress((prev) => ({ ...prev, fullName: e.target.value }))}
                placeholder="Enter your full name"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="phone">Phone Number *</label>
              <input
                id="phone"
                value={address.phone}
                onChange={(e) => setAddress((prev) => ({ ...prev, phone: e.target.value }))}
                placeholder="+91-XXXXXXXXXX"
                required
              />
            </div>

            <div className="form-group full-width">
              <label htmlFor="street">Street Address *</label>
              <input
                id="street"
                value={address.street}
                onChange={(e) => setAddress((prev) => ({ ...prev, street: e.target.value }))}
                placeholder="123 Main Street"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="city">City *</label>
              <input
                id="city"
                value={address.city}
                onChange={(e) => setAddress((prev) => ({ ...prev, city: e.target.value }))}
                placeholder="Patna"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="state">State *</label>
              <input
                id="state"
                value={address.state}
                onChange={(e) => setAddress((prev) => ({ ...prev, state: e.target.value }))}
                placeholder="Bihar"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="pincode">Pincode *</label>
              <input
                id="pincode"
                value={address.pincode}
                onChange={(e) => setAddress((prev) => ({ ...prev, pincode: e.target.value }))}
                placeholder="110001"
                required
              />
            </div>
          </div>

          <button type="submit" className="btn btn-solid btn-large" disabled={!canStart || busy}>
            {busy ? '⏳ Processing...' : '→ Continue to Payment'}
          </button>

          <Link to="/cart" className="btn btn-outline btn-large">
            ← Back to Cart
          </Link>
        </form>
      </div>
    </section>
  );
}
