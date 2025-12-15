import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate, Link } from 'react-router-dom';

const LoginPage = () => {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const { login } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        try {
            await login(email, password);
            navigate('/');
        } catch (err) {
            setError('Login failed. Please check your credentials.');
        }
    };

    return (
        <div className="auth-container">
            <div className="glass-panel" style={{ padding: '3rem', width: '100%', maxWidth: '400px' }}>
                <h2 style={{ textAlign: 'center', marginBottom: '2rem' }}>Welcome Back</h2>
                
                {error && <div style={{ color: 'var(--color-secondary)', marginBottom: '1rem', textAlign: 'center' }}>{error}</div>}
                
                <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
                    <div>
                        <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.9rem' }}>Email</label>
                        <input 
                            type="email" 
                            required
                            value={email}
                            onChange={e => setEmail(e.target.value)}
                            style={{ 
                                width: '100%', 
                                padding: '0.8rem', 
                                borderRadius: 'var(--radius-sm)', 
                                border: '1px solid var(--glass-border)',
                                background: 'rgba(255,255,255,0.05)',
                                color: 'white',
                                outline: 'none'
                            }}
                        />
                    </div>
                    
                    <div>
                        <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.9rem' }}>Password</label>
                        <input 
                            type="password" 
                            required
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            style={{ 
                                width: '100%', 
                                padding: '0.8rem', 
                                borderRadius: 'var(--radius-sm)', 
                                border: '1px solid var(--glass-border)',
                                background: 'rgba(255,255,255,0.05)',
                                color: 'white',
                                outline: 'none'
                            }}
                        />
                    </div>
                    
                    <button type="submit" className="btn btn-primary" style={{ marginTop: '1rem' }}>Log In</button>
                </form>
                
                <div style={{ textAlign: 'center', marginTop: '1.5rem', fontSize: '0.9rem', color: 'var(--color-text-muted)' }}>
                    Don't have an account? <Link to="/register" style={{ color: 'var(--color-primary)' }}>Sign Up</Link>
                </div>
            </div>
        </div>
    );
};

export default LoginPage;
