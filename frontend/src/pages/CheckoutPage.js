import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';

const RAZORPAY_SCRIPT = 'https://checkout.razorpay.com/v1/checkout.js';

async function loadRazorpayScript() {
  if (window.Razorpay) {
    return true;
  }

  return new Promise((resolve) => {
    const script = document.createElement('script');
    script.src = RAZORPAY_SCRIPT;
    script.async = true;
    script.onload = () => resolve(true);
    script.onerror = () => resolve(false);
    document.body.appendChild(script);
  });
}

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
  const [checkoutInfo, setCheckoutInfo] = useState(null);
  const [paymentInfo, setPaymentInfo] = useState(null);
  const [orderInfo, setOrderInfo] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [paymentRequested, setPaymentRequested] = useState(false);

  async function placeOrderWithRetry(orderId, maxAttempts = 90, delayMs = 1000) {
    let lastError = null;

    for (let i = 0; i < maxAttempts; i += 1) {
      try {
        // eslint-disable-next-line no-await-in-loop
        return await orderApi.placeOrder(token, orderId);
      } catch (err) {
        lastError = err;
        const msg = String(err?.message || '').toLowerCase();
        if (!msg.includes('must be paid before placing') && !msg.includes('still being finalized')) {
          throw err;
        }

        // eslint-disable-next-line no-await-in-loop
        await new Promise((resolve) => setTimeout(resolve, delayMs));
      }
    }

    if (lastError) {
      throw lastError;
    }

    throw new Error('Payment is still processing. Please wait a few seconds and try again.');
  }

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

  async function handleRazorpayPayment() {
    if (!checkoutInfo) return;

    setBusy(true);
    setError('');

    try {
      const loaded = await loadRazorpayScript();
      if (!loaded) {
        throw new Error('Unable to load Razorpay checkout script.');
      }

      const intent = await orderApi.createPaymentIntent(token, {
        orderId: checkoutInfo.orderId,
        paymentMethod,
        currency: 'INR'
      });

      const verifiedPayment = await new Promise((resolve, reject) => {
        const razorpay = new window.Razorpay({
          key: intent.keyId,
          amount: intent.amount,
          currency: intent.currency,
          name: 'CapShop',
          description: `Order ${checkoutInfo.orderNumber}`,
          order_id: intent.razorpayOrderId,
          prefill: {
            name: address.fullName,
            contact: address.phone
          },
          theme: {
            color: '#2f6f58'
          },
          handler: async (paymentResult) => {
            try {
              const verifyResponse = await orderApi.verifyPayment(token, {
                orderId: checkoutInfo.orderId,
                razorpayOrderId: paymentResult.razorpay_order_id,
                razorpayPaymentId: paymentResult.razorpay_payment_id,
                razorpaySignature: paymentResult.razorpay_signature
              });

              resolve(verifyResponse);
            } catch (verifyError) {
              reject(verifyError);
            }
          },
          modal: {
            ondismiss: () => reject(new Error('Payment popup closed before completion.'))
          }
        });

        razorpay.open();
      });

      setPaymentRequested(true);

      setPaymentInfo({
        transactionId: verifiedPayment.transactionId,
        success: verifiedPayment.verified,
        message: verifiedPayment.message
      });

      try {
        const placed = await placeOrderWithRetry(checkoutInfo.orderId);
        setPaymentInfo({
          transactionId: verifiedPayment.transactionId,
          success: true,
          message: 'Payment successful'
        });
        setOrderInfo(placed);
        await refreshCart();
      } catch (err) {
        const msg = String(err?.message || '').toLowerCase();
        const isFinalizing = msg.includes('must be paid before placing') || msg.includes('still being finalized');

        if (!isFinalizing) {
          throw err;
        }

        setPaymentInfo({
          transactionId: verifiedPayment.transactionId,
          success: true,
          message: 'Payment is still processing. Please use Place Order. No need to pay again.'
        });
      }
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
      const response = await placeOrderWithRetry(checkoutInfo.orderId);
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
                </select>
              </label>

              <button type="button" className="btn btn-outline" onClick={handleRazorpayPayment} disabled={busy}>
                Pay with Razorpay (Test)
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
                disabled={!paymentRequested || busy || !!orderInfo}
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
