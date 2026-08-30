#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== E-Commerce Platform: Phase 1 Setup ===${NC}\n"

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

# Step 1: Check prerequisites
echo -e "${YELLOW}Step 1: Checking Prerequisites${NC}"

if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed"
    exit 1
fi
print_status "Docker installed"

if ! command -v docker-compose &> /dev/null; then
    print_error "Docker Compose is not installed"
    exit 1
fi
print_status "Docker Compose installed"

if ! command -v go &> /dev/null; then
    print_error "Go is not installed. Please install Go 1.21+"
    exit 1
fi
GO_VERSION=$(go version | awk '{print $3}')
print_status "Go installed ($GO_VERSION)"

# Step 2: Create directory structure
echo -e "\n${YELLOW}Step 2: Creating Directory Structure${NC}"

mkdir -p golang-shared/{pkg,config}
mkdir -p golang-shared/{pkg/{logger,database,cache,kafka,errors,jwt,http,utils}}
mkdir -p golang-shared/config

mkdir -p monitoring/{prometheus,grafana/provisioning/{dashboards,datasources}}
mkdir -p scripts/{kafka,migrations}

print_status "Directory structure created"

# Step 3: Initialize Go modules
echo -e "\n${YELLOW}Step 3: Initializing Go Modules${NC}"

cd golang-shared
go mod init github.com/ecommerce-platform/shared
go get github.com/sirupsen/logrus
go get github.com/joho/godotenv
go get github.com/confluentinc/confluent-kafka-go/v2
go get github.com/go-redis/redis/v8
go get gorm.io/gorm
go get gorm.io/driver/postgres
go get github.com/golang-jwt/jwt/v5
go get go.opentelemetry.io/otel
go get go.opentelemetry.io/otel/exporters/jaeger
print_status "Go modules initialized"

cd ..

# Step 4: Create Prometheus configuration
echo -e "\n${YELLOW}Step 4: Creating Monitoring Configuration${NC}"

cat > monitoring/prometheus.yml << 'EOF'
global:
  scrape_interval: 15s
  evaluation_interval: 15s

alerting:
  alertmanagers:
    - static_configs:
        - targets: []

rule_files: []

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'kafka'
    static_configs:
      - targets: ['kafka-broker-1:9092', 'kafka-broker-2:9092', 'kafka-broker-3:9092']

  - job_name: 'golang-services'
    static_configs:
      - targets: ['localhost:8081', 'localhost:8082', 'localhost:8083']
    relabel_configs:
      - source_labels: [__address__]
        target_label: instance
EOF

print_status "Prometheus configuration created"

# Step 5: Create Grafana provisioning
cat > monitoring/grafana/provisioning/datasources/prometheus.yml << 'EOF'
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    orgId: 1
    url: http://prometheus:9090
    isDefault: true
    editable: true
EOF

print_status "Grafana datasources configured"

# Step 6: Create Kafka topics initialization script
echo -e "\n${YELLOW}Step 5: Creating Kafka Setup Scripts${NC}"

cat > scripts/kafka/create-topics.sh << 'EOF'
#!/bin/bash

KAFKA_BROKERS="kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092"

echo "Creating Kafka topics..."

# Create topics
docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $KAFKA_BROKERS --create \
  --topic order.events --partitions 3 --replication-factor 1 --if-not-exists

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $KAFKA_BROKERS --create \
  --topic inventory.events --partitions 3 --replication-factor 1 --if-not-exists

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $KAFKA_BROKERS --create \
  --topic payment.events --partitions 3 --replication-factor 1 --if-not-exists

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $KAFKA_BROKERS --create \
  --topic notification.events --partitions 2 --replication-factor 1 --if-not-exists

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $KAFKA_BROKERS --create \
  --topic cart.events --partitions 2 --replication-factor 1 --if-not-exists

docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $KAFKA_BROKERS --create \
  --topic user.events --partitions 1 --replication-factor 1 --if-not-exists

echo "Topics created successfully!"

# List topics
docker-compose exec -T kafka-broker-1 kafka-topics --bootstrap-server $KAFKA_BROKERS --list
EOF

chmod +x scripts/kafka/create-topics.sh
print_status "Kafka setup scripts created"

# Step 7: Start Docker Compose
echo -e "\n${YELLOW}Step 6: Starting Infrastructure${NC}"

docker-compose pull
print_status "Docker images pulled"

docker-compose up -d
print_status "Docker containers started"

# Wait for services to be healthy
echo -e "\n${YELLOW}Step 7: Waiting for Services to be Healthy${NC}"

wait_for_service() {
    local service=$1
    local port=$2
    local max_attempts=30
    local attempt=1

    while [ $attempt -le $max_attempts ]; do
        if curl -s http://localhost:$port > /dev/null 2>&1; then
            print_status "$service is healthy"
            return 0
        fi
        echo -n "."
        sleep 2
        attempt=$((attempt + 1))
    done

    print_error "$service failed to start"
    return 1
}

echo "Waiting for Kafka..."
sleep 10
docker-compose exec -T kafka-broker-1 kafka-broker-api-versions.sh --bootstrap-server kafka-broker-1:9092 > /dev/null 2>&1 && print_status "Kafka is healthy" || print_error "Kafka health check failed"

wait_for_service "Prometheus" 9090
wait_for_service "Grafana" 3000
wait_for_service "Jaeger" 16686
wait_for_service "Kafka UI" 8888

# Step 8: Create Kafka topics
echo -e "\n${YELLOW}Step 8: Creating Kafka Topics${NC}"
sleep 5
bash scripts/kafka/create-topics.sh

# Step 9: Display summary
echo -e "\n${GREEN}=== Phase 1 Setup Complete! ===${NC}\n"

echo -e "${BLUE}Infrastructure Status:${NC}"
docker-compose ps

echo -e "\n${BLUE}Access Points:${NC}"
print_info "Kafka UI: http://localhost:8888"
print_info "Prometheus: http://localhost:9090"
print_info "Grafana: http://localhost:3000 (admin/admin)"
print_info "Jaeger: http://localhost:16686"
print_info "Schema Registry: http://localhost:8081"
print_info "Redis: localhost:6379"
print_info "PostgreSQL (User): localhost:5432"
print_info "PostgreSQL (Product): localhost:5434"
print_info "PostgreSQL (Cart): localhost:5435"
print_info "PostgreSQL (Order): localhost:5436"

echo -e "\n${BLUE}Next Steps:${NC}"
echo "1. Verify all services are running: docker-compose ps"
echo "2. Check Kafka topics: docker-compose exec kafka-broker-1 kafka-topics --bootstrap-server kafka-broker-1:9092 --list"
echo "3. Start Phase 2: Implement UserService in Golang"
echo "4. Reference: ../docs/GOLANG_KAFKA_MIGRATION_SPECKIT.md Section 4.2.1"

echo -e "\n${GREEN}✓ Phase 1 Foundation is ready for Phase 2!${NC}\n"
