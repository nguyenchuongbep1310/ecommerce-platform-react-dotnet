---
description: how to deploy the application for free
---

# Deploying for Free (Hybrid Strategy)

Deploying a complex microservices architecture (6+ containers, Postgres, RabbitMQ) completely for free on the cloud is very difficult due to resource limits on free tiers.

The best strategy for a "Free Demo" is:

1.  **Backend**: Run it locally on your computer and tunnel it to the internet using **Ngrok**.
2.  **Frontend**: Deploy it to **Vercel** (which is excellent and free).

## Step 1: Expose Backend with Ngrok

1.  **Install Ngrok**:
    - Sign up at [ngrok.com](https://ngrok.com).
    - Install via Homebrew: `brew install ngrok/ngrok/ngrok`
    - Connect your account: `ngrok config add-authtoken <YOUR_TOKEN>`
2.  **Start the Tunnel**:
    - Ensure your docker backend is running (`docker-compose up`).
    - Run: `ngrok http 80`
    - Copy the "Forwarding" URL (e.g., `https://a1b2-c3d4.ngrok-free.app`).

## Step 2: Deploy Frontend to Vercel

1.  **Push Code to GitHub**:
    - Ensure your project is pushed to a public GitHub repository.
2.  **Deploy on Vercel**:
    - Go to [Vercel](https://vercel.com) and "Add New Project".
    - Import your GitHub repo.
    - Select `src/WebClient` as the **Root Directory**.
    - **Environment Variables**:
      - Name: `VITE_API_BASE_URL`
      - Value: `<YOUR_NGROK_URL>` (e.g., `https://a1b2-c3d4.ngrok-free.app`)
    - Click **Deploy**.

## Step 3: Configure CORS (Important!)

Since your Frontend (Vercel) domain will be different from `localhost`, you need to update the API Gateway's CORS settings to allow the Vercel domain.

1.  Open `src/ApiGateway/ApiGateway/Program.cs`.
2.  Update the CORS policy:
    ```csharp
    .WithOrigins("http://localhost:5173", "https://<your-vercel-app>.vercel.app")
    ```
3.  Rebuild the backend: `docker-compose up -d --build apigateway`.

Now anyone on the internet can visit your Vercel URL and interact with your backend running on your computer!
