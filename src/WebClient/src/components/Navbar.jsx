import { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import './Navbar.css';

const Navbar = () => {
    const { user, logout } = useAuth();
    const { cartCount } = useCart();
    const navigate = useNavigate();
    const location = useLocation();
    const [isMenuOpen, setIsMenuOpen] = useState(false);

    // Close menu when route changes
    useEffect(() => {
        setIsMenuOpen(false);
    }, [location]);

    // Close menu when clicking escape
    useEffect(() => {
        const handleEsc = (e) => {
            if (e.key === 'Escape') setIsMenuOpen(false);
        };
        window.addEventListener('keydown', handleEsc);
        return () => window.removeEventListener('keydown', handleEsc);
    }, []);

    // Prevent scrolling when menu is open
    useEffect(() => {
        if (isMenuOpen) {
            document.body.style.overflow = 'hidden';
        } else {
            document.body.style.overflow = 'unset';
        }
    }, [isMenuOpen]);

    const handleLogout = () => {
        logout();
        navigate('/');
        setIsMenuOpen(false);
    };

    const toggleMenu = () => {
        setIsMenuOpen(!isMenuOpen);
    };

    return (
        <>
            <nav className="navbar glass-panel">
                <div className="nav-content">
                    <Link to="/" className="nav-logo">
                        <h1><span className="text-gradient">LuxeMarket</span></h1>
                    </Link>

                    <button 
                        className="mobile-toggle" 
                        onClick={toggleMenu}
                        aria-label="Toggle navigation"
                    >
                        {isMenuOpen ? '✕' : '☰'}
                    </button>

                    <div className={`nav-overlay ${isMenuOpen ? 'active' : ''}`} onClick={() => setIsMenuOpen(false)} />

                    <div className={`nav-links ${isMenuOpen ? 'active' : ''}`}>
                        {/* Mobile Header inside drawer */}
                        {isMenuOpen && (
                            <div style={{ width: '100%', marginBottom: '1rem', borderBottom: '1px solid var(--glass-border)', paddingBottom: '1rem' }}>
                                <h2 style={{ fontSize: '1.2rem' }}>Menu</h2>
                            </div>
                        )}

                        <Link to="/">Products</Link>
                        <a href="/#categories">Categories</a>
                        
                        {user ? (
                            <>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.2rem' }}>
                                  <span style={{ fontSize: '0.9rem', color: 'var(--color-text-muted)' }}>Signed in as</span>
                                  <span style={{ fontWeight: '600' }}>{user.email?.split('@')[0]}</span>
                                </div>
                                <Link to="/orders" style={{ fontSize: '0.9rem', color: 'var(--color-primary)' }}>My Orders</Link>
                                <button onClick={handleLogout} style={{ background: 'none', border: 'none', color: 'var(--color-secondary)', cursor: 'pointer', font: 'inherit', textAlign: 'left', padding: 0 }}>
                                    Logout
                                </button>
                            </>
                        ) : (
                            <Link to="/login">Login</Link>
                        )}

                        <Link to="/cart" style={{ color: 'var(--color-primary)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <span style={{ fontSize: '1.2rem' }}>🛒</span>
                            <span>Cart ({cartCount})</span>
                        </Link>
                    </div>
                </div>
            </nav>
        </>
    );
};

export default Navbar;
