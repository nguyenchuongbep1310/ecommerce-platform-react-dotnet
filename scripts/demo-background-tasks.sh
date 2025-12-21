#!/bin/bash

# Background Tasks Demo Script
# This script demonstrates the background tasks and scheduled jobs functionality

echo "=========================================="
echo "Background Tasks & Scheduled Jobs Demo"
echo "=========================================="
echo ""

BASE_URL="http://localhost:8080"

# Check if service is running
echo "1. Checking if service is running..."
if curl -s "$BASE_URL/health" > /dev/null; then
    echo "✅ Service is running"
else
    echo "❌ Service is not running. Please start the service first."
    echo "   Run: dotnet run"
    exit 1
fi
echo ""

# View background services
echo "2. Viewing background services..."
curl -s "$BASE_URL/api/backgroundjobs/background-services" | jq '.'
echo ""

# View recurring jobs
echo "3. Viewing recurring jobs..."
curl -s "$BASE_URL/api/backgroundjobs/recurring-jobs" | jq '.'
echo ""

# Trigger analytics
echo "4. Triggering daily analytics generation..."
ANALYTICS_RESPONSE=$(curl -s -X POST "$BASE_URL/api/backgroundjobs/trigger-analytics")
echo "$ANALYTICS_RESPONSE" | jq '.'
ANALYTICS_JOB_ID=$(echo "$ANALYTICS_RESPONSE" | jq -r '.jobId')
echo "   Job ID: $ANALYTICS_JOB_ID"
echo ""

# Trigger inventory report
echo "5. Triggering weekly inventory report..."
REPORT_RESPONSE=$(curl -s -X POST "$BASE_URL/api/backgroundjobs/trigger-inventory-report")
echo "$REPORT_RESPONSE" | jq '.'
REPORT_JOB_ID=$(echo "$REPORT_RESPONSE" | jq -r '.jobId')
echo "   Job ID: $REPORT_JOB_ID"
echo ""

# Schedule delayed cleanup
echo "6. Scheduling delayed cleanup (in 5 minutes)..."
CLEANUP_RESPONSE=$(curl -s -X POST "$BASE_URL/api/backgroundjobs/schedule-cleanup?delayMinutes=5")
echo "$CLEANUP_RESPONSE" | jq '.'
CLEANUP_JOB_ID=$(echo "$CLEANUP_RESPONSE" | jq -r '.jobId')
echo "   Job ID: $CLEANUP_JOB_ID"
echo ""

# Dashboard info
echo "=========================================="
echo "Next Steps:"
echo "=========================================="
echo ""
echo "1. View Hangfire Dashboard:"
echo "   🌐 $BASE_URL/hangfire"
echo ""
echo "2. Monitor job execution:"
echo "   - Analytics Job: $ANALYTICS_JOB_ID"
echo "   - Report Job: $REPORT_JOB_ID"
echo "   - Cleanup Job: $CLEANUP_JOB_ID"
echo ""
echo "3. Check application logs for background service activity"
echo ""
echo "4. View Swagger API documentation:"
echo "   🌐 $BASE_URL/swagger"
echo ""
echo "=========================================="
echo "Demo completed successfully! ✨"
echo "=========================================="
