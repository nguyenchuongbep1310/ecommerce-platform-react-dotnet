import { useState, useEffect } from 'react';
import { productService } from '../services/api';
import { useCart } from '../context/CartContext';
import { Link, useSearchParams } from 'react-router-dom';

const HomePage = () => {
    const [products, setProducts] = useState([]);
    const [filteredProducts, setFilteredProducts] = useState([]);
    const [categories, setCategories] = useState([]);
    const [selectedCategory, setSelectedCategory] = useState('All');
    const [searchTerm, setSearchTerm] = useState('');
    const [priceRange, setPriceRange] = useState({ min: '', max: '' });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const { addToCart } = useCart();
    const [searchParams] = useSearchParams();

    const fetchProducts = async (params = {}) => {
        setLoading(true);
        try {
            // Build query object from params or current state
            const query = {
                search: params.search !== undefined ? params.search : searchTerm,
                category: params.category !== undefined ? (params.category === 'All' ? '' : params.category) : (selectedCategory === 'All' ? '' : selectedCategory),
                minPrice: params.minPrice !== undefined ? params.minPrice : priceRange.min,
                maxPrice: params.maxPrice !== undefined ? params.maxPrice : priceRange.max
            };

            // Remove empty/null values
            Object.keys(query).forEach(key => (query[key] === '' || query[key] === null) && delete query[key]);

            const data = await productService.getAll(query);
            
            setProducts(data);
            setFilteredProducts(data);

            // Only extract categories if we have a full list (no filters applied yet) or if it's the first load
            if (categories.length === 0 && Object.keys(query).length === 0) {
                 const cats = ['All', ...new Set(data.map(p => p.category || 'Uncategorized'))];
                 setCategories(cats.filter(c => c));
            } else if (categories.length === 0) {
                 // Fallback if initial load had filters
                 setCategories(['All', 'Electronics', 'Accessories', 'Fashion']); 
            }

        } catch (err) {
            console.error(err);
            setError('Failed to load products.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        // Initial load
        const catParam = searchParams.get('category');
        if (catParam) {
            setSelectedCategory(catParam);
            fetchProducts({ category: catParam });
        } else {
            fetchProducts();
        }
    }, []); // Only run on mount

    const handleSearch = (e) => {
        e.preventDefault();
        fetchProducts();
    };

    const filterByCategory = (category) => {
        setSelectedCategory(category);
        fetchProducts({ category });
    };

    if (loading && products.length === 0) return (
        <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <div className="animate-fade-in">Loading luxury items...</div>
        </div>
    );

    return (
        <main className="container page-header-spacer" style={{ textAlign: 'center', paddingBottom: '4rem' }}>
            <h1 className="animate-fade-in" style={{ 
            fontSize: 'clamp(2.5rem, 8vw, 5rem)', 
            lineHeight: '1.1', 
            marginBottom: '1.5rem',
            fontWeight: '800' 
            }}>
            Discover the <br />
            <span className="text-gradient">Extraordinary</span>
            </h1>
            
            <p className="animate-fade-in" style={{ 
            fontSize: 'clamp(1rem, 4vw, 1.25rem)',
            color: 'var(--color-text-muted)', 
            marginBottom: '3rem', 
            maxWidth: '600px', 
            marginInline: 'auto',
            animationDelay: '0.1s' 
            }}>
            Experience the premium selection of products with our new immersive shopping platform. Curated just for you.
            </p>
            
            {error && <div style={{ color: 'var(--color-secondary)', marginBottom: '2rem' }}>Note: {error}</div>}

            {/* Search and Filters */}
            <form onSubmit={handleSearch} className="glass-panel" style={{ 
                marginBottom: '3rem', 
                padding: '1.5rem',
                display: 'flex',
                gap: '1rem',
                flexWrap: 'wrap',
                justifyContent: 'center',
                alignItems: 'center'
            }}>
                <input 
                    type="text" 
                    placeholder="Search products..." 
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    style={{ padding: '0.5rem', borderRadius: '4px', border: '1px solid var(--glass-border)', background: 'rgba(255,255,255,0.05)', color: 'white' }}
                />
                <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                    <input 
                        type="number" 
                        placeholder="Min Price" 
                        value={priceRange.min}
                        onChange={(e) => setPriceRange({...priceRange, min: e.target.value})}
                        style={{ width: '80px', padding: '0.5rem', borderRadius: '4px', border: '1px solid var(--glass-border)', background: 'rgba(255,255,255,0.05)', color: 'white' }}
                    />
                    <span>-</span>
                    <input 
                        type="number" 
                        placeholder="Max Price" 
                        value={priceRange.max}
                        onChange={(e) => setPriceRange({...priceRange, max: e.target.value})}
                        style={{ width: '80px', padding: '0.5rem', borderRadius: '4px', border: '1px solid var(--glass-border)', background: 'rgba(255,255,255,0.05)', color: 'white' }}
                    />
                </div>
                <button type="submit" className="btn btn-primary" style={{ padding: '0.5rem 1.5rem' }}>Search</button>
            </form>
            
            <div id="products" style={{ textAlign: 'left', marginBottom: '1.5rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
                <h2 style={{ fontSize: '2rem' }}>Featured Products</h2>
                
                <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                    {categories.map(cat => (
                        <button 
                            key={cat}
                            onClick={() => filterByCategory(cat)}
                            className={selectedCategory === cat ? "btn btn-primary" : "btn glass-panel"}
                            style={{ 
                                padding: '0.4rem 1rem', 
                                fontSize: '0.9rem',
                                border: selectedCategory === cat ? 'none' : '1px solid var(--glass-border)'
                            }}
                        >
                            {cat}
                        </button>
                    ))}
                </div>
            </div>
            
            <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', 
            gap: '2rem' 
            }}>
            {filteredProducts.map(product => {
                // Simple Image Mapper based on name keyword matching
                const getImage = (name) => {
                    const n = name.toLowerCase();
                    if (n.includes('iphone')) return '/products/iphone_15_pro.png';
                    if (n.includes('samsung') || n.includes('galaxy')) return '/products/samsung_galaxy_s24.png';
                    if (n.includes('headphone') || n.includes('sony')) return '/products/headphones.png';
                    if (n.includes('macbook') || n.includes('laptop')) return '/products/macbook_pro_m3.png';
                    if (n.includes('bag') || n.includes('leather')) return '/products/leather_bag.png';
                    if (n.includes('coffee')) return '/products/coffee_maker.png';
                    if (n.includes('mouse')) return '/products/wireless_mouse.png';
                    if (n.includes('watch')) return '/products/samsung_galaxy_s24.png'; // Fallback to tech generic
                    if (n.includes('keyboard')) return '/products/macbook_pro_m3.png'; // Fallback
                    return null;
                };

                const imageSrc = getImage(product.name);

                return (
                <div key={product.id} className="glass-panel" style={{ 
                display: 'flex', 
                flexDirection: 'column', 
                overflow: 'hidden',
                transition: 'transform 0.3s ease',
                cursor: 'pointer'
                }}
                onMouseEnter={(e) => e.currentTarget.style.transform = 'translateY(-5px)'}
                onMouseLeave={(e) => e.currentTarget.style.transform = 'translateY(0)'}
                >
                <div style={{ 
                    height: '280px', 
                    background: `linear-gradient(45deg, rgba(255,255,255,0.05), rgba(255,255,255,0.02))`,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    overflow: 'hidden'
                }}>
                    {imageSrc ? (
                        <img src={imageSrc} alt={product.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                    ) : (
                        <span style={{ fontSize: '3rem', opacity: 0.2 }}>✨</span>
                    )}
                </div>
                <div style={{ padding: '1.5rem', textAlign: 'left', flex: 1, display: 'flex', flexDirection: 'column' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
                    <h3 style={{ fontSize: '1.1rem', fontWeight: '600' }}>{product.name}</h3>
                    <span style={{ color: 'var(--color-primary)', fontWeight: '700' }}>${product.price}</span>
                    </div>
                    <div style={{ marginBottom: '0.5rem' }}>
                       <span style={{ fontSize: '0.75rem', background: 'rgba(255,255,255,0.1)', padding: '0.2rem 0.5rem', borderRadius: '4px' }}>{product.category || 'General'}</span>
                    </div>
                    <p style={{ fontSize: '0.9rem', color: 'var(--color-text-muted)', flex: 1 }}>{product.description}</p>
                    <button 
                        onClick={() => addToCart(product)}
                        className="btn btn-primary" 
                        style={{ width: '100%', marginTop: '1.5rem', padding: '0.5rem' }}
                    >
                        Add to Cart
                    </button>
                </div>
                </div>
            )})}
            {filteredProducts.length === 0 && (
                <div style={{ gridColumn: '1/-1', padding: '2rem', textAlign: 'center', color: 'var(--color-text-muted)' }}>
                    No products found matching your search.
                </div>
            )}
            </div>
        </main>
    );
};

export default HomePage;
