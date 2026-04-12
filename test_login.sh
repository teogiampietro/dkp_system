#!/bin/bash

# Script para probar el login directamente
echo "=== Testing Login Flow ==="
echo ""

# Conectar a la base de datos y verificar el usuario admin
echo "1. Checking admin user in database:"
docker exec -it dkp-postgres psql -U dkp_user -d dkp_system -c "SELECT id, email, username, role, active, LENGTH(password_hash) as hash_length FROM users WHERE email = 'admin@dkp.local';"

echo ""
echo "2. Attempting to login via API..."
curl -X POST http://localhost:5073/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@dkp.local","password":"Admin123!"}' \
  -v

echo ""
echo "=== Test Complete ==="
