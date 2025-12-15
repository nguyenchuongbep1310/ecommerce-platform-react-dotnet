import { Routes, Route, Link, useNavigate } from 'react-router-dom';
import { useCart } from './context/CartContext';
import { useAuth } from './context/AuthContext';
import HomePage from './pages/HomePage';
import CartPage from './pages/CartPage';
import CollectionsPage from './pages/CollectionsPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import './App.css';

import Navbar from './components/Navbar';

function App() {
  const { cartCount } = useCart();
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <div className="app">
      <Navbar />
      
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/collections" element={<CollectionsPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Routes>
    </div>
  );
}

export default App;
