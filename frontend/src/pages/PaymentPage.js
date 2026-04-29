import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { orderApi } from '../api/orderApi';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import ProgressIndicator from '../components/ProgressIndicator';

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

export default function PaymentPage() {
  const navigate = useNavigate();
  const { orderId } = useParams();
  const { token } = useAuth();
  const { refreshCart } = useCart();

  const [paymentMethod, setPaymentMethod] = useState('UPI');
  const [checkoutInfo, setCheckoutInfo] = useState(null);
  const [paymentInfo, setPaymentInfo] = useState(null);
  const [orderInfo, setOrderInfo] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [paymentRequested, setPaymentRequested] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchCheckoutInfo() {
      setLoading(true);
      setError('');
      try {
        if (!orderId) {
          throw new Error('No order found. Please complete checkout address first.');
        }
        // Fetch the checkout info from the backend or use passed data
        // For now, we'll assume the order is already created and we're just processing payment
        setCheckoutInfo({ orderId, loading: false });
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    if (orderId && token) {
      fetchCheckoutInfo();
    }
  }, [orderId, token]);

  async function placeOrderWithRetry(orderIdParam, maxAttempts = 90, delayMs = 1000) {
    let lastError = null;

    for (let i = 0; i < maxAttempts; i += 1) {
      try {
        // eslint-disable-next-line no-await-in-loop
        return await orderApi.placeOrder(token, orderIdParam);
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
          theme: {
            color: '#CCFF00'
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

  // Redirect to confirmation page when order is placed
  if (orderInfo) {
    navigate(`/orders/${orderInfo.orderId}/confirmation`);
    return null;
  }

  if (loading) {
    return (
      <section className="section card">
        <p>Loading payment details...</p>
      </section>
    );
  }

  if (!orderId) {
    return (
      <section className="section card">
        <h1>Invalid Order</h1>
        <p className="message error">Order not found. Please start checkout again.</p>
        <Link to="/checkout" className="btn btn-solid">Back to Checkout</Link>
      </section>
    );
  }

  const currentStep = orderInfo ? 2 : paymentRequested ? 1 : 1;

  return (
    <section className="section">
      <div className="checkout-header">
        <h1>Complete Payment</h1>
        <p>Secure payment for your order</p>
      </div>

      <ProgressIndicator currentStep={currentStep} />

      {error && <div className="message error"><strong>Error:</strong> {error}</div>}

      <div className="payment-page-container">
        <div className="payment-card card">
          <div className="step-header">
            <div className="step-number">2</div>
            <h2>Payment & Confirmation</h2>
          </div>

          <div className="payment-method-group">
            <label className="payment-method-label">
              <span>Select Payment Method</span>
              <select value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)} className="payment-select">
                <option value="UPI">UPI</option>
                <option value="Card">Card</option>
              </select>
            </label>
          </div>

          <button
            type="button"
            className="btn btn-solid btn-large"
            onClick={handleRazorpayPayment}
            disabled={busy}
          >
            {busy ? '⏳ Processing...' : '💳 Pay Now'}
          </button>

          {paymentInfo && (
            <div className={`payment-result ${paymentInfo.success ? 'success' : 'error'}`}>
              <div className="result-icon">
                {paymentInfo.success ? '✓' : '⚠'}
              </div>
              <p className="result-message">{paymentInfo.message}</p>
              <p className="transaction-id">Txn ID: {paymentInfo.transactionId}</p>
            </div>
          )}

          {paymentRequested && !orderInfo && (
            <div>
              <div className="divider" />
              <p className="note-text">Payment verified. Finalizing your order...</p>
              <button
                type="button"
                className="btn btn-outline btn-large"
                onClick={handlePlaceOrder}
                disabled={busy}
              >
                {busy ? '⏳ Placing Order...' : 'Complete Order'}
              </button>
            </div>
          )}

          <div className="payment-actions">
            <Link to="/checkout" className="btn btn-outline">← Back to Address</Link>
          </div>
        </div>
      </div>
    </section>
  );
}
