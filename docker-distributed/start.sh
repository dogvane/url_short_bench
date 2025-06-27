#!/bin/bash

echo "Starting URL Short Service with distributed architecture..."
echo

echo "Pulling latest images..."
docker-compose pull

echo
echo "Starting services..."
docker-compose up -d

echo
echo "Waiting for MySQL to be ready..."
max_attempts=60
attempt=1
while [ $attempt -le $max_attempts ]; do
    if docker-compose exec -T mysql mysqladmin ping -h localhost --silent > /dev/null 2>&1; then
        echo "MySQL is ready!"
        break
    fi
    echo "MySQL not ready yet (attempt $attempt/$max_attempts), waiting..."
    sleep 5
    attempt=$((attempt + 1))
done

if [ $attempt -gt $max_attempts ]; then
    echo "ERROR: MySQL failed to start within expected time"
    echo "Checking MySQL logs:"
    docker-compose logs mysql
    exit 1
fi

echo
echo "Waiting for Redis to be ready..."
max_attempts=20
attempt=1
while [ $attempt -le $max_attempts ]; do
    if docker-compose exec -T redis redis-cli ping > /dev/null 2>&1; then
        echo "Redis is ready!"
        break
    fi
    echo "Redis not ready yet (attempt $attempt/$max_attempts), waiting..."
    sleep 2
    attempt=$((attempt + 1))
done

echo
echo "Waiting for applications to be ready..."
sleep 30

echo
echo "Service status:"
docker-compose ps

echo
echo "Nginx is running on port 80"
echo "MySQL is running on port 3306"
echo "Redis is running on port 6379"
echo "App instances are running on ports 8081, 8082, 8083"

echo
echo "Testing service health..."
curl -s http://localhost/health || echo "Failed to connect to service"

echo
echo "Distributed URL Short Service is now running!"
echo "Create short URL: POST http://localhost/create"
echo "Access short URL: GET http://localhost/u/{alias}"
