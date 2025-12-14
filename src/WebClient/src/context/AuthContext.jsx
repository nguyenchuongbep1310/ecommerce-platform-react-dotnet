import { createContext, useContext, useState, useEffect } from 'react';
import api from '../services/api';

const AuthContext = createContext();

export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Check for saved token on load
        const token = localStorage.getItem('token');
        const userData = localStorage.getItem('user');
        if (token && userData) {
            api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
            setUser(JSON.parse(userData));
        }
        setLoading(false);
    }, []);

    const login = async (email, password) => {
        try {
            const response = await api.post('/api/Auth/login', { email, password });
            const { token, ...userData } = response.data;
            
            // Allow backend response to not contain "user" object directly if it's flat
            const userObj = { 
                email: userData.email || email, 
                id: userData.id || userData.userId // Handle various potential response formats
            };

            localStorage.setItem('token', token);
            localStorage.setItem('user', JSON.stringify(userObj));
            
            api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
            setUser(userObj);
            return true;
        } catch (error) {
            console.error('Login failed:', error);
            throw error;
        }
    };

    const register = async (email, password) => {
        try {
            await api.post('/api/Auth/register', { email, password });
            return true;
        } catch (error) {
            console.error('Registration failed:', error);
            throw error;
        }
    };

    const logout = () => {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        delete api.defaults.headers.common['Authorization'];
        setUser(null);
    };

    return (
        <AuthContext.Provider value={{ user, login, register, logout, loading }}>
            {children}
        </AuthContext.Provider>
    );
};
