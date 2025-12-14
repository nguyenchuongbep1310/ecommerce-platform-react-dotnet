---
description: how to run the application locally
---

# How to Run Locally

You need to run both the backend (Microservices) and the frontend (React).

## 1. Start the Backend

The backend consists of several services (Identity, Product, Cart, Order, Payment) and infrastructure (Postgres, RabbitMQ, Consul).

1.  Open a terminal in the project root.
2.  Navigate to the `src` folder:
    ```bash
    cd src
    ```
3.  Stop any manually running services (like `dotnet run`).
4.  Start everything using Docker Compose:
    ```bash
    // turbo
    docker-compose up --build
    ```
    _Wait for all containers to start. It may take a minute or two for databases to initialize and migrations to run._
    _Verify that the API Gateway is running on `http://localhost:80`._

## 2. Start the Frontend

The frontend is a Vite + React app that connects to the backend via the API Gateway.

1.  Open a **new** terminal window.
2.  Navigate to the WebClient folder:
    ```bash
    cd src/WebClient
    ```
3.  Install dependencies (if you haven't already):
    ```bash
    npm install
    ```
4.  Start the development server:
    ```bash
    npm run dev
    ```
5.  Open the link shown (usually `http://localhost:5173`).

## 3. Verify the Flow

1.  **Browse**: On the Homepage, you should see products (iPhone, Samsung, etc.). If you see them, the connection to `ProductService` is working.
2.  **Register**: Click **Login** -> **Sign Up**. Create an account.
3.  **Login**: Log in with your new account. Even if you refresh, you should stay logged in.
4.  **Cart**: Add items to the cart. Go to the Cart page. The items are now saved in the `CartService` database.
5.  **Checkout**: Click **Checkout**. This will trigger the `OrderService` to create an order, reduce stock, and clear your cart.

## Troubleshooting

- **API Connection Error**: If you see "Network Error" or demo data, check if `apigateway` container is running (`docker ps`).
- **Database Error**: If services crash, ensure Docker has enough memory allocated.
- **Ports**: ensure ports 80, 5001-5006, 5432-5436 are not occupied by other apps on your machine.
