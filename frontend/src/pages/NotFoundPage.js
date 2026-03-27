import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <section className="section card center">
      <h1>Page Not Found</h1>
      <p>The page you requested does not exist.</p>
      <Link to="/" className="btn btn-solid">Go Home</Link>
    </section>
  );
}
