# Integration Tests

This project contains End-to-End (E2E) integration tests for the specialized microservices flow.

## Requirements

- The Docker Compose environment must be running (`docker-compose up -d`).
- The services must be accessible locally on their mapped ports (ApiGateway on 80, Seq on 5341).
- The `ProductCatalogService` must have seeded data (or at least one product).

## Running Tests

Run the following command from the root of the repository:

```bash
dotnet test src/Tests/IntegrationTests
```

## Scenarios

- **CoreTransactionFlowTests**: Covers the full flow from User Registration -> Product Selection -> Cart -> Order -> Notification Verification (via Seq).
