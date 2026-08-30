#!/bin/bash

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=== E-Commerce Platform: Debug & Diagnostic ===${NC}\n"

# Function to print status
print_status() {
    echo -e "${GREEN}✓${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

# ========== STEP 1: Docker Status ==========
echo -e "${YELLOW}Step 1: Docker Status${NC}"

if command -v docker &> /dev/null; then
    print_status "Docker installed"
    DOCKER_VERSION=$(docker --version)
    echo "  → $DOCKER_VERSION"
else
    print_error "Docker not installed"
    exit 1
fi

if command -v docker-compose &> /dev/null; then
    print_status "Docker Compose installed"
    COMPOSE_VERSION=$(docker-compose --version)
    echo "  → $COMPOSE_VERSION"
else
    print_error "Docker Compose not installed"
    exit 1
fi

# Check if Docker daemon is running
if docker ps > /dev/null 2>&1; then
    print_status "Docker daemon running"
else
    print_error "Docker daemon not running"
    echo "  → Please start Docker Desktop"
    exit 1
fi

# ========== STEP 2: Container Status ==========
echo -e "\n${YELLOW}Step 2: Container Status${NC}"

CONTAINER_COUNT=$(docker-compose ps --services | wc -l)
RUNNING_COUNT=$(docker-compose ps --status=running -q | wc -l)

echo "Total services defined: $CONTAINER_COUNT"
echo "Running containers: $RUNNING_COUNT"

if [ "$RUNNING_COUNT" -lt 10 ]; then
    print_warning "Only $RUNNING_COUNT/$CONTAINER_COUNT containers running"
    echo -e "\n${YELLOW}Container Details:${NC}"
    docker-compose ps
else
    print_status "Most containers running"
    docker-compose ps --status=running | head -5
fi

# ========== STEP 3: Health Check ==========
echo -e "\n${YELLOW}Step 3: Container Health Status${NC}"

HEALTHY=$(docker-compose ps | grep -i "healthy" | wc -l)
UNHEALTHY=$(docker-compose ps | grep -i "unhealthy" | wc -l)

echo "Healthy containers: $HEALTHY"
echo "Unhealthy containers: $UNHEALTHY"

if [ "$UNHEALTHY" -gt 0 ]; then
    print_warning "Some containers are unhealthy:"
    docker-compose ps | grep "unhealthy"
fi

# ========== STEP 4: Port Binding ==========
echo -e "\n${YELLOW}Step 4: Port Binding Check${NC}"

declare -A ports=(
    ["8888"]="Kafka UI"
    ["3000"]="Grafana"
    ["9090"]="Prometheus"
    ["16686"]="Jaeger"
    ["8081"]="Schema Registry"
    ["6379"]="Redis"
    ["5432"]="PostgreSQL (User)"
    ["5434"]="PostgreSQL (Product)"
    ["5435"]="PostgreSQL (Cart)"
    ["5436"]="PostgreSQL (Order)"
    ["2181"]="Zookeeper"
)

for port in "${!ports[@]}"; do
    if nc -z localhost "$port" 2>/dev/null; then
        print_status "${ports[$port]} (port $port)"
    else
        print_error "${ports[$port]} (port $port)"
    fi
done

# ========== STEP 5: Kafka Connectivity ==========
echo -e "\n${YELLOW}Step 5: Kafka Cluster Health${NC}"

if docker-compose exec -T kafka-broker-1 kafka-broker-api-versions.sh --bootstrap-server kafka-broker-1:9092 > /dev/null 2>&1; then
    print_status "Kafka brokers responding"
else
    print_error "Kafka brokers not responding"
fi

# Check topics
TOPIC_COUNT=$(docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server kafka-broker-1:9092 --list 2>/dev/null | wc -l)
echo "Topics created: $TOPIC_COUNT"

if [ "$TOPIC_COUNT" -ge 6 ]; then
    print_status "All required topics present"
else
    print_warning "Only $TOPIC_COUNT topics found (expected 6+)"
fi

# ========== STEP 6: Database Connectivity ==========
echo -e "\n${YELLOW}Step 6: Database Connectivity${NC}"

DATABASES=("user_db" "product_db" "cart_db" "order_db")

for db in "${DATABASES[@]}"; do
    if docker-compose exec postgres-user psql -U postgres -d "$db" -c "SELECT 1;" > /dev/null 2>&1; then
        print_status "$db accessible"
    else
        print_error "$db not accessible"
    fi
done

# ========== STEP 7: HTTP Endpoints ==========
echo -e "\n${YELLOW}Step 7: HTTP Endpoints Health${NC}"

echo "Testing Kafka UI (8888):"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8888 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    print_status "Kafka UI responding (HTTP $HTTP_CODE)"
else
    print_error "Kafka UI not responding (HTTP $HTTP_CODE)"
fi

echo "Testing Grafana (3000):"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:3000 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    print_status "Grafana responding (HTTP $HTTP_CODE)"
else
    print_error "Grafana not responding (HTTP $HTTP_CODE)"
fi

echo "Testing Prometheus (9090):"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:9090 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    print_status "Prometheus responding (HTTP $HTTP_CODE)"
else
    print_error "Prometheus not responding (HTTP $HTTP_CODE)"
fi

echo "Testing Jaeger (16686):"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:16686 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    print_status "Jaeger responding (HTTP $HTTP_CODE)"
else
    print_error "Jaeger not responding (HTTP $HTTP_CODE)"
fi

# ========== STEP 8: Redis ==========
echo -e "\n${YELLOW}Step 8: Redis Check${NC}"

if docker-compose exec redis redis-cli ping > /dev/null 2>&1; then
    print_status "Redis responding"
else
    print_error "Redis not responding"
fi

# ========== STEP 9: Recent Logs ==========
echo -e "\n${YELLOW}Step 9: Recent Container Logs${NC}"

echo "Kafka UI recent errors:"
docker-compose logs kafka-ui --tail=3 2>&1 | grep -i "error" || echo "  No errors"

echo "Grafana recent errors:"
docker-compose logs grafana --tail=3 2>&1 | grep -i "error" || echo "  No errors"

echo "Prometheus recent errors:"
docker-compose logs prometheus --tail=3 2>&1 | grep -i "error" || echo "  No errors"

# ========== SUMMARY ==========
echo -e "\n${BLUE}=== SUMMARY ===${NC}"

if [ "$RUNNING_COUNT" -ge 13 ] && [ "$UNHEALTHY" -eq 0 ]; then
    print_status "✨ System appears healthy!"
    echo -e "\n${GREEN}Access your services:${NC}"
    echo "  Kafka UI: http://localhost:8888"
    echo "  Grafana: http://localhost:3000 (admin/admin)"
    echo "  Prometheus: http://localhost:9090"
    echo "  Jaeger: http://localhost:16686"
else
    print_warning "⚠️  Some services have issues"
    echo -e "\n${YELLOW}Recommended Actions:${NC}"
    echo "1. Check container logs: docker-compose logs service-name"
    echo "2. Restart services: docker-compose restart"
    echo "3. Full recovery: Run recovery.sh script"
    echo "4. Nuclear option: docker-compose down -v && bash setup.sh"
fi

echo -e "\n${BLUE}=== END DEBUG ===${NC}\n"
