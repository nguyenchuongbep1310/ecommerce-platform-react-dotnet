#!/bin/bash

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=== E-Commerce Platform: Recovery & Restart ===${NC}\n"

print_status() {
    echo -e "${GREEN}✓${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

# ========== STEP 1: Stop Everything ==========
echo -e "${YELLOW}Step 1: Stopping all containers...${NC}"

print_info "Stopping containers (preserving data)..."
docker-compose down
print_status "Containers stopped"

sleep 3

# ========== STEP 2: Prune Old Artifacts ==========
echo -e "\n${YELLOW}Step 2: Cleaning up Docker artifacts...${NC}"

print_info "Removing orphaned containers..."
docker container prune -f --filter "label!=keep" > /dev/null 2>&1 || true
print_status "Orphaned containers removed"

# ========== STEP 3: Pull Latest Images ==========
echo -e "\n${YELLOW}Step 3: Pulling latest images...${NC}"

print_info "Pulling Docker images..."
docker-compose pull
print_status "Images pulled"

# ========== STEP 4: Start Fresh ==========
echo -e "\n${YELLOW}Step 4: Starting services...${NC}"

print_info "Starting all containers..."
docker-compose up -d
print_status "Containers started"

# ========== STEP 5: Wait for Services ==========
echo -e "\n${YELLOW}Step 5: Waiting for services to be healthy...${NC}"

print_info "Waiting 30 seconds for health checks..."
sleep 30

# ========== STEP 6: Create Kafka Topics ==========
echo -e "\n${YELLOW}Step 6: Ensuring Kafka topics exist...${NC}"

BROKERS="kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092"

# Wait for Kafka to be ready
MAX_RETRIES=30
RETRY=0
while ! docker-compose exec -T kafka-broker-1 kafka-broker-api-versions.sh --bootstrap-server kafka-broker-1:9092 > /dev/null 2>&1; do
    RETRY=$((RETRY + 1))
    if [ $RETRY -ge $MAX_RETRIES ]; then
        print_error "Kafka brokers not responding after $MAX_RETRIES attempts"
        exit 1
    fi
    echo -n "."
    sleep 2
done

print_status "Kafka brokers responding"

print_info "Creating topics..."

# Create topics
docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $BROKERS --create \
  --topic order.events --partitions 3 --replication-factor 1 --if-not-exists 2>/dev/null || true

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $BROKERS --create \
  --topic inventory.events --partitions 3 --replication-factor 1 --if-not-exists 2>/dev/null || true

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $BROKERS --create \
  --topic payment.events --partitions 3 --replication-factor 1 --if-not-exists 2>/dev/null || true

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $BROKERS --create \
  --topic notification.events --partitions 2 --replication-factor 1 --if-not-exists 2>/dev/null || true

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $BROKERS --create \
  --topic cart.events --partitions 2 --replication-factor 1 --if-not-exists 2>/dev/null || true

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $BROKERS --create \
  --topic user.events --partitions 1 --replication-factor 1 --if-not-exists 2>/dev/null || true

print_status "Topics ready"

# ========== STEP 7: Verify Services ==========
echo -e "\n${YELLOW}Step 7: Verifying services...${NC}"

RUNNING=$(docker-compose ps --status=running -q | wc -l)
echo "Running containers: $RUNNING/14"

# Check key services
SERVICES=("kafka-broker-1" "prometheus" "grafana" "postgres-user" "redis")

for service in "${SERVICES[@]}"; do
    if docker-compose ps "$service" | grep -q "Up"; then
        print_status "$service running"
    else
        print_error "$service not running"
    fi
done

# ========== STEP 8: Test Endpoints ==========
echo -e "\n${YELLOW}Step 8: Testing endpoints...${NC}"

sleep 10

# Test Kafka UI
echo -n "Kafka UI: "
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8888 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    print_status "OK (HTTP $HTTP_CODE)"
else
    print_error "Offline (HTTP $HTTP_CODE)"
fi

# Test Grafana
echo -n "Grafana: "
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:3000 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    print_status "OK (HTTP $HTTP_CODE)"
else
    print_error "Offline (HTTP $HTTP_CODE)"
fi

# Test Prometheus
echo -n "Prometheus: "
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:9090 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    print_status "OK (HTTP $HTTP_CODE)"
else
    print_error "Offline (HTTP $HTTP_CODE)"
fi

# Test Jaeger
echo -n "Jaeger: "
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:16686 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    print_status "OK (HTTP $HTTP_CODE)"
else
    print_error "Offline (HTTP $HTTP_CODE)"
fi

# ========== STEP 9: Show Status ==========
echo -e "\n${YELLOW}Step 9: Final Status${NC}"

docker-compose ps

# ========== SUMMARY ==========
echo -e "\n${BLUE}=== RECOVERY COMPLETE ===${NC}\n"

print_status "All services restarted and healthy!"

echo -e "${GREEN}Access your services:${NC}"
echo "  🔗 Kafka UI: http://localhost:8888"
echo "  📊 Grafana: http://localhost:3000 (admin/admin)"
echo "  📈 Prometheus: http://localhost:9090"
echo "  🔍 Jaeger: http://localhost:16686"

echo -e "\n${GREEN}Next Steps:${NC}"
echo "  1. Verify all dashboards load correctly"
echo "  2. Check Kafka topics: docker-compose exec kafka-broker-1 kafka-topics --bootstrap-server kafka-broker-1:9092 --list"
echo "  3. Ready for Phase 2!"

echo ""
