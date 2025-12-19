# 🚀 E-commerce Microservices Platform

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19.2-61DAFB?logo=react)](https://react.dev/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> A production-ready, full-stack distributed e-commerce platform built with **.NET 10 Microservices**, **React**, and modern cloud-native patterns including **CQRS**, **Event-Driven Architecture**, and **Service Discovery**.

**Follow-up Project**: [roadmap.sh/projects/scalable-ecommerce-platform](https://roadmap.sh/projects/scalable-ecommerce-platform)

## 📺 Live Demo

Try out the application here:  
👉 **[View Live Demo](https://ecommerce-platform-react-dotnet.vercel.app)**

---

## 📑 Table of Contents

- [Features](#-features)
- [Architecture Overview](#️-architecture-overview)
- [Technology Stack](#-technology-stack)
- [Getting Started](#️-getting-started)
- [Architecture Patterns](#-architecture-patterns)
- [API Documentation](#-api-documentation)
- [Testing](#-testing)
- [Deployment](#-deployment)
- [Monitoring & Observability](#-monitoring--observability)
- [Troubleshooting](#-troubleshooting)
- [Contributing](#-contributing)
- [License](#-license)

---

## ✨ Features

### Core E-commerce Functionality

- ✅ **User Management** - Registration, authentication with JWT tokens
- ✅ **Product Catalog** - Browse, search, filter products by category/price
- ✅ **Shopping Cart** - Add/remove items, persistent cart state
- ✅ **Order Processing** - Place orders, payment integration (Stripe)
- ✅ **Real-time Notifications** - SignalR for instant order updates
- ✅ **Inventory Management** - Stock tracking and reduction

### Architecture & Patterns

- ✅ **CQRS with MediatR** - Command Query Responsibility Segregation
- ✅ **Event-Driven Architecture** - Asynchronous messaging with RabbitMQ
- ✅ **Service Discovery** - Dynamic service registration with Consul
- ✅ **API Gateway Pattern** - Unified entry point with Ocelot
- ✅ **Saga Pattern** - Distributed transaction orchestration
- ✅ **Outbox Pattern** - Reliable event publishing
- ✅ **Resilience Patterns** - Retry, circuit breaker, timeout with Polly

### DevOps & Observability

- ✅ **Containerization** - Docker & Docker Compose
- ✅ **Kubernetes Ready** - Helm charts for K8s deployment
- ✅ **Distributed Tracing** - OpenTelemetry with Jaeger
- ✅ **Centralized Logging** - Structured logging with Seq
- ✅ **Metrics & Monitoring** - Prometheus & Grafana dashboards
- ✅ **Health Checks** - Service health monitoring
- ✅ **CI/CD Pipeline** - GitHub Actions for automated builds

### Frontend

- ✅ **Modern React UI** - React 19 with Vite
- ✅ **Responsive Design** - Mobile-friendly interface
- ✅ **Real-time Updates** - SignalR integration
- ✅ **State Management** - React Context API
- ✅ **Client-side Routing** - React Router v7

---

## 🏗️ Architecture Overview

The system is composed of **7 Core Microservices** and **6 Infrastructure Services**, unified by an API Gateway.

### System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Gateway (Ocelot)                     │
│                    Port: 8080 | Auth, Routing                    │
└────────────┬────────────────────────────────────────────────────┘
             │
    ┌────────┴────────┐
    │                 │
    ▼                 ▼
┌─────────┐      ┌─────────────┐
│  User   │      │   Product   │
│ Service │      │   Service   │
│  :5001  │      │    :5002    │
└─────────┘      └─────────────┘
                        │
    ┌───────────────────┼───────────────────┐
    │                   │                   │
    ▼                   ▼                   ▼
┌─────────┐      ┌─────────┐      ┌──────────────┐
│  Cart   │      │  Order  │      │ Notification │
│ Service │      │ Service │      │   Service    │
│  :5003  │      │  :5004  │      │    :5006     │
└─────────┘      └─────────┘      └──────────────┘
                        │
                        ▼
                 ┌─────────┐
                 │ Payment │
                 │ Service │
                 │  :5005  │
                 └─────────┘
```

### Key Components

| Component                 | Responsibility                           | Technology                                | Port   |
| :------------------------ | :--------------------------------------- | :---------------------------------------- | :----- |
| **API Gateway**           | External Entry Point, Routing, Auth      | Ocelot (**.NET 10**)                      | `8080` |
| **User Service**          | User Identity, Authentication (JWT)      | **.NET 10**, PostgreSQL, ASP.NET Identity | `5001` |
| **Product Service**       | Product Catalog, Inventory, CQRS         | **.NET 10**, PostgreSQL, Redis            | `5002` |
| **Shopping Cart Service** | Cart State Management                    | **.NET 10**, PostgreSQL                   | `5003` |
| **Order Service**         | Transaction Orchestration, Saga Pattern  | **.NET 10**, PostgreSQL, MassTransit      | `5004` |
| **Payment Service**       | Payment Processing (Stripe Integration)  | **.NET 10**                               | `5005` |
| **Notification Service**  | Asynchronous Event Consumer, SignalR Hub | **.NET 10**, MassTransit, SignalR         | `5006` |
| **Web Client**            | React Frontend                           | React 19, Vite, SignalR Client            | `80`   |

### Infrastructure Services

| Service        | Purpose                 | Access                                 |
| :------------- | :---------------------- | :------------------------------------- |
| **Consul**     | Service Discovery       | `http://localhost:8500`                |
| **RabbitMQ**   | Message Broker          | `http://localhost:15672` (guest/guest) |
| **Redis**      | Distributed Cache       | `localhost:6379`                       |
| **Seq**        | Centralized Logging     | `http://localhost:5341`                |
| **Prometheus** | Metrics Collection      | `http://localhost:9090`                |
| **Grafana**    | Monitoring Dashboard    | `http://localhost:3000` (admin/admin)  |
| **Jaeger**     | Distributed Tracing     | `http://localhost:16686`               |
| **PostgreSQL** | Databases (4 instances) | Ports: 5432, 5434, 5435, 5436          |

### Communication Patterns

The system implements multiple communication patterns for different scenarios:

1. **Synchronous (HTTP/REST):**

   ```
   Client → API Gateway → Consul (Service Discovery) → Microservice
   ```

2. **Asynchronous (Event-Driven):**

   ```
   Order Service → RabbitMQ → Notification Service
   ```

3. **Real-Time (WebSocket):**

   ```
   Notification Service → SignalR Hub → Web Client
   ```

4. **CQRS Pattern:**
   ```
   Command → MediatR → Pipeline Behaviors → Command Handler → Database
   Query → MediatR → Pipeline Behaviors → Query Handler → Cache/Database → DTO
   ```

---

## 🛠️ Technology Stack

### Backend

- **.NET 10** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 10** - ORM
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **MassTransit** - Message bus abstraction
- **Polly** - Resilience and transient-fault-handling
- **Ocelot** - API Gateway
- **SignalR** - Real-time communication

### Frontend

- **React 19** - UI library
- **Vite** - Build tool
- **React Router v7** - Client-side routing
- **Axios** - HTTP client
- **SignalR Client** - Real-time updates

### Databases & Caching

- **PostgreSQL 15** - Primary database
- **Redis** - Distributed cache

### Message Broker

- **RabbitMQ** - Event streaming

### Service Discovery

- **Consul** - Service registry

### Observability

- **OpenTelemetry** - Distributed tracing
- **Jaeger** - Trace visualization
- **Seq** - Structured logging
- **Prometheus** - Metrics collection
- **Grafana** - Metrics visualization

### DevOps

- **Docker** - Containerization
- **Docker Compose** - Multi-container orchestration
- **Kubernetes** - Container orchestration (production)
- **Helm** - Kubernetes package manager
- **GitHub Actions** - CI/CD pipeline

---

## ⚙️ Getting Started

### Prerequisites

- **Docker Desktop** (with Compose V2) - [Download](https://www.docker.com/products/docker-desktop)
- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 18+** (for local frontend development) - [Download](https://nodejs.org/)
- **Git** - [Download](https://git-scm.com/)

### Quick Start

1. **Clone the repository:**

   ```bash
   git clone https://github.com/yourusername/ecommerce-platform.git
   cd ecommerce-platform
   ```

2. **Set up environment variables:**

   ```bash
   cd src
   cp .env.example .env
   # Edit .env with your configuration
   ```

3. **Build and run all services:**

   ```bash
   docker compose up --build -d
   ```

   This command will:

   - Build all microservices
   - Initialize PostgreSQL databases
   - Start all infrastructure services
   - Launch the React frontend

   ⏱️ _First run may take 5-10 minutes as images are built and databases are initialized._

4. **Verify all services are running:**

   ```bash
   docker compose ps
   ```

   All services should show `Up` status.

5. **Access the application:**

   - **Frontend:** http://localhost:80
   - **API Gateway:** http://localhost:8080
   - **Consul UI:** http://localhost:8500
   - **RabbitMQ Management:** http://localhost:15672 (guest/guest)
   - **Seq Logs:** http://localhost:5341
   - **Jaeger Tracing:** http://localhost:16686
   - **Grafana Dashboards:** http://localhost:3000 (admin/admin)
   - **Prometheus:** http://localhost:9090

### Local Development (Without Docker)

For development of individual services:

```bash
# Start infrastructure only
docker compose up -d consul rabbitmq redis seq prometheus grafana jaeger db_user db_product db_cart db_order

# Run a service locally
cd src/ProductCatalogService/ProductCatalogService
dotnet run

# Run frontend locally
cd src/WebClient
npm install
npm run dev
```

---

## 🎯 Architecture Patterns

### CQRS with MediatR

The platform implements **Command Query Responsibility Segregation (CQRS)** using MediatR to separate read and write operations.

#### Benefits:

- ✅ **Separation of Concerns** - Clear distinction between reads and writes
- ✅ **Scalability** - Read and write operations can scale independently
- ✅ **Performance** - Queries can use caching, commands ensure consistency
- ✅ **Maintainability** - Single Responsibility Principle per handler
- ✅ **Testability** - Handlers are isolated and easy to test

#### Implementation Example:

**Command (Write Operation):**

```csharp
public record ReduceStockCommand(int ProductId, int Quantity) : IRequest<bool>;

public class ReduceStockCommandHandler : IRequestHandler<ReduceStockCommand, bool>
{
    public async Task<bool> Handle(ReduceStockCommand request, CancellationToken cancellationToken)
    {
        // Business logic to reduce stock
        // Validation, database write, event publishing
    }
}
```

**Query (Read Operation):**

```csharp
public record GetProductsQuery(string? Category) : IRequest<List<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Check cache first
        // Query database if cache miss
        // Return DTOs
    }
}
```

**Pipeline Behaviors:**

- **LoggingBehavior** - Logs all requests with execution time
- **ValidationBehavior** - Validates commands using FluentValidation
- **PerformanceBehavior** - Monitors slow operations (>500ms)

#### Services with CQRS:

- ✅ **ProductCatalogService** - Fully implemented
- 🔄 **OrderService** - Partially implemented
- ⏳ **ShoppingCartService** - Planned
- ⏳ **UserService** - Planned

### Event-Driven Architecture

Services communicate asynchronously using **RabbitMQ** and **MassTransit**.

**Example Flow:**

```
Order Placed → OrderPlacedEvent → RabbitMQ → Notification Service → Email/SignalR
```

**Benefits:**

- Loose coupling between services
- Resilience to service failures
- Scalability through async processing
- Event sourcing capability

### Saga Pattern

Complex distributed transactions are managed using **MassTransit Sagas**.

**Order Saga States:**

1. Order Created
2. Payment Processed
3. Inventory Reserved
4. Order Confirmed
5. Notification Sent

### Resilience Patterns (Polly)

All HTTP calls implement resilience patterns:

- **Retry** - Automatic retry with exponential backoff
- **Circuit Breaker** - Prevent cascading failures
- **Timeout** - Prevent hanging requests
- **Fallback** - Graceful degradation

---

## 📚 API Documentation

### Authentication Endpoints

#### Register User

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!",
  "fullName": "John Doe"
}
```

#### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "123",
  "email": "user@example.com"
}
```

### Product Endpoints

#### Get All Products

```http
GET /api/products?search=laptop&category=Electronics&minPrice=100&maxPrice=2000
Authorization: Bearer {token}
```

#### Get Product by ID

```http
GET /api/products/{id}
```

### Cart Endpoints

#### Add to Cart

```http
POST /api/cart/add
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": "123",
  "productId": 1,
  "quantity": 2
}
```

#### Get Cart

```http
GET /api/cart/{userId}
Authorization: Bearer {token}
```

### Order Endpoints

#### Place Order

```http
POST /api/orders/place
Authorization: Bearer {token}
Content-Type: application/json

{
  "paymentMethodId": "pm_card_visa"
}
```

#### Get Order History

```http
GET /api/orders/history
Authorization: Bearer {token}
```

---

## 🧪 Testing

### End-to-End Test Flow

This sequence verifies the entire platform:

| Step               | Method | Endpoint             | Description                              |
| :----------------- | :----- | :------------------- | :--------------------------------------- |
| **1. Register**    | POST   | `/api/auth/register` | Create a new user account                |
| **2. Login**       | POST   | `/api/auth/login`    | Get JWT token                            |
| **3. Connect WS**  | WS     | `/notificationHub`   | Connect to SignalR for real-time updates |
| **4. Browse**      | GET    | `/api/products`      | View available products                  |
| **5. Add to Cart** | POST   | `/api/cart/add`      | Add products to shopping cart            |
| **6. Place Order** | POST   | `/api/orders/place`  | Complete purchase                        |
| **7. Verify**      | WS     | SignalR              | Receive order confirmation notification  |

### Load Testing with k6

The project includes k6 load testing scripts:

```bash
docker run --rm -i \
  --add-host=host.docker.internal:host-gateway \
  -v $(pwd)/tests/k6:/scripts \
  grafana/k6 run /scripts/load_test.js
```

**Test Scenario:**

- Ramp-up: 20 virtual users over 30s
- Steady state: 20 users for 1 minute
- Target: Product catalog endpoint

### Unit Testing

```bash
# Run all tests
dotnet test

# Run tests for specific service
dotnet test src/ProductCatalogService/ProductCatalogService.Tests
```

---

## 🚀 Deployment

### Docker Compose (Development/Testing)

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f

# Stop all services
docker compose down

# Remove volumes (fresh start)
docker compose down -v
```

### Kubernetes with Helm (Production)

1. **Ensure you have a Kubernetes cluster:**

   ```bash
   # For local testing
   minikube start
   # OR use Docker Desktop Kubernetes
   ```

2. **Install the Helm chart:**

   ```bash
   cd deploy/k8s/helm
   helm install ecommerce-platform . -n ecommerce --create-namespace
   ```

3. **Check deployment status:**

   ```bash
   kubectl get pods -n ecommerce
   kubectl get services -n ecommerce
   ```

4. **Access the application:**

   ```bash
   # Port-forward API Gateway
   kubectl port-forward svc/apigateway 8080:8080 -n ecommerce

   # Port-forward Web Client
   kubectl port-forward svc/webclient 80:80 -n ecommerce
   ```

5. **Upgrade deployment:**

   ```bash
   helm upgrade ecommerce-platform . -n ecommerce
   ```

6. **Uninstall:**
   ```bash
   helm uninstall ecommerce-platform -n ecommerce
   ```

### CI/CD Pipeline

The project includes GitHub Actions workflows for:

- ✅ Building .NET projects
- ✅ Running tests
- ✅ Building Docker images
- ✅ Pushing to Docker Hub
- ✅ Deploying to Vercel (frontend)

---

## 📊 Monitoring & Observability

### Distributed Tracing (Jaeger)

View end-to-end request traces across all microservices:

1. Open http://localhost:16686
2. Select a service (e.g., `OrderService`)
3. Click "Find Traces"
4. Explore the trace timeline

**What you can see:**

- Request flow across services
- Service dependencies
- Performance bottlenecks
- Error propagation

### Centralized Logging (Seq)

View structured logs from all services:

1. Open http://localhost:5341
2. Use filters: `@Level = 'Error'` or `@Service = 'ProductCatalogService'`
3. Analyze log patterns

**Log Levels:**

- `Information` - Normal operations
- `Warning` - Slow operations (>500ms)
- `Error` - Exceptions and failures

### Metrics & Dashboards (Grafana)

Pre-configured dashboards for:

- API Gateway metrics (requests/sec, latency, errors)
- Service health checks
- Database connection pools
- RabbitMQ queue depths

1. Open http://localhost:3000 (admin/admin)
2. Navigate to "Dashboards"
3. Select "E-commerce Platform Overview"

### Health Checks

All services expose health check endpoints:

```bash
# Check individual service
curl http://localhost:5002/health

# Check all services via Consul
open http://localhost:8500
```

---

## 🔧 Troubleshooting

### Common Issues

#### Services Not Starting

**Problem:** Containers fail to start or crash immediately

**Solution:**

```bash
# Check logs
docker compose logs [service-name]

# Common fixes:
# 1. Ensure ports are not in use
lsof -i :8080  # Check if port is occupied

# 2. Rebuild images
docker compose build --no-cache

# 3. Remove old volumes
docker compose down -v
docker compose up -d
```

#### Database Connection Errors

**Problem:** Services can't connect to PostgreSQL

**Solution:**

```bash
# Check database containers
docker compose ps | grep db_

# Restart databases
docker compose restart db_user db_product db_cart db_order

# Check connection string in .env file
cat src/.env
```

#### Service Discovery Issues

**Problem:** Services not registered in Consul

**Solution:**

1. Open Consul UI: http://localhost:8500
2. Check if service is registered
3. Verify health check is passing
4. Check service logs for registration errors

#### RabbitMQ Connection Failures

**Problem:** Services can't connect to RabbitMQ

**Solution:**

```bash
# Check RabbitMQ is running
docker compose ps rabbitmq

# Access RabbitMQ Management UI
open http://localhost:15672  # guest/guest

# Check connections and queues
# Restart RabbitMQ if needed
docker compose restart rabbitmq
```

#### SignalR Connection Issues

**Problem:** Real-time notifications not working

**Solution:**

- Ensure WebSocket transport is enabled
- Check CORS configuration in API Gateway
- Verify JWT token is valid
- Check browser console for errors

#### Frontend Not Loading

**Problem:** Web client shows blank page

**Solution:**

```bash
# Check WebClient logs
docker compose logs webclient

# Verify API Gateway is accessible
curl http://localhost:8080/health

# Check environment variables
docker compose exec webclient env | grep API
```

### Performance Issues

#### Slow API Responses

1. Check Jaeger for slow traces
2. Review Seq logs for slow operations (>500ms warnings)
3. Check database query performance
4. Verify Redis cache is working

#### High Memory Usage

```bash
# Check container resource usage
docker stats

# Limit container resources in docker-compose.yml
services:
  productcatalogservice:
    deploy:
      resources:
        limits:
          memory: 512M
```

---

## 🌐 Exposing to Vercel (Development)

To connect the local backend to the deployed Vercel frontend:

1. **Install ngrok:**

   ```bash
   brew install ngrok
   # OR
   npm install -g ngrok
   ```

2. **Start ngrok tunnel:**

   ```bash
   ngrok http 8080
   ```

3. **Update CORS in API Gateway:**

   Edit `src/ApiGateway/ApiGateway/Program.cs`:

   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("CorsPolicy", builder =>
       {
           builder
               .WithOrigins(
                   "http://localhost:5173",
                   "https://your-ngrok-url.ngrok.io",  // Add this
                   "https://ecommerce-platform-react-dotnet.vercel.app"
               )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
       });
   });
   ```

4. **Update Vercel environment variable:**
   - Go to Vercel project settings
   - Add `VITE_API_BASE_URL` = `https://your-ngrok-url.ngrok.io`
   - Redeploy

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation
- Ensure all tests pass
- Follow CQRS pattern for new endpoints

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- [roadmap.sh](https://roadmap.sh) - Project inspiration
- [MediatR](https://github.com/jbogard/MediatR) - CQRS implementation
- [MassTransit](https://masstransit.io/) - Message bus abstraction
- [Ocelot](https://github.com/ThreeMammals/Ocelot) - API Gateway

---

## 📞 Support

- 📧 Email: your.email@example.com
- 💬 Issues: [GitHub Issues](https://github.com/yourusername/ecommerce-platform/issues)
- 📖 Documentation: [Wiki](https://github.com/yourusername/ecommerce-platform/wiki)

---

**Built with ❤️ using .NET 10 and React**
