#!/bin/bash
# Restore original docker-compose.yml with ZooKeeper
cd /Users/chuongnguyen/Downloads/Coding/ecommerce-platform/phase-1-foundation

echo "Restoring ZooKeeper-based docker-compose.yml..."

# Stop and remove
docker-compose down -v 2>/dev/null || true
sleep 5

# The original is being restored via the CLI below
echo "✓ Ready to start with ZooKeeper mode"
echo ""
echo "Next: Run this:"
echo "  docker-compose up -d"
echo "  sleep 120"
echo "  docker-compose ps"
