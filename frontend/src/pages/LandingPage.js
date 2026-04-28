import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
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
  const { user, isAdmin } = useAuth();
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

          <div className="lp-badge">
            <span className="lp-badge-dot" />
            Microservices · API Gateway · React
          </div>

          <h1 className="lp-hero-title">
            Your complete<br />
            <span className="lp-grad">shopping platform.</span>
          </h1>

          <p className="lp-hero-sub">
            CapShop brings together a smart product catalog, AI-powered search,
            secure checkout with Razorpay, and a full admin panel — all in one place.
          </p>

          <div className="lp-hero-btns">
            <Link to="/catalog" className="lp-btn lp-btn-primary">
              Browse Catalog
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
                stroke="currentColor" strokeWidth="2.5">
                <path d="M5 12h14M12 5l7 7-7 7" />
              </svg>
            </Link>

            {user ? (
              isAdmin
                ? <Link to="/admin" className="lp-btn lp-btn-glass">Admin Panel →</Link>
                : <Link to="/orders" className="lp-btn lp-btn-glass">My Orders →</Link>
            ) : (
              <>
                <Link to="/signup" className="lp-btn lp-btn-glass">Create Account</Link>
                <Link to="/login" className="lp-btn lp-btn-ghost">Sign In</Link>
              </>
            )}
          </div>
        </div>

        {/* Hero right — feature tags */}
        <div className="lp-hero-tags" aria-hidden="true">
          <div className="lp-hero-tags-inner">
            {[
              { icon: '🔍', label: 'Smart Catalog Search' },
              { icon: '🤖', label: 'AI Voice Assistant' },
              { icon: '🛒', label: 'Cart & Checkout' },
              { icon: '💳', label: 'Razorpay Payments' },
              { icon: '📦', label: 'Order Tracking' },
              { icon: '🔐', label: 'Two-Factor Auth' },
              { icon: '📊', label: 'Admin Reports' },
              { icon: '📁', label: 'CSV Export' },
            ].map((tag, i) => (
              <div
                key={i}
                className="lp-tag-pill"
                style={{ animationDelay: `${i * 80}ms` }}
              >
                <span>{tag.icon}</span>
                {tag.label}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ════════ FEATURES ════════ */}
      <section className="lp-section">
        <div className="lp-section-eyebrow">What's inside</div>
        <h2 className="lp-section-title">
          Every feature you need,<br />
          <span className="lp-grad">built and working.</span>
        </h2>

        <div className="lp-features-grid">
          <FeatureCard
            icon="🔍"
            title="Smart Product Catalog"
            desc="Search by name, filter by category, set price range (min–max), and sort by price or name — with paginated results."
            color="linear-gradient(135deg,#6366f1,#818cf8)"
            delay="0ms"
          />
          <FeatureCard
            icon="🤖"
            title="AI Catalog Assistant"
            desc="Ask for products in plain English — by budget, stock, or type. Also supports live voice input via Speech Recognition."
            color="linear-gradient(135deg,#0ea5e9,#38bdf8)"
            delay="70ms"
          />
          <FeatureCard
            icon="💳"
            title="Razorpay Checkout"
            desc="Full shipping address form, UPI or Card payment via Razorpay, signature verification, and automatic order confirmation."
            color="linear-gradient(135deg,#10b981,#34d399)"
            delay="140ms"
          />
          <FeatureCard
            icon="📦"
            title="Order Management"
            desc="View your full order history with status badges, cancel pending orders, and download all orders as a CSV file."
            color="linear-gradient(135deg,#f59e0b,#fbbf24)"
            delay="210ms"
          />
          <FeatureCard
            icon="🔐"
            title="Two-Factor Authentication"
            desc="Choose between SMS OTP, Email OTP, or Microsoft Authenticator app with QR code setup — all configurable from your profile."
            color="linear-gradient(135deg,#8b5cf6,#a78bfa)"
            delay="280ms"
          />
          <FeatureCard
            icon="🧑‍💼"
            title="Google OAuth Login"
            desc="Sign in with Google in one click. Google users can also set an email/password later for dual-method access."
            color="linear-gradient(135deg,#ec4899,#f472b6)"
            delay="350ms"
          />
          <FeatureCard
            icon="📊"
            title="Admin Reports & CSV"
            desc="Admin gets order status split, date-range sales report with per-day revenue, and one-click CSV export."
            color="linear-gradient(135deg,#f97316,#fb923c)"
            delay="420ms"
          />
          <FeatureCard
            icon="🧑‍⚙️"
            title="Full Admin Panel"
            desc="Manage products, categories, and all orders. Overview shows total orders, today's orders, total revenue, and product count."
            color="linear-gradient(135deg,#06b6d4,#22d3ee)"
            delay="490ms"
          />
          <FeatureCard
            icon="👤"
            title="User Profile"
            desc="Update your name, phone, and avatar photo. Change or set your password, and manage your 2FA settings — all in one screen."
            color="linear-gradient(135deg,#d946ef,#e879f9)"
            delay="560ms"
          />
        </div>
      </section>

      {/* ════════ HOW IT WORKS ════════ */}
      <section className="lp-section">
        <div className="lp-section-eyebrow">How it works</div>
        <h2 className="lp-section-title">
          From signup to order —<br />
          <span className="lp-grad">three steps.</span>
        </h2>

        <div className="lp-steps-grid">
          <StepCard
            num="01" icon="🔑" delay="0ms"
            title="Create your account"
            desc="Sign up with email or continue with Google. Enable Two-Factor Authentication (SMS, Email, or Authenticator App) for extra security."
          />
          <StepCard
            num="02" icon="🛍️" delay="120ms"
            title="Browse & add to cart"
            desc="Search the catalog, filter by category and price, or use the AI Assistant with voice input to find what you need. Add items to cart."
          />
          <StepCard
            num="03" icon="✅" delay="240ms"
            title="Checkout & track"
            desc="Enter your shipping address, pay securely via Razorpay (UPI or Card), and track every order from your orders page."
          />
        </div>
      </section>

      {/* ════════ ADMIN HIGHLIGHT ════════ */}
      <section className="lp-section">
        <div className="lp-admin-card">
          <div className="lp-admin-blob" />
          <div className="lp-admin-inner">
            <div className="lp-admin-left">
              <div className="lp-section-eyebrow lp-eyebrow-white">For Admins</div>
              <h2 className="lp-admin-title">
                A complete back-office<br />
                <span className="lp-grad-gold">built right in.</span>
              </h2>
              <p className="lp-admin-sub">
                The admin panel gives you everything to run the store: manage products and categories,
                view and update all orders, see today's metrics, and pull date-range sales reports
                with CSV export — no third-party tools needed.
              </p>
              {isAdmin && (
                <Link to="/admin" className="lp-btn lp-btn-white">
                  Open Admin Panel →
                </Link>
              )}
            </div>
            <div className="lp-admin-metrics">
              {[
                { label: 'Total Orders', icon: '📋' },
                { label: "Today's Orders", icon: '🗓️' },
                { label: 'Total Revenue', icon: '💰' },
                { label: 'Total Products', icon: '📦' },
              ].map((m, i) => (
                <div key={i} className="lp-admin-metric-card">
                  <span className="lp-admin-metric-icon">{m.icon}</span>
                  <span className="lp-admin-metric-label">{m.label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* ════════ CTA ════════ */}
      <section className="lp-section lp-cta-section">
        <div className="lp-cta-card">
          <div className="lp-cta-glow" />
          <h2 className="lp-cta-title">
            Ready to start shopping?
          </h2>
          <p className="lp-cta-sub">
            Browse the catalog right now — no account required.
            Sign up to unlock cart, checkout, and order tracking.
          </p>
          <div className="lp-cta-btns">
            <Link to="/catalog" className="lp-btn lp-btn-white">
              🛍️ Browse Catalog
            </Link>
            {!user && (
              <Link to="/signup" className="lp-btn lp-btn-glass-dark">
                Create Free Account
              </Link>
            )}
            {user && isAdmin && (
              <Link to="/admin" className="lp-btn lp-btn-glass-dark">
                Open Admin Panel
              </Link>
            )}
          </div>
        </div>
      </section>

    </div>
  );
}
