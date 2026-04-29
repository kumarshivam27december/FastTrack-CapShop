import { useRef } from 'react';
import html2pdf from 'html2pdf.js';

export default function InvoiceTemplate({ order, address }) {
  const invoiceRef = useRef(null);

  const handleDownloadPDF = () => {
    const element = invoiceRef.current;
    const opt = {
      margin: 10,
      filename: `Invoice-${order.orderNumber}.pdf`,
      image: { type: 'jpeg', quality: 0.98 },
      html2canvas: { scale: 2 },
      jsPDF: { orientation: 'portrait', unit: 'mm', format: 'a4' }
    };
    html2pdf().set(opt).from(element).save();
  };

  const subtotal = order.items?.reduce((sum, item) => sum + parseFloat(item.totalPrice || 0), 0) || 0;
  const tax = subtotal * 0.05; // 5% GST
  const total = subtotal + tax;

  return (
    <div className="invoice-container">
      <div ref={invoiceRef} className="invoice-template">
        {/* Header */}
        <div className="invoice-header">
          <div className="invoice-logo">
            <h1>CapShop</h1>
            <p className="tagline">Premium Online Store</p>
          </div>
          <div className="invoice-title">
            <h2>INVOICE</h2>
            <p className="invoice-number">#{order.orderNumber}</p>
          </div>
        </div>

        {/* Info Section */}
        <div className="invoice-info-section">
          <div className="invoice-info-row">
            <div className="invoice-info-block">
              <h3>Bill To</h3>
              <p className="info-name">{address?.fullName}</p>
              <p className="info-text">{address?.street}</p>
              <p className="info-text">{address?.city}, {address?.state} {address?.pincode}</p>
              <p className="info-text">Phone: {address?.phone}</p>
            </div>

            <div className="invoice-info-block">
              <h3>Order Details</h3>
              <p className="info-label">Order Date: <span className="info-text">{new Date(order.createdAtUtc).toLocaleDateString()}</span></p>
              <p className="info-label">Order Status: <span className="status-badge">{order.status}</span></p>
              <p className="info-label">Payment Method: <span className="info-text">Razorpay</span></p>
            </div>
          </div>
        </div>

        {/* Items Table */}
        <div className="invoice-items-section">
          <table className="invoice-table">
            <thead>
              <tr>
                <th className="col-product">Product</th>
                <th className="col-qty">Quantity</th>
                <th className="col-price">Unit Price</th>
                <th className="col-total">Total</th>
              </tr>
            </thead>
            <tbody>
              {order.items?.map((item) => (
                <tr key={`${item.productId}-${item.productName}`}>
                  <td className="col-product">
                    <p className="product-name">{item.productName}</p>
                  </td>
                  <td className="col-qty">{item.quantity}</td>
                  <td className="col-price">Rs. {Number(item.unitPrice).toFixed(2)}</td>
                  <td className="col-total">Rs. {Number(item.totalPrice).toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Totals Section */}
        <div className="invoice-totals-section">
          <div className="totals-container">
            <div className="totals-row">
              <span className="totals-label">Subtotal:</span>
              <span className="totals-value">Rs. {subtotal.toFixed(2)}</span>
            </div>
            <div className="totals-row">
              <span className="totals-label">Tax (5% GST):</span>
              <span className="totals-value">Rs. {tax.toFixed(2)}</span>
            </div>
            <div className="totals-row total-row">
              <span className="totals-label">Total Amount:</span>
              <span className="totals-value total-value">Rs. {total.toFixed(2)}</span>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="invoice-footer">
          <p>Thank you for your purchase! Track your order at any time on your account.</p>
          <p className="footer-contact">For support: support@capshop.com | Phone: +91-XXXX-XXXX-XX</p>
        </div>
      </div>

      {/* Download Button */}
      <div className="invoice-actions">
        <button type="button" className="btn btn-solid" onClick={handleDownloadPDF}>
          📥 Download Invoice PDF
        </button>
      </div>
    </div>
  );
}
