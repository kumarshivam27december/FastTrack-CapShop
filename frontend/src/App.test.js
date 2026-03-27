import { render, screen } from '@testing-library/react';
import App from './App';

test('renders brand name', () => {
  render(<App />);
  const brand = screen.getAllByText(/CapShop/i)[0];
  expect(brand).toBeInTheDocument();
});
