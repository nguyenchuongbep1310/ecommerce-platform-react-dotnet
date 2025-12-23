# Background Tasks Quick Start Guide

This guide will help you get started with the background tasks and scheduled jobs in the Product Catalog Service.

## Prerequisites

- .NET 10.0 SDK
- PostgreSQL (for Hangfire job storage and application data)
- Redis (for caching)
- Elasticsearch (for search indexing)
- RabbitMQ (for messaging)

## Starting the Service

1. **Start the infrastructure services** (if using Docker Compose):

   ```bash
   cd /path/to/ecommerce-platform/src
   docker-compose up -d postgres redis elasticsearch rabbitmq
   ```

2. **Run the Product Catalog Service**:

   ```bash
   cd /path/to/ProductCatalogService/ProductCatalogService.API
   dotnet run
   ```

3. **Verify the service is running**:
   - API: http://localhost:8080
   - Swagger: http://localhost:8080/swagger
   - Health Check: http://localhost:8080/health
   - **Hangfire Dashboard**: http://localhost:8080/hangfire

## Accessing the Hangfire Dashboard

The Hangfire Dashboard provides a comprehensive view of all background jobs:

**URL**: http://localhost:8080/hangfire

### Dashboard Features

- **Jobs**: View all queued, processing, succeeded, and failed jobs
- **Recurring Jobs**: Manage scheduled recurring jobs
- **Servers**: View active Hangfire servers
- **Retries**: Monitor and manage failed jobs awaiting retry
- **Statistics**: Real-time job execution statistics

### Dashboard Sections

1. **Recurring Jobs Tab**:

   - View all configured recurring jobs
   - See next execution time
   - Trigger jobs manually
   - Enable/disable jobs

2. **Jobs Tab**:
   - Enqueued: Jobs waiting to be processed
   - Processing: Currently running jobs
   - Succeeded: Successfully completed jobs
   - Failed: Jobs that encountered errors

## Background Services

These services run automatically when the application starts:

### 1. Elasticsearch Sync Service

- **Interval**: Every 30 minutes
- **Purpose**: Keeps Elasticsearch index in sync with PostgreSQL
- **Logs**: Check application logs for sync status

### 2. Cache Warming Service

- **Interval**: Every 6 hours
- **Purpose**: Pre-loads frequently accessed data into Redis
- **Cached Items**:
  - Popular products
  - Categories
  - Category counts
  - In-stock product counts

### 3. Inventory Monitoring Service

- **Interval**: Every 1 hour
- **Purpose**: Monitors stock levels and logs alerts
- **Alerts**:
  - Critical: ≤5 units
  - Low: ≤10 units
  - Out of stock: 0 units

## Scheduled Jobs (Hangfire)

### Viewing Scheduled Jobs

**API Endpoint**: `GET /api/backgroundjobs/recurring-jobs`

```bash
curl http://localhost:8080/api/backgroundjobs/recurring-jobs
```

**Response**:

```json
{
  "recurringJobs": [
    {
      "id": "daily-analytics",
      "name": "Daily Analytics Generation",
      "schedule": "Daily at 2:00 AM UTC",
      "cron": "0 2 * * *"
    }
    // ... more jobs
  ],
  "dashboardUrl": "/hangfire",
  "totalJobs": 5
}
```

### Manually Triggering Jobs

You can trigger jobs manually via API or the Hangfire Dashboard:

#### Via API

**Trigger Daily Analytics**:

```bash
curl -X POST http://localhost:8080/api/backgroundjobs/trigger-analytics
```

**Trigger Inventory Report**:

```bash
curl -X POST http://localhost:8080/api/backgroundjobs/trigger-inventory-report
```

**Trigger Cache Warmup**:

```bash
curl -X POST http://localhost:8080/api/backgroundjobs/trigger-cache-warmup
```

**Schedule Delayed Cleanup** (runs in 60 minutes):

```bash
curl -X POST "http://localhost:8080/api/backgroundjobs/schedule-cleanup?delayMinutes=60"
```

#### Via Hangfire Dashboard

1. Navigate to http://localhost:8080/hangfire
2. Click on **Recurring Jobs** tab
3. Find the job you want to trigger
4. Click **Trigger now** button

## Monitoring Jobs

### Check Job Status

1. **Via Hangfire Dashboard**:

   - Go to http://localhost:8080/hangfire
   - Click on **Jobs** tab
   - Select job status (Succeeded, Failed, Processing, etc.)
   - Click on a job to see details

2. **Via Application Logs**:

   ```bash
   # If running with dotnet run
   # Logs will appear in the console

   # If running in Docker
   docker logs productcatalog-service
   ```

### Common Log Messages

**Successful Job Execution**:

```
[Information] Daily analytics generated successfully. Total Products: 150, In Stock: 120, Total Value: $45,678.90
```

**Job Failure**:

```
[Error] Failed to generate daily analytics
System.Exception: Database connection failed
```

**Background Service Status**:

```
[Information] Elasticsearch Sync Service is starting
[Information] Successfully synced 150 products to Elasticsearch
[Information] Next Elasticsearch sync scheduled in 30 minutes
```

## Accessing Job Results

### Analytics Data

**Get Latest Daily Analytics**:

```bash
# The analytics are cached in Redis
# You can retrieve them via your cache service or Redis CLI

redis-cli GET "ProductCatalog_analytics:daily:latest"
```

### Weekly Reports

**Get Weekly Inventory Report**:

```bash
# Reports are cached with keys like: reports:inventory:weekly:2025:W51
redis-cli GET "ProductCatalog_reports:inventory:weekly:2025:W51"
```

### Popular Products

**Get Popular Products List**:

```bash
redis-cli GET "ProductCatalog_products:popular"
```

## Troubleshooting

### Jobs Not Running

**Check Hangfire Server Status**:

1. Go to http://localhost:8080/hangfire
2. Click on **Servers** tab
3. Verify server is active

**Check Application Logs**:

```bash
# Look for Hangfire startup messages
[Information] Hangfire Server started
```

### Background Services Not Running

**Check Health Endpoint**:

```bash
curl http://localhost:8080/health
```

**Check Application Logs**:

```bash
# Look for background service startup messages
[Information] Elasticsearch Sync Service is starting
[Information] Cache Warming Service is starting
[Information] Inventory Monitoring Service is starting
```

### Database Connection Issues

**Verify PostgreSQL Connection**:

```bash
# Check connection string in appsettings.json
# Default: "Host=localhost;Database=productcatalog;Username=postgres;Password=postgres"

# Test connection
psql -h localhost -U postgres -d productcatalog
```

### Redis Connection Issues

**Verify Redis Connection**:

```bash
# Check connection string in appsettings.json
# Default: "localhost:6379"

# Test connection
redis-cli ping
# Should return: PONG
```

### Elasticsearch Connection Issues

**Verify Elasticsearch Connection**:

```bash
# Check connection string in appsettings.json
# Default: "http://localhost:9200"

# Test connection
curl http://localhost:9200
```

## Configuration

### Adjusting Job Schedules

Edit `ScheduledJobs/HangfireJobScheduler.cs`:

```csharp
// Change daily analytics to run at 3:00 AM instead of 2:00 AM
RecurringJob.AddOrUpdate<ProductCatalogJobs>(
    "daily-analytics",
    job => job.GenerateDailyAnalyticsAsync(),
    Cron.Daily(3, 0), // Changed from 2 to 3
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });
```

### Adjusting Background Service Intervals

Edit the respective background service file:

**ElasticsearchSyncService.cs**:

```csharp
// Change from 30 minutes to 1 hour
private readonly TimeSpan _syncInterval = TimeSpan.FromHours(1);
```

**CacheWarmingService.cs**:

```csharp
// Change from 6 hours to 12 hours
private readonly TimeSpan _warmupInterval = TimeSpan.FromHours(12);
```

**InventoryMonitoringService.cs**:

```csharp
// Change from 1 hour to 30 minutes
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);
```

### Adjusting Hangfire Worker Count

Edit `Program.cs`:

```csharp
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 10; // Increase from 5 to 10 for more concurrent jobs
    options.ServerName = $"ProductCatalogService-{Environment.MachineName}";
});
```

## Testing

### Test Background Services

1. **Start the service**
2. **Check logs** for service startup messages
3. **Wait for first execution** (or trigger manually)
4. **Verify results** in logs and cache

### Test Scheduled Jobs

1. **Navigate to Hangfire Dashboard**: http://localhost:8080/hangfire
2. **Go to Recurring Jobs tab**
3. **Click "Trigger now"** on any job
4. **Monitor execution** in the Jobs tab
5. **Check job details** for success/failure

### Test Manual Job Triggering

```bash
# Trigger analytics
curl -X POST http://localhost:8080/api/backgroundjobs/trigger-analytics

# Check response for job ID
{
  "message": "Analytics generation job queued successfully",
  "jobId": "12345",
  "dashboardUrl": "/hangfire"
}

# Visit dashboard to monitor job
# http://localhost:8080/hangfire
```

## Best Practices

1. **Monitor the Dashboard Regularly**: Check for failed jobs and retry them if needed
2. **Review Logs**: Keep an eye on application logs for warnings and errors
3. **Adjust Schedules**: Optimize job schedules based on your traffic patterns
4. **Test Before Production**: Always test job changes in a development environment
5. **Implement Proper Authentication**: Secure the Hangfire dashboard in production
6. **Set Up Alerts**: Configure alerts for critical job failures
7. **Clean Up Old Jobs**: Regularly clean up old job history in Hangfire

## Production Considerations

### Security

**Secure Hangfire Dashboard**:

Edit `HangfireAuthorizationFilter.cs`:

```csharp
public bool Authorize(DashboardContext context)
{
    var httpContext = context.GetHttpContext();

    // Require authentication
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return false;

    // Require admin role
    return httpContext.User.IsInRole("Admin");
}
```

### Performance

- **Adjust Worker Count**: Based on server capacity
- **Optimize Job Execution**: Keep jobs lightweight and fast
- **Use Batch Operations**: For bulk data processing
- **Monitor Resource Usage**: CPU, memory, database connections

### Reliability

- **Enable Automatic Retries**: Already configured via `[AutomaticRetry]` attribute
- **Implement Idempotency**: Ensure jobs can be safely retried
- **Use Distributed Locks**: Prevent duplicate job execution in multi-instance setups
- **Monitor Job Queue**: Ensure jobs aren't backing up

## Additional Resources

- **Full Documentation**: See `/docs/BACKGROUND_TASKS.md`
- **Hangfire Documentation**: https://docs.hangfire.io/
- **Cron Expression Generator**: https://crontab.guru/
- **API Documentation**: http://localhost:8080/swagger

## Support

For issues or questions:

1. Check application logs
2. Review Hangfire dashboard
3. Consult the full documentation
4. Check health endpoints
