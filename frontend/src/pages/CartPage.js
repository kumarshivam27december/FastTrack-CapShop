import { Link, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';

export default function CartPage() {
  const navigate = useNavigate();
  const { cart, loading, updateCartItem, removeCartItem } = useCart();

  async function changeQuantity(itemId, nextQuantity) {
    try {
      await updateCartItem(itemId, nextQuantity);
    } catch (err) {
      alert(err.message);
    }
  }

  async function handleRemove(itemId) {
    try {
      await removeCartItem(itemId);
    } catch (err) {
      alert(err.message);
    }
  }

  if (loading) {
    return <p className="message">Loading cart...</p>;
  }

  if (!cart.items?.length) {
    return (
      <section className="section card">
        <h1>Your cart is empty</h1>
        <p>Add products from the catalog to begin checkout.</p>
        <Link to="/catalog" className="btn btn-solid">Go to Catalog</Link>
      </section>
    );
  }

  return (
    <section className="section">
      <h1>My Cart</h1>

      <div className="table-wrap card">
        <table>
          <thead>
            <tr>
              <th>Product</th>
              <th>Unit Price</th>
              <th>Quantity</th>
              <th>Total</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            {cart.items.map((item) => (
              <tr key={item.id}>
                <td>{item.productName}</td>
                <td>Rs. {Number(item.unitPrice).toFixed(2)}</td>
                <td>
                  <div className="inline-actions">
                    <button
                      type="button"
                      className="btn btn-outline"
                      onClick={() => changeQuantity(item.id, Math.max(0, item.quantity - 1))}
                    >
                      -
                    </button>
                    <span>{item.quantity}</span>
                    <button
                      type="button"
                      className="btn btn-outline"
                      onClick={() => changeQuantity(item.id, item.quantity + 1)}
                    >
                      +
                    </button>
                  </div>
                </td>
                <td>Rs. {Number(item.totalPrice).toFixed(2)}</td>
                <td>
                  <button
                    type="button"
                    className="btn btn-outline"
                    onClick={() => handleRemove(item.id)}
                  >
                    Remove
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="card summary-card">
        <h2>Total: Rs. {Number(cart.totalAmount).toFixed(2)}</h2>
        <button type="button" className="btn btn-solid" onClick={() => navigate('/checkout')}>
          Proceed to Checkout
        </button>
      </div>
    </section>
  );
}
