# Background Tasks and Scheduled Jobs

This document describes the background tasks and scheduled jobs implemented in the Product Catalog Service.

## Overview

The service implements two types of asynchronous task processing:

1. **Background Services** - Long-running tasks using `IHostedService`/`BackgroundService`
2. **Scheduled Jobs** - Recurring tasks using Hangfire

## Background Services

Background services run continuously in the background and are managed by the .NET hosting infrastructure.

### 1. Elasticsearch Sync Service

**Purpose**: Periodically synchronizes products from PostgreSQL to Elasticsearch to ensure search index consistency.

**Schedule**: Every 30 minutes

**Location**: `BackgroundServices/ElasticsearchSyncService.cs`

**Features**:

- Waits 1 minute after startup before first sync
- Bulk indexes all products to Elasticsearch
- Automatic retry with 5-minute delay on errors
- Comprehensive logging

**Configuration**:

```csharp
private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(30);
```

### 2. Cache Warming Service

**Purpose**: Proactively warms up Redis cache with frequently accessed data to improve performance.

**Schedule**: Every 6 hours

**Location**: `BackgroundServices/CacheWarmingService.cs`

**Cached Data**:

- Top 50 popular products (12-hour TTL)
- All product categories (24-hour TTL)
- Product count by category (12-hour TTL)
- In-stock products count (6-hour TTL)

**Configuration**:

```csharp
private readonly TimeSpan _warmupInterval = TimeSpan.FromHours(6);
```

### 3. Inventory Monitoring Service

**Purpose**: Monitors inventory levels and logs alerts for low stock and out-of-stock products.

**Schedule**: Every 1 hour

**Location**: `BackgroundServices/InventoryMonitoringService.cs`

**Thresholds**:

- Critical Stock: ≤ 5 units (WARNING level)
- Low Stock: ≤ 10 units (INFO level)
- Out of Stock: 0 units (INFO level)

**Configuration**:

```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
private const int LowStockThreshold = 10;
private const int CriticalStockThreshold = 5;
```

## Scheduled Jobs (Hangfire)

Hangfire provides a robust job scheduling system with a web-based dashboard for monitoring and management.

### Dashboard Access

**URL**: `http://localhost:8080/hangfire`

The dashboard provides:

- Real-time job monitoring
- Job history and statistics
- Manual job triggering
- Job retry management

### Configured Jobs

#### 1. Daily Analytics Generation

**Job ID**: `daily-analytics`

**Schedule**: Daily at 2:00 AM UTC

**Purpose**: Generates comprehensive daily analytics report

**Metrics Collected**:

- Total products
- In-stock vs out-of-stock counts
- Low stock product count
- Total inventory value
- Average product price
- Category breakdown with values

**Storage**: Cached for 24 hours under key `analytics:daily:latest`

**Retry Policy**: 3 automatic retries on failure

#### 2. Cache Cleanup

**Job ID**: `cache-cleanup`

**Schedule**: Every 6 hours

**Purpose**: Cleans up old or expired cache entries

**Retry Policy**: 2 automatic retries on failure

#### 3. Product Popularity Score Update

**Job ID**: `update-popularity-scores`

**Schedule**: Every 12 hours

**Purpose**: Updates product popularity scores based on stock levels

**Logic**: Products with low stock (< 20 units) are considered popular

**Storage**: Cached for 12 hours under key `products:popular`

**Retry Policy**: 3 automatic retries on failure

#### 4. Weekly Inventory Report

**Job ID**: `weekly-inventory-report`

**Schedule**: Every Monday at 8:00 AM UTC

**Purpose**: Generates comprehensive weekly inventory report

**Report Contents**:

- Week number and year
- Total products and value
- Low stock and out-of-stock alerts
- Top 10 categories by value
- Average stock levels per category

**Storage**: Cached for 30 days under key `reports:inventory:weekly:{year}:W{week}`

**Retry Policy**: 3 automatic retries on failure

#### 5. Monthly Data Cleanup

**Job ID**: `monthly-data-cleanup`

**Schedule**: 1st of each month at 3:00 AM UTC

**Purpose**: Cleans up old data (e.g., soft-deleted items older than 90 days)

**Retry Policy**: 2 automatic retries on failure

## Architecture

### Background Services Flow

```
Application Startup
    ↓
IHostedService.StartAsync()
    ↓
ExecuteAsync() - Long-running loop
    ↓
Perform Task → Wait (Interval) → Repeat
    ↓
Application Shutdown → StopAsync()
```

### Hangfire Jobs Flow

```
Application Startup
    ↓
Configure Hangfire with PostgreSQL
    ↓
Register Job Classes
    ↓
Schedule Recurring Jobs
    ↓
Hangfire Server Processes Jobs
    ↓
Jobs Execute on Schedule
```

## Configuration

### Hangfire Settings

Located in `Program.cs`:

```csharp
builder.Services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
        {
            options.UseNpgsqlConnection(connectionString);
        });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5; // Number of concurrent jobs
    options.ServerName = $"ProductCatalogService-{Environment.MachineName}";
});
```

### Background Services Registration

Located in `Program.cs`:

```csharp
builder.Services.AddHostedService<ElasticsearchSyncService>();
builder.Services.AddHostedService<CacheWarmingService>();
builder.Services.AddHostedService<InventoryMonitoringService>();
```

## Monitoring and Observability

### Logs

All background tasks and scheduled jobs use structured logging with appropriate log levels:

- **Information**: Normal operations, task completion
- **Warning**: Low stock alerts, partial failures
- **Error**: Task failures, exceptions

### Metrics

Jobs are tracked in Hangfire dashboard with:

- Execution count
- Success/failure rate
- Average execution time
- Last execution timestamp

### Health Checks

Background services are monitored through the application's health check system at `/health`.

## Cron Schedule Reference

Common cron expressions used:

```
Cron.Daily(2, 0)           → Every day at 2:00 AM
Cron.Hourly(6)             → Every 6 hours
"0 */12 * * *"             → Every 12 hours at minute 0
Cron.Weekly(Monday, 8, 0)  → Every Monday at 8:00 AM
Cron.Monthly(1, 3, 0)      → 1st of month at 3:00 AM
```

## Best Practices

1. **Scoped Services**: Use `IServiceProvider.CreateScope()` to create scoped services in background tasks
2. **Cancellation Tokens**: Always respect cancellation tokens for graceful shutdown
3. **Error Handling**: Implement try-catch blocks and log errors appropriately
4. **Retry Logic**: Use Hangfire's `[AutomaticRetry]` attribute for transient failures
5. **Resource Management**: Dispose of resources properly using `using` statements
6. **Monitoring**: Regularly check Hangfire dashboard and application logs

## Troubleshooting

### Background Service Not Running

1. Check application logs for startup errors
2. Verify database connectivity
3. Ensure required services (Redis, Elasticsearch) are available

### Hangfire Jobs Not Executing

1. Check Hangfire dashboard at `/hangfire`
2. Verify PostgreSQL connection string
3. Check Hangfire server is running (logs should show "Hangfire Server started")
4. Verify job is scheduled in "Recurring Jobs" tab

### Performance Issues

1. Adjust worker count in Hangfire configuration
2. Modify job schedules to reduce overlap
3. Optimize database queries in job implementations
4. Consider adding indexes to frequently queried tables

## Future Enhancements

Potential improvements:

1. **Email Notifications**: Send alerts for critical stock levels
2. **Metrics Export**: Export analytics to external systems
3. **Dynamic Scheduling**: Allow runtime schedule modifications
4. **Job Prioritization**: Implement job priority queues
5. **Distributed Locking**: Prevent duplicate job execution in multi-instance deployments
6. **Advanced Analytics**: Machine learning-based demand forecasting

## Dependencies

### NuGet Packages

```xml
<PackageReference Include="Hangfire.Core" Version="1.8.14" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.14" />
<PackageReference Include="Hangfire.PostgreSql" Version="1.20.9" />
```

### Infrastructure

- PostgreSQL (for Hangfire job storage)
- Redis (for caching)
- Elasticsearch (for search indexing)
- RabbitMQ (for messaging)

## Security Considerations

### Hangfire Dashboard

The current implementation allows unrestricted access to the Hangfire dashboard. For production:

```csharp
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Implement your authentication logic
        return httpContext.User.Identity?.IsAuthenticated ?? false;

        // Or check for specific roles
        // return httpContext.User.IsInRole("Admin");
    }
}
```

### Job Data

- Avoid storing sensitive data in job parameters
- Use encryption for sensitive configuration values
- Implement proper access controls for job management

## References

- [Hangfire Documentation](https://docs.hangfire.io/)
- [IHostedService Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [BackgroundService Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice)
