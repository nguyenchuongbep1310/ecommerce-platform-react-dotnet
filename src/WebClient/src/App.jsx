import { Routes, Route, Link, useNavigate } from 'react-router-dom';
import { useCart } from './context/CartContext';
import { useAuth } from './context/AuthContext';
import HomePage from './pages/HomePage';
import CartPage from './pages/CartPage';
import CollectionsPage from './pages/CollectionsPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import './App.css';

function App() {
  const { cartCount } = useCart();
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <div className="app">
      <nav className="glass-panel" style={{ 
        position: 'fixed', 
        top: '1rem', 
        left: '50%', 
        transform: 'translateX(-50%)', 
        width: 'calc(100% - 2rem)', 
        maxWidth: '1280px', 
        padding: '1rem 2rem', 
        zIndex: 100,
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center' 
      }}>
        <Link to="/">
          <h1 style={{ fontSize: '1.5rem', fontWeight: '700' }}><span className="text-gradient">LuxeMarket</span></h1>
        </Link>
        <div style={{ display: 'flex', gap: '2rem', fontWeight: '500', alignItems: 'center' }}>
          <Link to="/">Products</Link>
          <a href="#categories">Categories</a>
          
          {user ? (
              <>
                 <span style={{ fontSize: '0.9rem', color: 'var(--color-text-muted)' }}>Hi, {user.email?.split('@')[0]}</span>
                 <button onClick={handleLogout} style={{ background: 'none', border: 'none', color: 'inherit', cursor: 'pointer', font: 'inherit' }}>Logout</button>
              </>
          ) : (
              <Link to="/login">Login</Link>
          )}

          <Link to="/cart" style={{ color: 'var(--color-primary)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <span style={{ fontSize: '1.2rem' }}>🛒</span>
            <span>Cart ({cartCount})</span>
          </Link>
        </div>
      </nav>
      
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
