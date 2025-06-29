#!/bin/bash

echo "Restarting URL Short Service (WSL)..."
echo

echo "Checking current container status..."
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo
echo "Stopping all services..."
docker-compose down

echo
echo "Cleaning up any orphaned containers..."
docker container prune -f

echo
echo "Checking which services are defined..."
docker-compose config --services

echo
echo "Rebuilding all application services..."
# Build all services that exist in the compose file
if docker-compose config --services | grep -q urlshort-app-4; then
    echo "Found 4 application instances - building enhanced version..."
    docker-compose build urlshort-app-1 urlshort-app-2 urlshort-app-3 urlshort-app-4
else
    echo "Found 3 application instances - building standard version..."
    docker-compose build urlshort-app-1 urlshort-app-2 urlshort-app-3
fi

echo
echo "Starting infrastructure services first..."
docker-compose up -d mysql redis

echo
echo "Waiting for infrastructure to be ready..."
sleep 30

echo
echo "Starting monitoring services..."
docker-compose up -d prometheus grafana mysql-exporter redis-exporter

echo
echo "Waiting for monitoring services..."
sleep 15

echo
echo "Starting application services..."
docker-compose up -d $(docker-compose config --services | grep urlshort-app)

echo
echo "Starting nginx load balancer..."
docker-compose up -d nginx

echo
echo "Final startup - ensuring all services are running..."
docker-compose up -d

echo
echo "Waiting for all services to be healthy..."
echo "This may take 2-3 minutes for MySQL to fully initialize..."
sleep 90

echo
echo "Final service status:"
docker-compose ps

echo
echo "Detailed container status:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo
echo "Testing health endpoints..."

echo "MySQL Status:"
max_attempts=5
attempt=1
while [ $attempt -le $max_attempts ]; do
    echo "Attempt $attempt/$max_attempts: Testing MySQL connection..."
    if docker-compose exec -T mysql mysqladmin ping -h localhost -u urlshort -purlshort123 2>/dev/null; then
        echo "✓ MySQL is ready!"
        break
    elif [ $attempt -eq $max_attempts ]; then
        echo "✗ MySQL not ready after $max_attempts attempts"
    else
        echo "  MySQL not ready, waiting 10 seconds..."
        sleep 10
    fi
    attempt=$((attempt + 1))
done

echo
echo "Redis Status:"
if docker-compose exec -T redis redis-cli ping 2>/dev/null | grep -q PONG; then
    echo "✓ Redis is ready!"
else
    echo "✗ Redis not ready"
fi

echo
echo "Application Instances Status:"
app_ready=0
total_apps=0

# Count total application instances
for app in urlshort-app-1 urlshort-app-2 urlshort-app-3 urlshort-app-4; do
    if docker-compose ps | grep -q "$app"; then
        total_apps=$((total_apps + 1))
        echo "Testing $app..."
        
        # Get the port for this app instance
        case $app in
            urlshort-app-1) port=8081 ;;
            urlshort-app-2) port=8082 ;;
            urlshort-app-3) port=8083 ;;
            urlshort-app-4) port=8084 ;;
        esac
        
        if curl -s http://localhost:$port/health >/dev/null 2>&1; then
            echo "✓ $app (port $port) is ready!"
            app_ready=$((app_ready + 1))
        else
            echo "✗ $app (port $port) not ready"
        fi
    fi
done

echo
echo "Load Balancer Status:"
if curl -s http://localhost:10086/health >/dev/null 2>&1; then
    echo "✓ Nginx load balancer is ready!"
else
    echo "✗ Nginx load balancer not ready"
    echo "  Checking if nginx container is running..."
    if docker-compose ps | grep -q nginx; then
        echo "  Nginx container is running, but not responding on port 10086"
    else
        echo "  Nginx container is not running!"
    fi
fi

echo
echo "============================================"
echo "Restart Summary:"
echo "============================================"
echo "Application instances ready: $app_ready/$total_apps"
echo "MySQL: $(if docker-compose exec -T mysql mysqladmin ping -h localhost -u urlshort -purlshort123 2>/dev/null >/dev/null; then echo "Ready"; else echo "Not Ready"; fi)"
echo "Redis: $(if docker-compose exec -T redis redis-cli ping 2>/dev/null | grep -q PONG; then echo "Ready"; else echo "Not Ready"; fi)"
echo "Load Balancer: $(if curl -s http://localhost:10086/health >/dev/null 2>&1; then echo "Ready"; else echo "Not Ready"; fi)"
echo
echo "Service URLs:"
echo "  Main Application: http://localhost:10086"
echo "  Grafana Monitor: http://localhost:3000 (admin/admin123)"
echo "  Prometheus: http://localhost:9090"
echo
if [ $app_ready -eq $total_apps ] && curl -s http://localhost:10086/health >/dev/null 2>&1; then
    echo "🎉 All services are ready! System is operational."
else
    echo "⚠️  Some services are not ready. Check logs with:"
    echo "   docker-compose logs [service-name]"
    echo "   or run: ./validate_performance.sh"
fi
echo "============================================"
