# 🚀 E-Commerce Platform Deployment Guide

This guide covers how to deploy the E-Commerce Microservices Platform using **Docker Compose** (for local dev/testing) and **Kubernetes/Helm** (for production-like environments).

---

## 1. 🐳 Docker Compose (Local Development)

The simplest way to run the entire stack locally.

### Steps

1.  **Navigate to the source directory:**

    ```bash
    cd src
    ```

2.  **Run Services:**

    ```bash
    docker-compose up -d --build
    ```

3.  **Access:**
    - Web Client: http://localhost:5173 (Port 80 mapped in some setups)
    - API Gateway: http://localhost:8080

### Cleanup

To stop and remove containers/networks:

```bash
docker-compose down
```

To also remove database volumes (fresh start):

```bash
docker-compose down -v
```

---

## 2. ☸️ Kubernetes via Helm (Production Ready)

We provide a **Helm Chart** to strictly manage deployments, services, and configuration.

### Prerequisites

- A running Kubernetes cluster (Minikube, Docker Desktop, AKS, EKS, etc.)
- `kubectl` installed and configured.
- `helm` installed.

### Architecture

The chart automatically deploys:

- **Deployments**: One for each microservice (User, Product, Order, Cart, Payment, Notification, Gateway, WebClient).
- **Infrastructure**: Postgres (User, Product, Cart, Order), RabbitMQ, Redis.
- **Services**: ClusterIP for internal comms, LoadBalancer (or NodePort) for Gateway/WebClient.

### Installation

1.  **Navigate to Chart Directory:**

    ```bash
    cd deploy/k8s/helm
    ```

2.  **Install/Upgrade Chart:**

    ```bash
    helm upgrade --install ecommerce-platform . -n ecommerce --create-namespace
    ```

3.  **Check Status:**
    ```bash
    kubectl get pods -n ecommerce
    ```
    Wait until all pods are `Running` and `Ready`.

### Accessing the Application

If you are on **Docker Desktop (Mac/Windows)** or a Cloud Provider with LoadBalancers:

- **Web Client**: `http://localhost` (via LoadBalancer service on port 80)
- **API Gateway**: `http://localhost:8080`

If you are on **Minikube**:

```bash
minikube service apigateway -n ecommerce --url
```

### Configuration

You can customize the deployment by editing `values.yaml`.

**Key overrides:**

- `image.tag`: Update this to deploy specific versions of your containers.
- `infra.enabled`: Set false if you want to use managed cloud services (e.g., AWS RDS, Azure Service Bus) instead of in-cluster containers.

```yaml
# Example: Disable local Postgres to use RDS
infra:
  postgres:
    enabled: false
```

---

## 3. ☁️ CI/CD Pipeline (GitHub Actions)

The repository includes a `.github/workflows/dotnet.yml` file that:

1.  Builds the .NET projects.
2.  Runs Unit/Integration Tests.
3.  Builds Docker Images.
4.  Pushes images to Docker Hub (requires secrets `DOCKER_USERNAME` and `DOCKER_PASSWORD`).

To automate deployment, you would extend this workflow to run `helm upgrade` after a successful build.
