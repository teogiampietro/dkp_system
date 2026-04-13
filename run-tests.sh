#!/bin/bash

# Script para ejecutar los tests del proyecto DKP System
# Asegura que la base de datos de tests esté corriendo y ejecuta todos los tests

set -e  # Detener si hay algún error

echo "🚀 Iniciando tests del DKP System..."
echo ""

# Colores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Verificar si Docker está corriendo
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Error: Docker no está corriendo${NC}"
    echo "Por favor inicia Docker Desktop y vuelve a intentar"
    exit 1
fi

# Iniciar el contenedor de base de datos de tests
echo -e "${BLUE}📦 Iniciando contenedor de PostgreSQL para tests...${NC}"
docker-compose up -d postgres_test

# Esperar a que la base de datos esté lista
echo -e "${BLUE}⏳ Esperando a que PostgreSQL esté listo...${NC}"
sleep 3

# Verificar que el contenedor esté corriendo
if ! docker ps | grep -q dkp_postgres_test; then
    echo -e "${RED}❌ Error: El contenedor de PostgreSQL no está corriendo${NC}"
    exit 1
fi

# Limpiar y preparar la base de datos
echo -e "${BLUE}🗄️  Preparando base de datos de tests...${NC}"
docker exec -i dkp_postgres_test psql -U postgres -d dkp_test -c "TRUNCATE TABLE auction_bids, auction_items, auctions, dkp_earnings, event_reward_lines, events, users, guilds CASCADE;" > /dev/null 2>&1 || true
docker exec -i dkp_postgres_test psql -U postgres -d dkp_test < DkpSystem/Migrations/run_all_migrations.sql > /dev/null 2>&1

echo -e "${GREEN}✅ Base de datos lista${NC}"
echo ""

# Ejecutar los tests
echo -e "${BLUE}🧪 Ejecutando tests...${NC}"
echo ""

TEST_CONNECTION_STRING="Host=localhost;Port=5433;Database=dkp_test;Username=postgres;Password=postgres" \
    dotnet test DkpSystem.Tests/DkpSystem.Tests.csproj --verbosity normal

# Capturar el código de salida
TEST_EXIT_CODE=$?

echo ""
if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ Todos los tests pasaron exitosamente!${NC}"
else
    echo -e "${RED}❌ Algunos tests fallaron${NC}"
fi

echo ""
echo -e "${YELLOW}💡 Tip: El contenedor de PostgreSQL seguirá corriendo.${NC}"
echo -e "${YELLOW}   Para detenerlo: docker-compose down postgres_test${NC}"

exit $TEST_EXIT_CODE
