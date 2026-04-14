import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { assistantApi } from '../api/assistantApi';
import { useAuth } from '../context/AuthContext';

const QUICK_PROMPTS = [
  'coding book under 2000 in stock',
  'show me the cheapest programming books',
  'give me options for beginners in coding',
  'books above 1500 with high stock'
];

export default function CatalogAssistantPage() {
  const { token } = useAuth();
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [messages, setMessages] = useState([
    {
      id: 1,
      role: 'assistant',
      text: 'Hi, I can search your catalog using natural language. Ask for budget, availability, or product type.',
      products: []
    }
  ]);

  const canSend = useMemo(() => input.trim().length > 0 && !loading, [input, loading]);

  async function sendPrompt(text) {
    const message = text.trim();
    if (!message || loading) {
      return;
    }

    setMessages((prev) => [
      ...prev,
      {
        id: Date.now(),
        role: 'user',
        text: message,
        products: []
      }
    ]);
    setInput('');
    setLoading(true);

    try {
      const response = await assistantApi.queryCatalog(token, message);
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 1,
          role: 'assistant',
          text: response?.reply || 'I could not generate a response this time.',
          products: Array.isArray(response?.products) ? response.products : []
        }
      ]);
    } catch (error) {
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 1,
          role: 'assistant',
          text: error?.message || 'The assistant request failed. Please try again.',
          products: []
        }
      ]);
    } finally {
      setLoading(false);
    }
  }

  function handleSubmit(event) {
    event.preventDefault();
    sendPrompt(input);
  }

  return (
    <section className="section assistant-page">
      <div className="section-head">
        <h1>Catalog AI Assistant</h1>
      </div>

      <div className="assistant-layout">
        <aside className="card assistant-side-panel">
          <h3>Quick Prompts</h3>
          <p className="hint">Use these to test your demo quickly.</p>
          <div className="assistant-prompt-list">
            {QUICK_PROMPTS.map((prompt) => (
              <button
                key={prompt}
                type="button"
                className="btn btn-outline assistant-prompt-btn"
                onClick={() => sendPrompt(prompt)}
                disabled={loading}
              >
                {prompt}
              </button>
            ))}
          </div>
        </aside>

        <div className="card assistant-chat-shell">
          <div className="assistant-chat-header">
            <h3>Chat</h3>
            <p className="hint">Answers come from your real catalog data.</p>
          </div>

          <div className="assistant-chat-thread" role="log" aria-live="polite">
            {messages.map((message) => (
              <article
                key={message.id}
                className={`assistant-chat-bubble assistant-chat-bubble-${message.role}`}
              >
                <p>{message.text}</p>

                {Array.isArray(message.products) && message.products.length > 0 && (
                  <div className="assistant-result-grid">
                    {message.products.map((product) => (
                      <div key={product.id} className="assistant-result-card">
                        <h4>{product.name}</h4>
                        <p className="hint">{product.categoryName || 'Unknown category'}</p>
                        <p className="hint">Rs. {Number(product.price).toFixed(2)} | Stock: {product.stock}</p>
                        <Link className="btn btn-outline" to={`/products/${product.id}`}>View Product</Link>
                      </div>
                    ))}
                  </div>
                )}
              </article>
            ))}

            {loading && (
              <article className="assistant-chat-bubble assistant-chat-bubble-assistant">
                <p>Searching products and preparing response...</p>
              </article>
            )}
          </div>

          <form className="assistant-chat-form" onSubmit={handleSubmit}>
            <input
              value={input}
              onChange={(event) => setInput(event.target.value)}
              placeholder="Ask for products, budget, and stock..."
              aria-label="Catalog assistant prompt"
            />
            <button type="submit" className="btn btn-solid" disabled={!canSend}>
              Send
            </button>
          </form>
        </div>
      </div>
    </section>
  );
}