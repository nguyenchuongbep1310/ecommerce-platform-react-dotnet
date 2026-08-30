# 🚀 E-commerce Platform - Golang & Kafka Migration

[![Go](https://img.shields.io/badge/Go-1.21-00ADD8?logo=go&logoColor=white)](https://golang.org/)
[![Kafka](https://img.shields.io/badge/Kafka-3.5-000000?logo=apache-kafka&logoColor=white)](https://kafka.apache.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 📋 Project Overview

A **production-ready microservices platform** built with **Go**, **Kafka**, and **PostgreSQL**. This project demonstrates modern cloud-native architecture patterns with emphasis on scalability, reliability, and operational simplicity.

**Status**: 🚀 **Phase 1 Complete** ✅ | 🏗️ **Phase 2 In Progress**

### 🌟 Key Highlights

| Feature | Technology | Status |
|---------|-----------|--------|
| **Microservices** | Go 1.21 | ✅ Production Ready |
| **Message Broker** | Kafka 3.5 + ZooKeeper | ✅ Running |
| **Databases** | PostgreSQL 15 (4 instances) | ✅ Ready |
| **Caching** | Redis 7 | ✅ Running |
| **Monitoring** | Prometheus + Grafana + Jaeger | ✅ Operational |
| **Authentication** | JWT (24h tokens) | ✅ Implemented |
| **Clean Architecture** | Domain → Application → Infrastructure → API | ✅ Pattern Used |
| **Event-Driven** | Kafka Producers/Consumers | ✅ Ready |

---

## 🏗️ Architecture Overview

```
┌────────────────────────────────────────────────────────┐
│                  API Gateway (Future)                  │
│              Port: 8000 | Auth, Routing                │
└─────────────────────────┬──────────────────────────────┘
                          │
       ┌──────────────────┼──────────────────┐
       │                  │                  │
       ▼                  ▼                  ▼
┌────────────┐    ┌────────────┐    ┌────────────┐
│   User     │    │   Cart     │    │  Product   │
│  Service   │    │  Service   │    │  Service   │
│  :5001     │    │  :5003     │    │  :5002     │
└────────────┘    └────────────┘    └────────────┘
       │                                    │
       └────────────────┬───────────────────┘
                        ▼
              ┌─────────────────────┐
              │   Kafka Cluster     │
              │  (3 Brokers)        │
              │  ZooKeeper          │
              └─────────────────────┘
                        ▼
        ┌───────────────────────────────┐
        │  PostgreSQL (4 databases)     │
        │  - User DB                    │
        │  - Product DB                 │
        │  - Cart DB                    │
        │  - Order DB                   │
        └───────────────────────────────┘
```

---

## 📑 Quick Navigation

- [Getting Started](#-getting-started) - 5-minute setup
- [Services](#-services) - Implementation status
- [API Endpoints](#-api-endpoints) - Test endpoints
- [Architecture](#-architecture-details) - Design patterns
- [Troubleshooting](#-troubleshooting) - Common issues

---

## ⚡️ Getting Started

### Prerequisites

Ensure you have installed:

```bash
# Check versions
docker --version          # Docker 20.10+
docker compose version    # Compose V2
go version               # Go 1.21+
git --version            # Git latest
```

### 🚀 Quick Start (5 minutes)

#### Step 1: Start Infrastructure

```bash
cd phase-1-foundation
docker-compose up -d

# Wait for services to be healthy
docker-compose ps
```

Expected: All 13+ containers showing "Up (healthy)"

#### Step 2: Set Up Databases

```bash
# Create user database schema
docker exec phase-1-foundation-postgres-user-1 psql -U postgres -d user_db << 'EOF'
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_users_email ON users(email);
EOF
```

#### Step 3: Start UserService

```bash
cd src/user-service

# Install dependencies
go mod tidy

# Run service
go run cmd/main.go

# Service running on http://localhost:5001
```

#### Step 4: Test API

```bash
# Health check
curl http://localhost:5001/health

# Register user
curl -X POST http://localhost:5001/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!",
    "confirmPassword": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }'

# Login (get JWT token)
curl -X POST http://localhost:5001/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!"
  }'

# Get profile (use token from login)
curl http://localhost:5001/auth/profile \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## 🎯 Services

### Phase 1: Foundation ✅ COMPLETE

| Component | Status | Details |
|-----------|--------|---------|
| **Kafka Cluster** | ✅ Ready | 3 brokers + ZooKeeper |
| **PostgreSQL** | ✅ Ready | 4 databases (user, product, cart, order) |
| **Redis** | ✅ Ready | Caching layer |
| **Monitoring** | ✅ Ready | Prometheus, Grafana, Jaeger |
| **Kafka UI** | ✅ Ready | http://localhost:8888 |

### Phase 2: UserService ✅ COMPLETE

| Feature | Status | Implementation |
|---------|--------|-----------------|
| **Registration** | ✅ Done | Email validation, bcrypt hashing |
| **Login** | ✅ Done | JWT token generation (24h expiry) |
| **Profile** | ✅ Done | Protected endpoint with Bearer auth |
| **Database** | ✅ Done | PostgreSQL with GORM |
| **Kafka** | ✅ Ready | Event producer scaffolded |

**Location**: `src/user-service`

**API Endpoints**:
- `POST /auth/register` - User registration
- `POST /auth/login` - User login (returns JWT)
- `GET /auth/profile` - Get user profile (requires JWT)
- `GET /health` - Health check

### Phase 2b: CartService 🚧 PLANNED

- Add to cart
- Remove from cart
- Get cart
- Kafka event publishing

### Phase 2c: PaymentService 🚧 PLANNED

- Process payment
- Payment status tracking
- Stripe integration

### Phase 3: ProductService 🚧 PLANNED

- Product catalog
- Inventory management
- Search functionality

---

## 🌐 Access Points

Once everything is running:

| Service | URL | Purpose |
|---------|-----|---------|
| **Kafka UI** | http://localhost:8888 | Topic management |
| **Grafana** | http://localhost:3000 | Metrics (admin/admin) |
| **Prometheus** | http://localhost:9090 | Metrics collection |
| **Jaeger** | http://localhost:16686 | Distributed tracing |
| **Redis** | localhost:6379 | Caching (internal) |
| **PostgreSQL** | localhost:5432, 5434-5436 | Databases |

---

## 📊 Project Structure

```
ecommerce-platform/
├── phase-1-foundation/           # Infrastructure setup
│   ├── docker-compose.yml        # All services (Kafka, DB, monitoring)
│   ├── scripts/
│   │   ├── setup.sh             # One-command setup
│   │   ├── debug.sh             # Diagnostic script
│   │   └── recovery.sh          # Recovery script
│   ├── monitoring/
│   │   └── prometheus.yml       # Prometheus config
│   └── golang-shared/           # Shared Go libraries
│       └── pkg/logger/          # Logging utilities
│
├── src/
│   ├── user-service/            # ✅ Phase 2 - User Management
│   │   ├── cmd/main.go
│   │   ├── internal/
│   │   │   ├── domain/          # Domain models
│   │   │   ├── repository/      # Data access
│   │   │   ├── usecase/         # Business logic
│   │   │   ├── handler/         # HTTP handlers
│   │   │   ├── middleware/      # JWT auth middleware
│   │   │   └── kafka/           # Event producer
│   │   ├── migrations/          # Database schema
│   │   └── config/              # Configuration
│   │
│   ├── cart-service/            # 🚧 Phase 2b - Shopping Cart
│   ├── payment-service/         # 🚧 Phase 2c - Payments
│   └── product-service/         # 🚧 Phase 3 - Catalog
│
└── README.md                     # This file
```

---

## 🔧 Development Workflow

### Running UserService Locally

```bash
cd src/user-service

# Option 1: Direct execution
go run cmd/main.go

# Option 2: Build binary
go build -o user-service cmd/main.go
./user-service

# With custom port
USER_SERVICE_PORT=5002 go run cmd/main.go

# With custom database
DATABASE_URL_USER="postgres://user:pass@localhost:5432/mydb?sslmode=disable" go run cmd/main.go
```

### Environment Variables

```bash
# Database connection (required)
DATABASE_URL_USER=postgres://postgres:postgres@localhost:5432/user_db?sslmode=disable

# JWT secret (required for token generation)
JWT_SECRET=your-secret-key-min-32-characters

# Service configuration
USER_SERVICE_PORT=5001
ENVIRONMENT=development
```

### Running Tests

```bash
cd src/user-service

# Run all tests
go test ./...

# Run with coverage
go test -cover ./...

# Run specific test
go test -run TestRegisterUser ./internal/usecase/...
```

### Building Docker Image

```bash
cd src/user-service

# Build image
docker build -t user-service:latest .

# Run container
docker run -p 5001:5001 \
  -e DATABASE_URL_USER="postgres://..." \
  -e JWT_SECRET="..." \
  user-service:latest
```

---

## 🧪 API Testing

### Using curl

```bash
# 1. Register
REGISTER=$(curl -s -X POST http://localhost:5001/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!",
    "confirmPassword": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }')

# 2. Login
LOGIN=$(curl -s -X POST http://localhost:5001/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!"
  }')

# Extract token (requires jq)
TOKEN=$(echo $LOGIN | jq -r '.accessToken')

# 3. Get profile
curl -s http://localhost:5001/auth/profile \
  -H "Authorization: Bearer $TOKEN" | jq
```

### Using Postman

1. Import `phase-1-foundation/postman_collection.json`
2. Set environment variables
3. Test endpoints in order:
   - Register → Login → Get Profile

---

## 🏗️ Architecture Details

### Clean Architecture Pattern

```
┌─────────────────────────────────┐
│   API Layer (Handlers)          │
│   - HTTP endpoints              │
│   - Request/response mapping    │
└─────────────┬───────────────────┘
              │
┌─────────────▼───────────────────┐
│   Use Case Layer (Business)     │
│   - Business logic              │
│   - Orchestration               │
│   - Domain validations          │
└─────────────┬───────────────────┘
              │
┌─────────────▼───────────────────┐
│   Repository Layer              │
│   - Data persistence            │
│   - Database queries            │
│   - Cache operations            │
└─────────────┬───────────────────┘
              │
┌─────────────▼───────────────────┐
│   Domain Layer (Entities)       │
│   - User model                  │
│   - DTOs                        │
│   - Domain logic                │
└─────────────────────────────────┘
```

### JWT Authentication Flow

```
1. User submits credentials
   └─► Handler receives request

2. Handler calls use case
   └─► Use case validates credentials
   └─► Use case hashes password check
   └─► Use case generates JWT token

3. Use case returns token
   └─► Handler returns response with token

4. Client sends requests with token
   └─► Middleware validates JWT
   └─► Middleware extracts user_id
   └─► Route handler processes request
```

### Kafka Event Publishing (Scaffolded)

```
User Registration Flow:
  1. RegisterUser use case completes
  2. Publishes user.registered event to Kafka
  3. Event contains: user_id, email, timestamp
  4. Other services can subscribe to event

Future Consumers:
  - Email Service (send welcome email)
  - Analytics Service (track signups)
  - Notification Service (broadcast event)
```

---

## 📈 Monitoring

### Health Checks

```bash
# UserService health
curl http://localhost:5001/health

# Response:
# {"status":"ok"}
```

### Grafana Dashboards

1. Open http://localhost:3000 (admin/admin)
2. View pre-configured dashboards:
   - Kafka cluster metrics
   - Database performance
   - Service health status

### Jaeger Tracing

1. Open http://localhost:16686
2. Select service: "user-service"
3. View request traces and performance

### Prometheus Metrics

1. Open http://localhost:9090
2. Query examples:
   - `rate(http_requests_total[5m])` - Request rate
   - `histogram_quantile(0.95, http_request_duration_seconds_bucket)` - P95 latency

---

## 🐛 Troubleshooting

### Port Already in Use

```bash
# Kill process using port 5001
kill -9 $(lsof -t -i :5001)

# Or use different port
USER_SERVICE_PORT=5002 go run cmd/main.go
```

### Database Connection Error

```bash
# Check if PostgreSQL is running
docker-compose ps | grep postgres-user

# Verify connection string
echo $DATABASE_URL_USER

# Test connection manually
psql $DATABASE_URL_USER -c "SELECT 1"
```

### Kafka Connection Issues

```bash
# Check Kafka brokers
docker-compose exec kafka-1 kafka-broker-api-versions.sh --bootstrap-server kafka-1:9092

# List topics
docker-compose exec kafka-1 kafka-topics --bootstrap-server kafka-1:9092 --list
```

### JWT Token Validation Fails

```bash
# Check JWT_SECRET is set
echo $JWT_SECRET

# Verify token format
# Token should be: "Bearer {token}"

# Test with valid credentials
curl -X POST http://localhost:5001/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}'
```

---

## 🚀 Next Steps

### Short Term (This Week)

1. ✅ Phase 1: Infrastructure complete
2. ✅ Phase 2: UserService complete
3. 🚧 Phase 2b: CartService implementation
4. 🚧 Phase 2c: PaymentService implementation

### Medium Term (This Month)

1. ProductService with search
2. OrderService with Saga pattern
3. Comprehensive testing (unit + integration)
4. API Gateway implementation

### Long Term (Q1 2026)

1. Kubernetes deployment
2. Advanced monitoring & alerting
3. Load testing & optimization
4. Multi-region deployment

---

## 📚 Documentation

- **Phase 1 Quick Start**: `phase-1-foundation/PHASE1_QUICK_START.md`
- **Phase 1 Checklist**: `phase-1-foundation/PHASE1_CHECKLIST.md`
- **Troubleshooting**: `phase-1-foundation/HELP.md`
- **Tech Decisions**: See architecture section above

---

## 🔐 Security

### JWT Configuration

- **Algorithm**: HS256
- **Expiry**: 24 hours
- **Secret**: Generated per environment (min 32 characters)
- **Issuer**: User credentials

### Password Security

- **Hashing**: bcrypt (cost: 10)
- **Validation**: Email unique, password confirmed

### Production Checklist

- [ ] JWT_SECRET set to strong random value
- [ ] DATABASE_URL uses TLS (`?sslmode=require`)
- [ ] ENVIRONMENT set to "production"
- [ ] All container secrets in .env (not git)
- [ ] CORS configured for frontend domain
- [ ] Rate limiting enabled
- [ ] Security headers configured

---

## 🤝 Contributing

To add a new service or feature:

1. Create directory: `src/new-service/`
2. Follow structure:
   ```
   new-service/
   ├── cmd/main.go
   ├── internal/
   │   ├── domain/
   │   ├── repository/
   │   ├── usecase/
   │   ├── handler/
   │   ├── middleware/
   │   └── kafka/
   ├── migrations/
   ├── config/
   ├── go.mod
   └── Dockerfile
   ```
3. Implement clean architecture
4. Write tests (>80% coverage)
5. Update README.md
6. Create pull request

---

## 📄 License

MIT License - see LICENSE file

---

## 📞 Support

- **Issues**: GitHub Issues
- **Questions**: GitHub Discussions
- **Security**: Email privately (do NOT open public issue)

---

<div align="center">

### ⭐ Star this repo if you find it useful!

**Made with ❤️ using Go & Kafka**

Built with clean architecture, event-driven design, and production-ready patterns.

[⬆ Back to Top](#-e-commerce-platform---golang--kafka-migration)

</div>
