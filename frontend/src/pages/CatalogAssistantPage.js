import { useEffect, useMemo, useRef, useState } from 'react';
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
  const [isListening, setIsListening] = useState(false);
  const [speakingMessageId, setSpeakingMessageId] = useState(null);
  const recognitionRef = useRef(null);
  const [messages, setMessages] = useState([
    {
      id: 1,
      role: 'assistant',
      text: 'Hi, I can search your catalog using natural language. Ask for budget, availability, or product type.',
      products: [],
      orders: []
    }
  ]);

  const canSend = useMemo(() => input.trim().length > 0 && !loading, [input, loading]);
  const canSpeakOutput = typeof window !== 'undefined' && 'speechSynthesis' in window;

  useEffect(() => {
    return () => {
      if (typeof window !== 'undefined' && window.speechSynthesis) {
        window.speechSynthesis.cancel();
      }
    };
  }, []);

  function getFullSpeechText(message) {
    let fullText = message.text || '';
    
    if (Array.isArray(message.products) && message.products.length > 0) {
      fullText += '. Found ' + message.products.length + ' products. ';
      message.products.forEach((p, idx) => {
        fullText += `Product ${idx + 1}: ${p.name}, priced at ${p.price} rupees. `;
      });
    }
    
    if (Array.isArray(message.orders) && message.orders.length > 0) {
      fullText += '. Found ' + message.orders.length + ' orders. ';
      message.orders.forEach((o, idx) => {
        fullText += `Order ${idx + 1}: Number ${o.orderNumber}, status ${o.status}. `;
      });
    }
    
    return fullText;
  }

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

  function initializeRecognition() {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
      alert('Speech Recognition not supported in your browser. Please use Chrome, Edge, or Firefox.');
      return;
    }

    const recognition = new SpeechRecognition();
    recognition.continuous = false;
    recognition.interimResults = true;
    recognition.lang = 'en-US';

    recognition.onstart = () => {
      setIsListening(true);
      setInput('');
    };

    recognition.onresult = (event) => {
      let transcript = '';
      for (let i = event.resultIndex; i < event.results.length; i++) {
        transcript += event.results[i][0].transcript;
      }
      setInput(transcript);
    };

    recognition.onerror = (event) => {
      console.error('Speech recognition error:', event.error);
      alert(`Error: ${event.error}`);
    };

    recognition.onend = () => {
      setIsListening(false);
    };

    recognitionRef.current = recognition;
  }

  function startListening() {
    if (!recognitionRef.current) {
      initializeRecognition();
    }
    recognitionRef.current?.start();
  }

  function stopListening() {
    recognitionRef.current?.stop();
  }

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
        products: [],
        orders: []
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
          products: Array.isArray(response?.products) ? response.products : [],
          orders: Array.isArray(response?.orders) ? response.orders : []
        }
      ]);
    } catch (error) {
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 1,
          role: 'assistant',
          text: error?.message || 'The assistant request failed. Please try again.',
          products: [],
          orders: []
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
            {/* <p className="hint">Answers come from your real catalog data.</p> */}
          </div>

          <div className="assistant-chat-thread" role="log" aria-live="polite">
            {messages.map((message) => (
              <article
                key={message.id}
                className={`assistant-chat-bubble assistant-chat-bubble-${message.role}`}
              >
                <p>{message.text}</p>
                {message.role === 'assistant' && canSpeakOutput && (
                  <button
                    type="button"
                    className="btn btn-outline assistant-speak-btn"
                    onClick={() => toggleSpeak(message.id, getFullSpeechText(message))}
                  >
                    {speakingMessageId === message.id ? 'Stop' : 'Listen'}
                  </button>
                )}

                {Array.isArray(message.orders) && message.orders.length > 0 && (
                  <div className="assistant-result-grid">
                    {message.orders.map((order) => (
                      <div key={order.id} className="assistant-result-card">
                        <h4>Order #{order.orderNumber}</h4>
                        <p className="hint">{new Date(order.createdAtUtc).toLocaleDateString()}</p>
                        <p className="hint">Rs. {Number(order.totalAmount).toFixed(2)} | Status: {order.status}</p>
                        <Link className="btn btn-outline" to={`/orders/${order.id}`}>View Order</Link>
                      </div>
                    ))}
                  </div>
                )}

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
            <button
              type="button"
              className={`btn ${isListening ? 'btn-solid' : 'btn-outline'}`}
              onClick={isListening ? stopListening : startListening}
              title={isListening ? 'Click to stop listening' : 'Click to speak'}
            >
              {isListening ? '🎙️ Listening...' : '🎤 Speak'}
            </button>
            <button type="submit" className="btn btn-solid" disabled={!canSend}>
              Send
            </button>
          </form>
        </div>
      </div>
    </section>
  );
}