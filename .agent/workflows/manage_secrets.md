---
description: how to manage secrets and environment variables
---

# Managing Secrets

This project uses environment variables to manage sensitive information like database passwords and API keys.

## Local Development (Docker)

To run the application locally with Docker Compose, you must have a `.env` file in the `src` directory: `src/.env`.

This file is git-ignored and should NOT be committed to version control.

### Setup

1. Copy the example below to `src/.env`:

```bash
# Database Configuration
POSTGRES_USER=user
POSTGRES_PASSWORD=password

# JWT Configuration
JWT_SECRET=your_strong_secret_key_here
JWT_ISSUER=ECommerceUserAuthService
JWT_AUDIENCE=ECommerceClients
JWT_TOKEN_LIFETIME=60

# Payment Configuration
STRIPE_SECRET_KEY=sk_test_placeholder
```

2. Run `docker-compose up` as usual. The `docker-compose.yml` is configured to read from this file.

## Production

When deploying to production (e.g., Kubernetes, Vercel, AWS):

1. **Do not** upload the `.env` file.
2. Set the environment variables in your deployment platform's secrets manager.
3. Ensure the variable names match those in `docker-compose.yml` or the service configuration.

## Adding New Secrets

1. Add the variable to `src/.env`.
2. Update `docker-compose.yml` to inject the variable into the relevant service(s).
3. Update `appsettings.json` (optional, as a fallback with empty string) to document the configuration structure.
