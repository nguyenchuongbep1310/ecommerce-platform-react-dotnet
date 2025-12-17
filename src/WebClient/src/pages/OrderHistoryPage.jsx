import { useState, useEffect } from 'react';
import { orderService } from '../services/api';
import { Link } from 'react-router-dom';

const OrderHistoryPage = () => {
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const fetchOrders = async () => {
            try {
                const data = await orderService.getHistory();
                // If it returns a message object (no orders), handle it
                if (data.message) {
                    setOrders([]);
                } else {
                    setOrders(data);
                }
            } catch (err) {
                console.error(err);
                setError('Failed to load order history.');
            } finally {
                setLoading(false);
            }
        };

        fetchOrders();
    }, []);

    if (loading) return (
        <div style={{ minHeight: '60vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <div className="animate-fade-in">Loading processing history...</div>
        </div>
    );

    return (
        <main className="container page-header-spacer" style={{ paddingBottom: '4rem' }}>
            <h1 className="animate-fade-in" style={{ 
                fontSize: 'clamp(2rem, 5vw, 3rem)', 
                marginBottom: '2rem',
                textAlign: 'center'
            }}>
                Order <span className="text-gradient">History</span>
            </h1>

            {error ? (
                <div style={{ textAlign: 'center', color: 'var(--color-secondary)' }}>{error}</div>
            ) : orders.length === 0 ? (
                <div className="glass-panel" style={{ padding: '3rem', textAlign: 'center' }}>
                    <p style={{ fontSize: '1.2rem', marginBottom: '1.5rem' }}>You haven't placed any orders yet.</p>
                    <Link to="/" className="btn btn-primary">Start Shopping</Link>
                </div>
            ) : (
                <div style={{ display: 'grid', gap: '1.5rem' }}>
                    {orders.map(order => (
                        <div key={order.id} className="glass-panel" style={{ padding: '1.5rem', animation: 'fadeIn 0.5s ease' }}>
                            <div style={{ 
                                display: 'flex', 
                                justifyContent: 'space-between', 
                                alignItems: 'center', 
                                borderBottom: '1px solid var(--glass-border)',
                                paddingBottom: '1rem',
                                marginBottom: '1rem',
                                flexWrap: 'wrap',
                                gap: '1rem'
                            }}>
                                <div>
                                    <h3 style={{ margin: 0 }}>Order #{order.id}</h3>
                                    <span style={{ fontSize: '0.9rem', color: 'var(--color-text-muted)' }}>
                                        {new Date(order.orderDate).toLocaleDateString()}
                                    </span>
                                </div>
                                <div style={{ textAlign: 'right' }}>
                                    <div style={{ fontSize: '1.2rem', fontWeight: 'bold' }}>${order.totalAmount.toFixed(2)}</div>
                                    <span style={{ 
                                        padding: '0.25rem 0.75rem', 
                                        borderRadius: '20px', 
                                        fontSize: '0.85rem',
                                        background: order.status === 'Processing' ? 'rgba(255, 215, 0, 0.2)' : 'rgba(0, 255, 0, 0.2)',
                                        color: order.status === 'Processing' ? '#ffd700' : '#4ade80',
                                        border: `1px solid ${order.status === 'Processing' ? 'rgba(255, 215, 0, 0.3)' : 'rgba(0, 255, 0, 0.3)'}`
                                    }}>
                                        {order.status}
                                    </span>
                                </div>
                            </div>

                            <div style={{ display: 'grid', gap: '0.5rem' }}>
                                {order.items.map((item, index) => (
                                    <div key={index} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.95rem' }}>
                                        <div>
                                            {/* Note: Ideally we fetch product names, but for MVP we might just show ID or generic text if name missing from Order Service projection */}
                                            <span style={{ color: 'var(--color-text-muted)' }}>{item.quantity}x</span> Product #{item.productId}
                                        </div>
                                        <div>${item.unitPrice.toFixed(2)}</div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </main>
    );
};

export default OrderHistoryPage;
