const STATUS_CLASS_MAP = {
  Draft: 'status-muted',
  CheckoutStarted: 'status-checkout',
  PaymentPending: 'status-payment',
  Paid: 'status-paid',
  Packed: 'status-packed',
  Shipped: 'status-shipped',
  Delivered: 'status-delivered',
  Cancelled: 'status-cancelled'
};

export default function StatusBadge({ status }) {
  return <span className={`status-badge ${STATUS_CLASS_MAP[status] || 'status-muted'}`}>{status}</span>;
}
