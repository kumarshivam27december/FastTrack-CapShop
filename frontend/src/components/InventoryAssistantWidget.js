import { useState } from 'react';
import { Link } from 'react-router-dom';
import { assistantApi } from '../api/assistantApi';

export default function InventoryAssistantWidget() {
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [messages, setMessages] = useState([
    {
      id: 1,
      role: 'assistant',
      text: 'Ask me for products in natural language, for example: coding book under 2000 in stock.'
    }
  ]);

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
      const response = await assistantApi.queryCatalog(message);
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 1,
          role: 'assistant',
          text: response?.reply || 'No response from assistant.',
          products: Array.isArray(response?.products) ? response.products : []
        }
      ]);
    } catch (err) {
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 1,
          role: 'assistant',
          text: err.message || 'Assistant request failed.'
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