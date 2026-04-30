import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import './LandingPage.css';

/* ─── Small reusable components ─── */

function FeatureCard({ icon, title, desc, color, delay }) {
  return (
    <div className="lp-feature-card" style={{ '--fc-color': color, animationDelay: delay }}>
      <div className="lp-feature-icon-wrap">
        <span>{icon}</span>
      </div>
      <h3>{title}</h3>
      <p>{desc}</p>
    </div>
  );
}

function StepCard({ num, icon, title, desc, delay }) {
  return (
    <div className="lp-step-card" style={{ animationDelay: delay }}>
      <div className="lp-step-num">{num}</div>
      <div className="lp-step-icon">{icon}</div>
      <h3>{title}</h3>
      <p>{desc}</p>
    </div>
  );
}

/* ─── Main ─── */
export default function LandingPage() {
  const { user } = useAuth();
  const { isDarkMode } = useTheme();
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => setVisible(true), 60);
    return () => clearTimeout(t);
  }, []);

  return (
    <div className="lp-root">

      {/* ── Animated background ── */}
      <div className="lp-bg-canvas" aria-hidden="true">
        <div className="lp-blob lp-blob-1" />
        <div className="lp-blob lp-blob-2" />
        <div className="lp-blob lp-blob-3" />
        <div className="lp-blob lp-blob-4" />
        <div className="lp-grid" />
      </div>

      {/* ════════ HERO ════════ */}
      <section className={`lp-hero${visible ? ' lp-hero--visible' : ''}`}>
        <div className="lp-hero-content">

          {/* <div className="lp-hero-badge fade-in">
            <span className="lp-badge-dot" />
            Ultimate E-Commerce Hub
          </div> */}

          <h1 className="lp-hero-title">
            One Point
            <br />
            <span className="lp-grad">Purchase Solution.</span>
          </h1>

          <p className="lp-hero-sub">
            We are selling all consumer goods in the e-commerce web hub.
            Experience a seamless, secure, and lightning-fast shopping journey
            for everything you need.
          </p>

          <div className="lp-hero-stats-mini">
            <div className="lp-mini-stat">
              <strong>100%</strong>
              <span>Secure</span>
            </div>
            <div className="lp-mini-divider" />
            <div className="lp-mini-stat">
              <strong>24/7</strong>
              <span>Support</span>
            </div>
            <div className="lp-mini-divider" />
            <div className="lp-mini-stat">
              <strong>AI</strong>
              <span>Assistant</span>
            </div>
          </div>

          <div className="lp-hero-btns">
            <Link to="/catalog" className="lp-btn lp-btn-primary">
              Browse Catalog
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
                stroke="currentColor" strokeWidth="2.5">
                <path d="M5 12h14M12 5l7 7-7 7" />
              </svg>
            </Link>

            {user ? (
              <Link to="/orders" className="lp-btn lp-btn-glass">My Orders →</Link>
            ) : (
              <>
                <Link to="/signup" className="lp-btn lp-btn-glass">Create Account</Link>
                <Link to="/login" className="lp-btn lp-btn-ghost">Sign In</Link>
              </>
            )}
          </div>
        </div>

        {/* Hero right — premium visual */}
        <div className="lp-hero-visual">
          <div className="lp-hero-img-wrap">
            <img 
              src={isDarkMode ? "/assets/hero.png" : "/assets/hero-light.png"} 
              alt="E-commerce Hub Visual" 
              className="lp-hero-img"
            />
            <div className="lp-hero-glow" />
          </div>
          
          {/* <div className="lp-floating-card lp-fcard-1">
            <span className="lp-fcard-icon">🚀</span>
            <div className="lp-fcard-txt">
              <strong>99.9%</strong>
              <span>Uptime Hub</span>
            </div>
          </div> */}
        </div>
      </section>

      {/* ════════ MARQUEE ════════ */}
      <div className="lp-marquee-wrap">
        <div className="lp-marquee-content">
          {[
            'NEW ARRIVALS', 'SECURE PAYMENTS', 'SHIPPING','Order Tracking ','AI Assitance', 'TOP BRANDS', 'BEST DEALS',
            'NEW ARRIVALS', 'SECURE PAYMENTS', 'SHIPPING', 'Order Tracking','AI Assitance','TOP BRANDS', 'BEST DEALS'
          ].map((text, i) => (
            <span key={i} className="lp-marquee-item">{text}</span>
          ))}
        </div>
      </div>

      {/* ════════ WHY SHOP WITH US ════════ */}
      <section className="lp-section">
        <div className="lp-section-eyebrow">Why shop with us</div>
        <h2 className="lp-section-title">Everything you need for a<br /><span className="lp-grad">perfect shopping experience.</span></h2>

        <div className="lp-features-grid lp-why-grid">
          <FeatureCard
            icon="🛍️"
            title="Curated Collections"
            desc="Explore our hand-picked selection of premium products. Filter easily to find exactly what matches your style and needs."
            color="linear-gradient(135deg,#6366f1,#818cf8)"
            delay="0ms"
          />
          <FeatureCard
            icon="🤖"
            title="AI Shopping Assistant"
            desc="Speak naturally to our voice assistant to get personalized product recommendations instantly."
            color="linear-gradient(135deg,#0ea5e9,#38bdf8)"
            delay="70ms"
          />
          <FeatureCard
            icon="🔒"
            title="Secure Checkout"
            desc="Experience lightning-fast and secure payments. We support major cards, debit cards, and UPI."
            color="linear-gradient(135deg,#10b981,#34d399)"
            delay="140ms"
          />
          <FeatureCard
            icon="📦"
            title="Real-Time Tracking"
            desc="Stay updated on your purchases from the moment they ship to when they arrive at your door."
            color="linear-gradient(135deg,#f59e0b,#fbbf24)"
            delay="210ms"
          />
          <FeatureCard
            icon="🛡️"
            title="Bank-Grade Security"
            desc="Your data is protected with industry-leading encryption and optional two-factor authentication."
            color="linear-gradient(135deg,#8b5cf6,#a78bfa)"
            delay="280ms"
          />
          <FeatureCard
            icon="⚡"
            title="Seamless Access"
            desc="Sign up in seconds with Google and dive straight into shopping without another password."
            color="linear-gradient(135deg,#ec4899,#f472b6)"
            delay="350ms"
          />
          <FeatureCard
            icon="🎁"
            title="Exclusive Rewards"
            desc="Create an account to save favorites, manage addresses, and unlock member-only discounts."
            color="linear-gradient(135deg,#f97316,#fb923c)"
            delay="420ms"
          />
          <FeatureCard
            icon="💬"
            title="Priority Support"
            desc="Our customer care team is ready to help with order inquiries, returns, or product questions."
            color="linear-gradient(135deg,#06b6d4,#22d3ee)"
            delay="490ms"
          />
        </div>
      </section>

      {/* ════════ THE PROCESS ════════ */}
      <section className="lp-section">
        <div className="lp-section-eyebrow">The process</div>
        <h2 className="lp-section-title">From screen to doorstep in<br /><span className="lp-grad">three simple steps.</span></h2>

        <div className="lp-steps-grid">
          <StepCard
            num="01" icon="🔍" delay="0ms"
            title="Discover & Choose"
            desc="Browse our catalog or use smart search to find products you love. Add favorites to your cart."
          />
          <StepCard
            num="02" icon="💳" delay="120ms"
            title="Secure Checkout"
            desc="Enter shipping details and complete payment through our encrypted checkout process."
          />
          <StepCard
            num="03" icon="🚚" delay="240ms"
            title="Fast Delivery"
            desc="Track your order in real-time as it moves from our warehouse to your doorstep."
          />
        </div>
      </section>

      {/* ════════ CONSUMER CATEGORIES ════════ */}
      <section className="lp-section">
        <div className="lp-section-eyebrow">Explore our hub</div>
        <h2 className="lp-section-title">Everything you need, in<br /><span className="lp-grad">one digital destination.</span></h2>
        
        <div className="lp-cat-grid">
          {[
            { name: 'Electronics', icon: '💻', count: '12k+ items' },
            { name: 'Fashion', icon: '👕', count: '8k+ items' },
            { name: 'Home & Living', icon: '🏠', count: '5k+ items' },
            { name: 'Beauty', icon: '💄', count: '3k+ items' },
            { name: 'Sports', icon: '🏀', count: '2k+ items' },
            { name: 'Groceries', icon: '🍎', count: '10k+ items' },
          ].map((cat, i) => (
            <div key={i} className="lp-cat-card" style={{ animationDelay: `${i * 60}ms` }}>
              <div className="lp-cat-icon">{cat.icon}</div>
              <div className="lp-cat-info">
                <h4>{cat.name}</h4>
                <span>{cat.count}</span>
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className="lp-section lp-cta-section">
        <div className="lp-cta-card">
          <div className="lp-cta-glow" />
          <h2 className="lp-cta-title">
            Ready to upgrade your lifestyle?
          </h2>
          <p className="lp-cta-sub">
            Join thousands of happy customers. Browse latest arrivals and find
            exactly what you've been looking for.
          </p>
          <div className="lp-cta-btns">
            <Link to="/catalog" className="lp-btn lp-btn-white">
              🛍️ Shop Now
            </Link>
            {!user && (
              <Link to="/signup" className="lp-btn lp-btn-glass-dark">
                Create Free Account
              </Link>
            )}
          </div>
        </div>
      </section>

      {/* ════════ TRAITS / QUICK FACTS ════════ */}
      <section className="lp-section lp-traits" aria-hidden="true">
        <div className="lp-traits-grid">
          {[
            { icon: '🚚', label: 'Fast Delivery' },
            { icon: '🤖', label: 'AI Shopping Assistant' },
            { icon: '🛡️', label: 'Secure Payments' },
            { icon: '⭐', label: 'Top Rated Products' },
            { icon: '📦', label: 'Easy Returns' },
            { icon: '🔒', label: 'Privacy Protected' },
            { icon: '🎁', label: 'Exclusive Deals' },
            { icon: '🎧', label: '24/7 Support' },
          ].map((t, i) => (
            <div key={i} className="lp-trait">
              <div className="lp-trait-icon">{t.icon}</div>
              <div className="lp-trait-label">{t.label}</div>
            </div>
          ))}
        </div>
      </section>

    </div>
  );
}
