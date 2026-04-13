# Guía de Testing - DKP System

## 🚀 Ejecutar Tests Localmente

### Opción 1: Script Automatizado (Recomendado)

Simplemente ejecuta:

```bash
./run-tests.sh
```

Este script automáticamente:
- ✅ Verifica que Docker esté corriendo
- ✅ Inicia el contenedor de PostgreSQL para tests
- ✅ Prepara la base de datos con las migraciones
- ✅ Ejecuta todos los tests
- ✅ Muestra resultados con colores

### Opción 2: Manual

Si prefieres ejecutar los pasos manualmente:

```bash
# 1. Iniciar la base de datos de tests
docker-compose up -d postgres_test

# 2. Preparar la base de datos
docker exec -i dkp_postgres_test psql -U postgres -d dkp_test < DkpSystem/Migrations/run_all_migrations.sql

# 3. Ejecutar los tests
TEST_CONNECTION_STRING="Host=localhost;Port=5433;Database=dkp_test;Username=postgres;Password=postgres" \
    dotnet test DkpSystem.Tests/DkpSystem.Tests.csproj --verbosity normal
```

## 🛑 Detener el Contenedor de Tests

Cuando termines de trabajar:

```bash
docker-compose down postgres_test
```

O para detener todos los contenedores:

```bash
docker-compose down
```

## 📊 Información de la Base de Datos de Tests

- **Host:** localhost
- **Puerto:** 5433 (diferente del puerto de producción 5432)
- **Base de datos:** dkp_test
- **Usuario:** postgres
- **Contraseña:** postgres

## 🔧 Configuración

### Tests en Paralelo

Los tests están configurados para ejecutarse **secuencialmente** (no en paralelo) para evitar conflictos de base de datos. Esta configuración está en [`xunit.runner.json`](DkpSystem.Tests/xunit.runner.json).

### Variables de Entorno

Los tests usan la variable de entorno `TEST_CONNECTION_STRING`. Si no está definida, usan valores por defecto que apuntan a `localhost:5433`.

## 📝 Suites de Tests

El proyecto incluye 58 tests organizados en:

- **AuctionTests** (19 tests) - Funcionalidad de subastas
- **EventManagementTests** (14 tests) - Gestión de eventos y DKP
- **AuthenticationTests** (5 tests) - Login, registro y autenticación
- **MemberManagementTests** (8 tests) - Gestión de miembros
- **ModelTests** (8 tests) - Validación de modelos
- **DbConnectionFactoryTests** (2 tests) - Conexión a base de datos
- **Otros** (2 tests)

## ⚠️ Requisitos

- Docker Desktop instalado y corriendo
- .NET 8.0 SDK
- Conexión a internet (para descargar la imagen de PostgreSQL la primera vez)

## 🐛 Troubleshooting

### Error: "Docker no está corriendo"
Inicia Docker Desktop y espera a que esté completamente iniciado.

### Error: "Connection refused"
El contenedor de PostgreSQL puede tardar unos segundos en estar listo. Espera 5-10 segundos y vuelve a intentar.

### Error: "Port 5433 already in use"
Otro proceso está usando el puerto 5433. Detén ese proceso o cambia el puerto en `docker-compose.yml`.

### Tests fallan con errores de base de datos
Limpia y reinicia el contenedor:
```bash
docker-compose down postgres_test
docker volume rm dkp_system_postgres_test_data
./run-tests.sh
```

## 🚀 CI/CD

Para configurar tests automáticos en GitHub Actions, crea `.github/workflows/tests.yml`:

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_DB: dkp_test
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
        ports:
          - 5433:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Run migrations
      run: |
        PGPASSWORD=postgres psql -h localhost -p 5433 -U postgres -d dkp_test < DkpSystem/Migrations/run_all_migrations.sql
    
    - name: Run tests
      env:
        TEST_CONNECTION_STRING: "Host=localhost;Port=5433;Database=dkp_test;Username=postgres;Password=postgres"
      run: dotnet test DkpSystem.Tests/DkpSystem.Tests.csproj --verbosity normal
```

## 📚 Más Información

- [Documentación del Sistema](docs/DKP_SYSTEM_DOC.md)
- [Guía de Desarrollo](docs/DKP_DEVELOPMENT_PLAYBOOK.md)
