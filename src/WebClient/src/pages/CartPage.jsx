import { useState } from 'react';
import { useCart } from '../context/CartContext';
import { useAuth } from '../context/AuthContext';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';

const CartPage = () => {
    const { cartItems, removeFromCart, updateQuantity, cartTotal, clearCart } = useCart();
    const { user } = useAuth();
    const navigate = useNavigate();
    const [isCheckingOut, setIsCheckingOut] = useState(false);
    const [orderStatus, setOrderStatus] = useState(null); // null, 'success', 'error'

    const handleCheckout = async () => {
        if (!user) {
            navigate('/login');
            return;
        }

        setIsCheckingOut(true);
        setOrderStatus(null);

        try {
            // Call Order Service to place order
            // The backend automatically picks up the cart for the logged-in user
            const response = await api.post('/api/Orders/place');
            
            if (response.status === 200) {
                setOrderStatus('success');
                // Backend clears its cart, so we should clear our local view too
                clearCart();
            }
        } catch (error) {
            console.error("Checkout failed:", error);
            setOrderStatus('error');
        } finally {
            setIsCheckingOut(false);
        }
    };

    if (orderStatus === 'success') {
        return (
            <div className="container" style={{ paddingTop: '8rem', textAlign: 'center' }}>
                <div className="glass-panel" style={{ padding: '3rem', maxWidth: '600px', margin: '0 auto' }}>
                    <h1 className="text-gradient" style={{ marginBottom: '1rem' }}>Order Placed Successfully!</h1>
                    <p style={{ marginBottom: '2rem', color: 'var(--color-text-muted)' }}>
                        Thank you for your purchase. Your order is being processed.
                    </p>
                    <Link to="/" className="btn btn-primary">Continue Shopping</Link>
                </div>
            </div>
        );
    }

    if (cartItems.length === 0) {
        return (
            <div className="container" style={{ paddingTop: '8rem', textAlign: 'center' }}>
                <h1 style={{ marginBottom: '2rem' }}>Your Cart is Empty</h1>
                <Link to="/" className="btn btn-primary">Start Shopping</Link>
            </div>
        );
    }

    return (
        <div className="container page-header-spacer" style={{ paddingBottom: '4rem' }}>
            <h1 style={{ marginBottom: '2rem' }}>Shopping Cart</h1>
            
            {orderStatus === 'error' && (
                <div style={{ color: 'var(--color-secondary)', marginBottom: '1rem', border: '1px solid currentColor', padding: '1rem', borderRadius: '8px' }}>
                    Failed to place order. NOT SUFFICIENT STOCK or other error on server. Please try again.
                </div>
            )}

            <div className="glass-panel" style={{ padding: '2rem' }}>
                {cartItems.map(item => (
                    <div key={item.id} className="cart-item" style={{ 
                        display: 'flex', 
                        justifyContent: 'space-between', 
                        alignItems: 'center', 
                        padding: '1rem 0',
                        borderBottom: '1px solid var(--glass-border)'
                    }}>
                        <div style={{ flex: 1 }}>
                            <h3>{item.name || `Product ${item.id}`}</h3>
                            <p style={{ color: 'var(--color-text-muted)' }}>${item.price}</p>
                        </div>
                        
                        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                            <button 
                                className="btn glass-panel" 
                                style={{ padding: '0.2rem 0.8rem' }}
                                onClick={() => updateQuantity(item.id, item.quantity - 1)}
                            >-</button>
                            <span>{item.quantity}</span>
                            <button 
                                className="btn glass-panel" 
                                style={{ padding: '0.2rem 0.8rem' }}
                                onClick={() => updateQuantity(item.id, item.quantity + 1)}
                            >+</button>
                        </div>
                        
                        <div style={{ marginLeft: '2rem' }}>
                            <span style={{ fontWeight: 'bold' }}>${(item.price * item.quantity).toFixed(2)}</span>
                        </div>
                        
                        <button 
                            onClick={() => removeFromCart(item.id)}
                            style={{ 
                                marginLeft: '2rem', 
                                background: 'transparent', 
                                border: 'none', 
                                color: 'var(--color-secondary)',
                                cursor: 'pointer' 
                            }}
                        >
                            Remove
                        </button>
                    </div>
                ))}
                
                <div style={{ marginTop: '2rem', textAlign: 'right' }}>
                    <div style={{ fontSize: '1.5rem', marginBottom: '1rem' }}>
                        Total: <span className="text-gradient" style={{ fontWeight: 'bold' }}>${cartTotal.toFixed(2)}</span>
                    </div>
                    <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end' }}>
                        <button onClick={clearCart} className="btn glass-panel">Clear Cart</button>
                        <button 
                            onClick={handleCheckout} 
                            disabled={isCheckingOut}
                            className="btn btn-primary"
                            style={{ opacity: isCheckingOut ? 0.7 : 1, cursor: isCheckingOut ? 'not-allowed' : 'pointer' }}
                        >
                            {isCheckingOut ? 'Processing...' : 'Checkout'}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default CartPage;
