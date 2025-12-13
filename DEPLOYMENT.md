# Deployment Guide

This project is set up as a set of Dockerized microservices. To deploy this application, you generally follow two main phases:

1. **Publish**: Build Docker images and push them to a container registry (e.g., Docker Hub, GitHub Container Registry, Azure Container Registry).
2. **Deploy**: Pull those images onto a hosting environment (e.g., Azure Container Apps, AWS ECS, Kubernetes, or a generic Linux VPS).

## Prerequisites

- A **GitHub** repository (which you have).
- A **Docker Hub** account (or another registry).
- A **Cloud Provider** account (Azure, AWS, DigitalOcean, etc.).

---

## Phase 1: Publishing Images (CI)

To automate the publishing of your images, you need to configure GitHub Actions secrets.

1. Go to your GitHub Repository -> **Settings** -> **Secrets and variables** -> **Actions**.
2. Add the following repository secrets:
   - `DOCKER_USERNAME`: Your Docker Hub username.
   - `DOCKER_PASSWORD`: Your Docker Hub access token (recommended) or password.

### Workflow Update

The `ci-cd.yml` needs to be updated to login to Docker Hub and push images. (See the "Proposed CI/CD Update" section below).

---

## Phase 2: Deployment Strategies (CD)

### Option A: Azure Container Apps (Recommended for .NET Microservices)

Azure Container Apps is serverless and supports .NET microservices and Dapr well.

1. Create an Azure Container Apps Environment.
2. Update the CI/CD pipeline to use the `azure/container-apps-deploy-action`.
   - Requires `AZURE_CREDENTIALS` secret.

### Option B: generic Linux VPS (Docker Compose)

Simplest for small scale/portfolio projects.

1. Provision a Linux server (Ubuntu).
2. Install Docker & Docker Compose.
3. **Deployment Strategy**:
   - Copy `docker-compose.yml` and `prometheus.yml` to the server.
   - Run `docker compose up -d --pull always`.

### Option C: Kubernetes (AKS/EKS)

Best for scalability.

1. Provision a Kubernetes Cluster.
2. Create Helm charts or Kubernetes manifest files (`.yaml`) for each service.
3. Update CI/CD to use `kubectl` or Helm to deploy.

---

## Recommended Next Steps

1. **Decide on a Hosting Provider**.
2. **Update `ci-cd.yml`** to push images to Docker Hub.
3. **Configure the Deploy Job** based on your chosen provider.

## Example: Updating `ci-cd.yml` to Push to Docker Hub

I can update your workflow to automatically build and push these services:

- `userservice`
- `productcatalogservice`
- `shoppingcartservice`
- `orderservice`
- `paymentservice`
- `notificationservice`
- `apigateway`
