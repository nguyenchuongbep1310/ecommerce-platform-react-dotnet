import { Link } from 'react-router-dom';

const collections = [
    { id: 'Electronics', name: 'Electronics', description: 'Cutting-edge gadgets and devices.', gradient: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' },
    { id: 'Accessories', name: 'Accessories', description: 'Essential add-ons for your daily life.', gradient: 'linear-gradient(135deg, #ff9a9e 0%, #fecfef 99%, #fecfef 100%)' },
    { id: 'Fashion', name: 'Fashion', description: 'Style meets comfort and elegance.', gradient: 'linear-gradient(120deg, #f093fb 0%, #f5576c 100%)' },
    { id: 'All', name: 'View All', description: 'Browse our entire catalog.', gradient: 'linear-gradient(to right, #4facfe 0%, #00f2fe 100%)' }
];

const CollectionsPage = () => {
    return (
        <div className="container page-header-spacer" style={{ paddingBottom: '4rem' }}>
            <h1 className="animate-fade-in" style={{ textAlign: 'center', marginBottom: '1rem', fontSize: 'clamp(2rem, 5vw, 3rem)' }}>
                <span className="text-gradient">Curated Collections</span>
            </h1>
            <p className="animate-fade-in" style={{ textAlign: 'center', color: 'var(--color-text-muted)', marginBottom: '4rem', maxWidth: '600px', marginInline: 'auto' }}>
                Explore our carefully selected categories designed to elevate your lifestyle.
            </p>

            <div style={{ 
                display: 'grid', 
                gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', 
                gap: '2rem' 
            }}>
                {collections.map((collection, index) => (
                    <Link 
                        to={`/?category=${collection.id}`} 
                        key={collection.id} 
                        className="glass-panel animate-fade-in"
                        style={{ 
                            textDecoration: 'none', 
                            color: 'inherit',
                            display: 'flex',
                            flexDirection: 'column',
                            height: '300px',
                            overflow: 'hidden',
                            transition: 'transform 0.3s ease, box-shadow 0.3s ease',
                            animationDelay: `${index * 0.1}s`
                        }}
                        onMouseEnter={(e) => {
                            e.currentTarget.style.transform = 'translateY(-10px)';
                            e.currentTarget.style.boxShadow = '0 20px 40px rgba(0,0,0,0.4)';
                        }}
                        onMouseLeave={(e) => {
                            e.currentTarget.style.transform = 'translateY(0)';
                            e.currentTarget.style.boxShadow = 'var(--glass-shadow)';
                        }}
                    >
                        <div style={{ 
                            flex: 2, 
                            background: collection.gradient,
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center'
                        }}>
                             <span style={{ fontSize: '4rem', color: 'rgba(255,255,255,0.3)' }}>📦</span>
                        </div>
                        <div style={{ flex: 1, padding: '1.5rem', background: 'rgba(0,0,0,0.2)' }}>
                            <h2 style={{ fontSize: '1.5rem', marginBottom: '0.5rem' }}>{collection.name}</h2>
                            <p style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>{collection.description}</p>
                        </div>
                    </Link>
                ))}
            </div>
        </div>
    );
};

export default CollectionsPage;
