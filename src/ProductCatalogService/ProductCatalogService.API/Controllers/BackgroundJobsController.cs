using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace ProductCatalogService.Controllers;

/// <summary>
/// Controller for managing background jobs and scheduled tasks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BackgroundJobsController : ControllerBase
{
    private readonly ILogger<BackgroundJobsController> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public BackgroundJobsController(
        ILogger<BackgroundJobsController> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Manually trigger daily analytics generation
    /// </summary>
    [HttpPost("trigger-analytics")]
    public IActionResult TriggerAnalytics()
    {
        var jobId = _backgroundJobClient.Enqueue<ProductCatalogService.Infrastructure.Jobs.Scheduled.ProductCatalogJobs>(
            job => job.GenerateDailyAnalyticsAsync());

        _logger.LogInformation("Manually triggered analytics generation. Job ID: {JobId}", jobId);

        return Ok(new
        {
            message = "Analytics generation job queued successfully",
            jobId = jobId,
            dashboardUrl = "/hangfire"
        });
    }

    /// <summary>
    /// Manually trigger inventory report generation
    /// </summary>
    [HttpPost("trigger-inventory-report")]
    public IActionResult TriggerInventoryReport()
    {
        var jobId = _backgroundJobClient.Enqueue<ProductCatalogService.Infrastructure.Jobs.Scheduled.ProductCatalogJobs>(
            job => job.GenerateWeeklyInventoryReportAsync());

        _logger.LogInformation("Manually triggered inventory report generation. Job ID: {JobId}", jobId);

        return Ok(new
        {
            message = "Inventory report generation job queued successfully",
            jobId = jobId,
            dashboardUrl = "/hangfire"
        });
    }

    /// <summary>
    /// Manually trigger cache warmup
    /// </summary>
    [HttpPost("trigger-cache-warmup")]
    public IActionResult TriggerCacheWarmup()
    {
        // Note: This triggers the Hangfire job, not the background service
        var jobId = _backgroundJobClient.Enqueue<ProductCatalogService.Infrastructure.Jobs.Scheduled.ProductCatalogJobs>(
            job => job.CleanupOldCacheEntriesAsync());

        _logger.LogInformation("Manually triggered cache warmup. Job ID: {JobId}", jobId);

        return Ok(new
        {
            message = "Cache warmup job queued successfully",
            jobId = jobId,
            dashboardUrl = "/hangfire"
        });
    }

    /// <summary>
    /// Schedule a delayed job (example: cleanup in 1 hour)
    /// </summary>
    [HttpPost("schedule-cleanup")]
    public IActionResult ScheduleDelayedCleanup([FromQuery] int delayMinutes = 60)
    {
        var jobId = _backgroundJobClient.Schedule<ProductCatalogService.Infrastructure.Jobs.Scheduled.ProductCatalogJobs>(
            job => job.CleanupOldDataAsync(),
            TimeSpan.FromMinutes(delayMinutes));

        _logger.LogInformation(
            "Scheduled cleanup job to run in {Minutes} minutes. Job ID: {JobId}",
            delayMinutes,
            jobId);

        return Ok(new
        {
            message = $"Cleanup job scheduled to run in {delayMinutes} minutes",
            jobId = jobId,
            scheduledFor = DateTime.UtcNow.AddMinutes(delayMinutes),
            dashboardUrl = "/hangfire"
        });
    }

    /// <summary>
    /// Get information about recurring jobs
    /// </summary>
    [HttpGet("recurring-jobs")]
    public IActionResult GetRecurringJobs()
    {
        var recurringJobs = new[]
        {
            new
            {
                id = "daily-analytics",
                name = "Daily Analytics Generation",
                schedule = "Daily at 2:00 AM UTC",
                cron = "0 2 * * *"
            },
            new
            {
                id = "cache-cleanup",
                name = "Cache Cleanup",
                schedule = "Every 6 hours",
                cron = "0 */6 * * *"
            },
            new
            {
                id = "update-popularity-scores",
                name = "Product Popularity Update",
                schedule = "Every 12 hours",
                cron = "0 */12 * * *"
            },
            new
            {
                id = "weekly-inventory-report",
                name = "Weekly Inventory Report",
                schedule = "Every Monday at 8:00 AM UTC",
                cron = "0 8 * * 1"
            },
            new
            {
                id = "monthly-data-cleanup",
                name = "Monthly Data Cleanup",
                schedule = "1st of month at 3:00 AM UTC",
                cron = "0 3 1 * *"
            }
        };

        return Ok(new
        {
            recurringJobs = recurringJobs,
            dashboardUrl = "/hangfire",
            totalJobs = recurringJobs.Length
        });
    }

    /// <summary>
    /// Get information about background services
    /// </summary>
    [HttpGet("background-services")]
    public IActionResult GetBackgroundServices()
    {
        var backgroundServices = new[]
        {
            new
            {
                name = "Elasticsearch Sync Service",
                interval = "Every 30 minutes",
                description = "Syncs products from PostgreSQL to Elasticsearch"
            },
            new
            {
                name = "Cache Warming Service",
                interval = "Every 6 hours",
                description = "Warms up Redis cache with frequently accessed data"
            },
            new
            {
                name = "Inventory Monitoring Service",
                interval = "Every 1 hour",
                description = "Monitors inventory levels and logs alerts"
            }
        };

        return Ok(new
        {
            backgroundServices = backgroundServices,
            totalServices = backgroundServices.Length,
            note = "Background services run continuously and cannot be manually triggered"
        });
    }
}
