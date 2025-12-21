using Hangfire;

namespace ProductCatalogService.ScheduledJobs;

/// <summary>
/// Configures and schedules recurring jobs using Hangfire
/// </summary>
public static class HangfireJobScheduler
{
    /// <summary>
    /// Configures all recurring jobs for the Product Catalog Service
    /// </summary>
    public static void ConfigureRecurringJobs()
    {
        // Daily analytics - runs every day at 2:00 AM UTC
        RecurringJob.AddOrUpdate<ProductCatalogJobs>(
            "daily-analytics",
            job => job.GenerateDailyAnalyticsAsync(),
            Cron.Daily(2, 0), // 2:00 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Cache cleanup - runs every 6 hours
        RecurringJob.AddOrUpdate<ProductCatalogJobs>(
            "cache-cleanup",
            job => job.CleanupOldCacheEntriesAsync(),
            Cron.Hourly(6), // Every 6 hours
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Product popularity update - runs every 12 hours
        RecurringJob.AddOrUpdate<ProductCatalogJobs>(
            "update-popularity-scores",
            job => job.UpdateProductPopularityScoresAsync(),
            "0 */12 * * *", // Every 12 hours at minute 0
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Weekly inventory report - runs every Monday at 8:00 AM UTC
        RecurringJob.AddOrUpdate<ProductCatalogJobs>(
            "weekly-inventory-report",
            job => job.GenerateWeeklyInventoryReportAsync(),
            Cron.Weekly(DayOfWeek.Monday, 8, 0), // Monday at 8:00 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Monthly data cleanup - runs on the 1st of each month at 3:00 AM UTC
        RecurringJob.AddOrUpdate<ProductCatalogJobs>(
            "monthly-data-cleanup",
            job => job.CleanupOldDataAsync(),
            Cron.Monthly(1, 3, 0), // 1st day of month at 3:00 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
    }

    /// <summary>
    /// Removes all configured recurring jobs
    /// </summary>
    public static void RemoveAllRecurringJobs()
    {
        RecurringJob.RemoveIfExists("daily-analytics");
        RecurringJob.RemoveIfExists("cache-cleanup");
        RecurringJob.RemoveIfExists("update-popularity-scores");
        RecurringJob.RemoveIfExists("weekly-inventory-report");
        RecurringJob.RemoveIfExists("monthly-data-cleanup");
    }
}
