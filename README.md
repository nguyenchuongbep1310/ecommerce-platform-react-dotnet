Follow up: https://roadmap.sh/projects/scalable-ecommerce-platform

# 🚀 E-commerce Microservices Platform

This repository contains a full-stack, distributed e-commerce platform built using **.NET 10 Microservices** orchestrated via **Docker Compose**. The architecture is designed for scalability, using Service Discovery (Consul) and Asynchronous Communication (RabbitMQ).

## 📺 Live Demo

Try out the application here:
👉 **[View Live Demo](https://ecommerce-platform-react-dotnet.vercel.app)**

---

## 1. 🏗️ Architecture Overview

The system is composed of **7 Core Microservices** and **5 Infrastructure Services**, unified by an API Gateway.

### Key Components

| Component                 | Responsibility                               | Technology                                | Port   |
| :------------------------ | :------------------------------------------- | :---------------------------------------- | :----- |
| **API Gateway**           | External Entry Point, Routing, Auth          | Ocelot (**.NET 10**)                      | `80`   |
| **User Service**          | User Identity, Authentication (JWT)          | **.NET 10**, PostgreSQL, ASP.NET Identity | `5001` |
| **Product Service**       | Product Catalog, Inventory                   | **.NET 10**, PostgreSQL                   | `5002` |
| **Shopping Cart Service** | Cart State Management                        | **.NET 10**, PostgreSQL                   | `5003` |
| **Order Service**         | Transaction Orchestration, Order Record      | **.NET 10**, PostgreSQL                   | `5004` |
| **Payment Service**       | Mock External Payment Processor              | **.NET 10**                               | `5005` |
| **Notification Service**  | Asynchronous Event Consumer (Email/SMS Mock) | **.NET 10**, MassTransit                  | `5006` |

### Infrastructure

| Service        | Purpose              | Access                                 |
| :------------- | :------------------- | :------------------------------------- |
| **Consul**     | Service Discovery    | `http://localhost:8500`                |
| **RabbitMQ**   | Message Broker       | `http://localhost:15672` (guest/guest) |
| **Seq**        | Centralized Logging  | `http://localhost:5341`                |
| **Prometheus** | Metrics Collection   | `http://localhost:9090`                |
| **Grafana**    | Monitoring Dashboard | `http://localhost:3000` (admin/admin)  |
| **Jaeger**     | Distributed Tracing  | `http://localhost:16686`               |

### Communication Flow (The Core Transaction)

The system uses a mix of synchronous HTTP calls (via **Consul** lookup) and asynchronous event messaging (via **RabbitMQ**).

1.  **Synchronous (HTTP):** External Client $\rightarrow$ **API Gateway** $\rightarrow$ **Consul** $\rightarrow$ **Order Service** $\rightarrow$ **Cart, Product, Payment** Services.
2.  **Asynchronous (Event-Driven):** **Order Service** (Publishes `OrderPlacedEvent`) $\rightarrow$ **RabbitMQ** $\rightarrow$ **Notification Service** (Consumes event).
3.  **Real-Time (SignalR):** **Notification Service** $\rightarrow$ **WebClient** (Push Notification).

![Architecture Diagram](docs/images/generated_architecture.png)

---

## 2. ⚙️ Getting Started (Local Setup)

### Prerequisites

- Docker Desktop (with Compose V2)
- **.NET 10 SDK**
- A REST client (e.g., Postman, cURL)

### Installation

1.  **Clone the repository:**

    ```bash
    git clone [your-repo-link]
    cd [repo-name]
    ```

2.  **Build and Run All Services:**
    The single `docker compose` command builds the microservices, initializes the PostgreSQL databases, and launches all infrastructure components.

    ```bash
    docker compose up --build -d
    ```

    _(This may take a few minutes on the first run as images are built and databases are initialized.)_

3.  **Verify Startup:**
    Check that all containers are running (`Up` status):
    ```bash
    docker ps
    ```

---

## 3. 🧪 Testing and Verification

### Quick Health Check

Verify the API Gateway, Consul, and Seq are accessible:

- **API Gateway:** `http://localhost:8080`
- **Consul UI:** `http://localhost:8500`
- **Seq UI:** `http://localhost:5341`
- **Jaeger UI:** `http://localhost:16686`

### End-to-End Manual Test Flow

This sequence verifies the entire platform, including database persistence, inventory reduction, payment processing, and asynchronous notification.

| Step               | Method | URL (Gateway: 8080)  | Body / Notes                                                                                         |
| :----------------- | :----- | :------------------- | :--------------------------------------------------------------------------------------------------- |
| **1. Register**    | `POST` | `/api/auth/register` | JSON: `{"email": "user@test.com", "password": "Password123!", "fullName": "Test User"}`              |
| **2. Login**       | `POST` | `/api/auth/login`    | JSON: `{"email": "user@test.com", "password": "Password123!"}`. **Copy the JWT Token**.              |
| **3. Connect WS**  | `WS`   | `/notificationHub`   | Connect via SignalR Client with Bearer Token. Join Group: `<UserId>`.                                |
| **4. Load Cart**   | `POST` | `/api/cart/add`      | Header `Authorization: Bearer <TOKEN>`<br>JSON: `{"userId": "<UID>", "productId": 1, "quantity": 1}` |
| **5. Place Order** | `POST` | `/api/orders/place`  | Header `Authorization: Bearer <TOKEN>`<br>JSON: `{"paymentMethodId": "pm_card_visa"}`.               |
| **6. Verify**      | -      | -                    | Check SignalR for "Order Placed" alert. Check Jaeger for trace.                                      |

---

## 4. 📊 Performance & Load Testing

This project includes a **k6** load testing script to verify the system's performance under load.

### Running the Load Test

1. Ensure all services are running (`docker compose up -d`).
2. Run the load test using Docker:

```bash
docker run --rm -i \
  --add-host=host.docker.internal:host-gateway \
  -v $(pwd)/tests/k6:/scripts \
  grafana/k6 run /scripts/load_test.js
```

### Test Scenario

- **Ramp-up:** 20 users over 30s
- **Steady State:** 20 users for 1m
- **Target:** API Gateway Product Endpoint (`/api/products`)

---

## 5. 🚀 Kubernetes Deployment

A full Helm chart is available in `deploy/k8s/helm`.

### Deploying with Helm

1. Ensure you have a Kubernetes cluster (Minikube, Docker Desktop K8s, or Cloud).
2. Install the chart:
   ```bash
   helm install ecommerce-platform ./deploy/k8s/helm
   ```
3. Port-forward the Gateway:
   ```bash
   kubectl port-forward svc/apigateway 8080:8080
   ```

For more details, see [DEPLOYMENT.md](DEPLOYMENT.md).

---

## 6. 🔧 Troubleshooting

- **Service Connectivity Issues:** Check `Consul` (http://localhost:8500) to ensure all services are registered and passing health checks.
- **Database Errors:** Ensure the PostgreSQL containers are healthy. The logs in `Seq` (http://localhost:5341) will provide detailed error traces.
- **SignalR Connection**: If connecting via Gateway, ensure Sticky Sessions are enabled or use WebSockets transport directly to avoid negotiation issues.
