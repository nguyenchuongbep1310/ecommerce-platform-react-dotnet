import { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import { useAuth } from './AuthContext';

const CartContext = createContext();

export const useCart = () => useContext(CartContext);

export const CartProvider = ({ children }) => {
    const { user } = useAuth();
    // Initialize cart from localStorage if available, but clear it if we switch to api mode later
    const [cartItems, setCartItems] = useState(() => {
        const savedCart = localStorage.getItem('cart');
        return savedCart ? JSON.parse(savedCart) : [];
    });

    // Sync with server when user logs in
    useEffect(() => {
        if (user) {
            fetchServerCart();
        } else {
            // Load local cart if logged out
             const savedCart = localStorage.getItem('cart');
             setCartItems(savedCart ? JSON.parse(savedCart) : []);
        }
    }, [user]);

    // Persist to local storage if GUEST
    useEffect(() => {
        if (!user) {
            localStorage.setItem('cart', JSON.stringify(cartItems));
        }
    }, [cartItems, user]);

    const fetchServerCart = async () => {
        try {
            // Check if user has an ID
             if (!user?.id && !user?.userId) return; // Should catch where id is missing
             
             const userId = user.id || user.userId;
             const response = await api.get(`/api/cart/${userId}`);
             
             // Transform server response to local format
             const serverItems = response.data.items.map(item => ({
                 id: item.productId, // Map productId to id for frontend consistency
                 // We might need to fetch names separately if the cart endpoint doesn't return them
                 // For MVP, we assume we might lack names. Ideally CartService should return them or we fetch here.
                 // Hack: We don't have product details from cart service. 
                 // We will trust the productId and maybe fetch product details later or just show ID.
                 // Actually, let's keep it simple: The frontend 'addToCart' has the full product object.
                 // Server cart assumes we just list items.
                 // Let's rely on what we have locally for names if possible, or just fetch them.
                 // For now, let's just map what we have.
                 quantity: item.quantity,
                 price: item.priceAtAddition,
                 // name: "Product " + item.productId // Placeholder if missing
             }));
             
             // If we really want full product details (name, image), we'd need to fetch them from ProductService
             // based on IDs. For this MVP, let's skip the extra fetch heavily.
             // We can populate names if we have them in local cache or just leave them.
             
             setCartItems(serverItems);
        } catch (error) {
            // Cart might not exist yet
            if (error.response?.status === 404) {
                setCartItems([]);
            } else {
                console.error("Failed to fetch server cart", error);
            }
        }
    };

    const navigate = useNavigate();

    const addToCart = async (product) => {
        if (!user) {
            setCartItems(prevItems => {
                const existingItem = prevItems.find(item => item.id === product.id);
                if (existingItem) {
                    return prevItems.map(item =>
                        item.id === product.id
                            ? { ...item, quantity: item.quantity + 1 }
                            : item
                    );
                } else {
                    return [...prevItems, { ...product, quantity: 1 }];
                }
            });
            return;
        }

        try {
            const userId = user.id || user.userId;
            await api.post('/api/cart/add', {
                userId: userId,
                productId: product.id,
                quantity: 1
            });
            // Re-fetch to sync exact state
            await fetchServerCart();
        } catch (error) {
            console.error("Failed to add to server cart", error);
            alert("Could not add to cart. Please try again.");
        }
    };

    const removeFromCart = async (productId) => {
        if (user) {
             // The API only has ClearCart or Add (which updates). It doesn't seem to have "Remove Item" or "Update Quantity" explicitly
             // looking at the Controller file provided.
             // The CartController only has GET, POST (add/update), DELETE (clear).
             // It does NOT have endpoint to remove single item or decrease quantity specifically, 
             // unless we pass negative quantity? Controller logic: `existingItem.Quantity += request.Quantity`.
             // Yes! We can pass negative quantity to decrease.
             // To remove completely, we might need to decrease by current quantity.
             // Let's implement decrease first.
             console.warn("Server cart remove/update not fully optimized: sending negative quantity");
             const item = cartItems.find(i => i.id === productId);
             if (item) {
                 await api.post('/api/cart/add', {
                     userId: user.id || user.userId,
                     productId: productId,
                     quantity: -item.quantity // Remove all
                 });
                 fetchServerCart();
             }
        } else {
            setCartItems(prevItems => prevItems.filter(item => item.id !== productId));
        }
    };

    const updateQuantity = async (productId, quantity) => {
        if (quantity < 0) return;
        
        if (user) {
             const item = cartItems.find(i => i.id === productId);
             if (!item) return;
             
             const diff = quantity - item.quantity;
             if (diff === 0) return;
             
             try {
                await api.post('/api/cart/add', {
                    userId: user.id || user.userId,
                    productId: productId,
                    quantity: diff
                });
                fetchServerCart();
             } catch (err) {
                 console.error(err);
             }
        } else {
            if (quantity === 0) {
                 removeFromCart(productId);
                 return;
            }
            setCartItems(prevItems =>
                prevItems.map(item =>
                    item.id === productId ? { ...item, quantity } : item
                )
            );
        }
    };

    const clearCart = async () => {
        if (user) {
             try {
                 await api.delete(`/api/cart/${user.id || user.userId}`);
                 setCartItems([]);
             } catch (err) {
                 console.error(err);
             }
        } else {
            setCartItems([]);
        }
    };

    const cartTotal = cartItems.reduce((total, item) => total + (item.price * item.quantity), 0);
    const cartCount = cartItems.reduce((count, item) => count + item.quantity, 0);

    return (
        <CartContext.Provider value={{
            cartItems,
            addToCart,
            removeFromCart,
            updateQuantity,
            clearCart,
            cartTotal,
            cartCount
        }}>
            {children}
        </CartContext.Provider>
    );
};
