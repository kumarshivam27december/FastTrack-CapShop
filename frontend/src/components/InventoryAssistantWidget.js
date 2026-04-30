import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { assistantApi } from '../api/assistantApi';
import { useAuth } from '../context/AuthContext';

export default function InventoryAssistantWidget() {
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [speakingMessageId, setSpeakingMessageId] = useState(null);
  const { token } = useAuth();
  const canSpeakOutput = typeof window !== 'undefined' && 'speechSynthesis' in window;
  const [messages, setMessages] = useState([
    {
      id: 1,
      role: 'assistant',
      text: 'Ask me for products in natural language, for example: coding book under 2000 in stock.'
    }
  ]);

  useEffect(() => {
    return () => {
      if (typeof window !== 'undefined' && window.speechSynthesis) {
        window.speechSynthesis.cancel();
      }
    };
  }, []);

  function toggleSpeak(messageId, text) {
    if (!canSpeakOutput || !text) {
      return;
    }

    if (speakingMessageId === messageId) {
      window.speechSynthesis.cancel();
      setSpeakingMessageId(null);
      return;
    }

    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'en-US';
    utterance.onend = () => setSpeakingMessageId((current) => (current === messageId ? null : current));
    utterance.onerror = () => setSpeakingMessageId((current) => (current === messageId ? null : current));

    setSpeakingMessageId(messageId);
    window.speechSynthesis.speak(utterance);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    const message = input.trim();
    if (!message || loading) {
      return;
    }

    const userMessage = { id: Date.now(), role: 'user', text: message };
    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setLoading(true);

    try {
      const response = await assistantApi.queryCatalog(token, message);
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 1,
          role: 'assistant',
          text: response?.reply || 'No response from assistant.',
          products: Array.isArray(response?.products) ? response.products : [],
          orders: Array.isArray(response?.orders) ? response.orders : []
        }
      ]);
    } catch (err) {
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 1,
          role: 'assistant',
          text: err.message || 'Assistant request failed.',
          products: [],
          orders: []
        }
      ]);
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="card assistant-widget" aria-label="Catalog Assistant">
      <div className="assistant-head">
        <h2>Catalog Assistant</h2>
        <p className="hint">Offline-ready AI helper for inventory search.</p>
      </div>

      <div className="assistant-thread" role="log" aria-live="polite">
        {messages.map((message) => (
          <article
            key={message.id}
            className={`assistant-message assistant-message-${message.role}`}
          >
            <p>{message.text}</p>
            {message.role === 'assistant' && canSpeakOutput && (
              <button
                type="button"
                className="btn btn-outline assistant-speak-btn"
                onClick={() => toggleSpeak(message.id, message.text)}
              >
                {speakingMessageId === message.id ? 'Stop' : 'Speak'}
              </button>
            )}
            {Array.isArray(message.orders) && message.orders.length > 0 && (
              <ul className="assistant-products-list">
                {message.orders.map((order) => (
                  <li key={order.id}>
                    <Link to={`/orders/${order.id}`}>Order #{order.orderNumber}</Link>
                    <span>{new Date(order.createdAtUtc).toLocaleDateString()} | Rs. {Number(order.totalAmount).toFixed(2)} | {order.status}</span>
                  </li>
                ))}
              </ul>
            )}
            {Array.isArray(message.products) && message.products.length > 0 && (
              <ul className="assistant-products-list">
                {message.products.map((product) => (
                  <li key={product.id}>
                    <Link to={`/products/${product.id}`}>{product.name}</Link>
                    <span>Rs. {Number(product.price).toFixed(2)} | Stock: {product.stock}</span>
                  </li>
                ))}
              </ul>
            )}
          </article>
        ))}
      </div>

      <form className="assistant-form" onSubmit={handleSubmit}>
        <input
          value={input}
          onChange={(event) => setInput(event.target.value)}
          placeholder="Try: coding book under 2000"
          aria-label="Ask catalog assistant"
        />
        <button type="submit" className="btn btn-solid" disabled={loading || !input.trim()}>
          {loading ? 'Searching...' : 'Ask'}
        </button>
      </form>
    </section>
  );
}