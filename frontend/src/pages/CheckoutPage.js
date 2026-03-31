import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';

export default function CheckoutPage() {
  const navigate = useNavigate();
  const { token } = useAuth();
  const { cart, refreshCart } = useCart();

  const [address, setAddress] = useState({
    fullName: '',
    street: '',
    city: '',
    state: '',
    pincode: '',
    phone: ''
  });
  const [paymentMethod, setPaymentMethod] = useState('UPI');
  const [simulateSuccess, setSimulateSuccess] = useState(true);
  const [checkoutInfo, setCheckoutInfo] = useState(null);
  const [paymentInfo, setPaymentInfo] = useState(null);
  const [orderInfo, setOrderInfo] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const canStart = useMemo(() => {
    return Object.values(address).every((value) => String(value).trim().length > 0);
  }, [address]);

  if (!cart.items?.length && !checkoutInfo) {
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
      setCheckoutInfo(response);
      setPaymentInfo(null);
      setOrderInfo(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function handleSimulatePayment() {
    if (!checkoutInfo) return;

    setBusy(true);
    setError('');

    try {
      const response = await orderApi.simulatePayment(token, {
        orderId: checkoutInfo.orderId,
        paymentMethod,
        simulateSuccess
      });

      // Payment is now async through RabbitMQ. Poll order status briefly for completion.
      let paid = false;
      for (let i = 0; i < 15; i += 1) {
        // eslint-disable-next-line no-await-in-loop
        await new Promise((resolve) => setTimeout(resolve, 1000));
        // eslint-disable-next-line no-await-in-loop
        const order = await orderApi.getOrderById(token, checkoutInfo.orderId);
        if (order?.status === 'Paid') {
          paid = true;
          break;
        }
      }

      setPaymentInfo({
        ...response,
        success: paid,
        message: paid
          ? 'Payment successful'
          : 'Payment is still processing. Please wait a few seconds and try again.'
      });
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function handlePlaceOrder() {
    if (!checkoutInfo) return;

    setBusy(true);
    setError('');

    try {
      const response = await orderApi.placeOrder(token, checkoutInfo.orderId);
      setOrderInfo(response);
      await refreshCart();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="section">
      <h1>Checkout</h1>
      {error && <p className="message error">{error}</p>}

      <div className="checkout-grid">
        <form className="card" onSubmit={handleStartCheckout}>
          <h2>1. Shipping Address</h2>

          {Object.keys(address).map((field) => (
            <label key={field}>
              {field}
              <input
                value={address[field]}
                onChange={(e) => setAddress((prev) => ({ ...prev, [field]: e.target.value }))}
                required
              />
            </label>
          ))}

          <button type="submit" className="btn btn-solid" disabled={!canStart || busy}>
            {busy ? 'Starting...' : 'Start Checkout'}
          </button>
        </form>

        <div className="card">
          <h2>2. Payment + Place Order</h2>

          {!checkoutInfo && <p className="message">Start checkout first.</p>}

          {checkoutInfo && (
            <>
              <p>Order: <strong>{checkoutInfo.orderNumber}</strong></p>
              <p>Total: Rs. {Number(checkoutInfo.totalAmount).toFixed(2)}</p>

              <label>
                Payment Method
                <select value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)}>
                  <option value="UPI">UPI</option>
                  <option value="Card">Card</option>
                  <option value="COD">COD</option>
                </select>
              </label>

              <label className="inline-actions">
                <input
                  type="checkbox"
                  checked={simulateSuccess}
                  onChange={(e) => setSimulateSuccess(e.target.checked)}
                />
                Simulate success
              </label>

              <button type="button" className="btn btn-outline" onClick={handleSimulatePayment} disabled={busy}>
                Simulate Payment
              </button>

              {paymentInfo && (
                <div className="message">
                  <p>{paymentInfo.message}</p>
                  <p>Txn: {paymentInfo.transactionId}</p>
                </div>
              )}

              <button
                type="button"
                className="btn btn-solid"
                onClick={handlePlaceOrder}
                disabled={!paymentInfo?.success || busy}
              >
                Place Order
              </button>

              {orderInfo && (
                <div className="message success">
                  <p>{orderInfo.message}</p>
                  <button
                    type="button"
                    className="btn btn-outline"
                    onClick={() => navigate(`/orders/${orderInfo.orderId}`)}
                  >
                    View Order
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </section>
  );
}
