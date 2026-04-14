# Test Database Setup

## Problema Actual

Los tests de autenticación fallan con el error:
```
Npgsql.PostgresException : 42703: column "invitation_code" does not exist
```

Esto ocurre porque la base de datos de test (`dkp_test`) no tiene la nueva columna `invitation_code` que se agregó en la migración 005.

## Solución

Necesitas ejecutar la migración 005 en tu base de datos de test antes de ejecutar los tests.

### Opción 1: Usando Docker (Recomendado)

Si estás usando Docker para la base de datos de test:

```bash
# Ejecutar la migración en el contenedor
docker exec -i dkp_postgres psql -U postgres -d dkp_test < DkpSystem/Migrations/005_add_invitation_code.sql
```

### Opción 2: Usando psql directamente

Si tienes PostgreSQL instalado localmente:

```bash
# Ejecutar la migración
psql -h localhost -p 5433 -U postgres -d dkp_test -f DkpSystem/Migrations/005_add_invitation_code.sql
```

### Opción 3: Recrear la base de datos de test

Si prefieres empezar desde cero:

```bash
# Eliminar y recrear la base de datos de test
psql -h localhost -p 5433 -U postgres -c "DROP DATABASE IF EXISTS dkp_test;"
psql -h localhost -p 5433 -U postgres -c "CREATE DATABASE dkp_test;"

# Ejecutar todas las migraciones
psql -h localhost -p 5433 -U postgres -d dkp_test -f DkpSystem/Migrations/run_all_migrations.sql
```

### Opción 4: Usando una herramienta GUI

Si usas pgAdmin, DBeaver, TablePlus u otra herramienta:

1. Conecta a la base de datos `dkp_test`
2. Abre el archivo `DkpSystem/Migrations/005_add_invitation_code.sql`
3. Ejecuta el script SQL

## Verificar que la Migración se Aplicó

Después de ejecutar la migración, verifica que la columna existe:

```sql
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'guilds' AND column_name = 'invitation_code';
```

Deberías ver:
```
 column_name     | data_type
-----------------+-----------
 invitation_code | character varying
```

## Ejecutar los Tests

Una vez aplicada la migración, ejecuta los tests:

```bash
dotnet test DkpSystem.Tests/DkpSystem.Tests.csproj --filter "FullyQualifiedName~AuthenticationTests"
```

## Nota para CI/CD

Si estás usando CI/CD, asegúrate de que el pipeline ejecute todas las migraciones antes de correr los tests:

```yaml
# Ejemplo para GitHub Actions
- name: Run migrations
  run: |
    psql -h localhost -U postgres -d dkp_test -f DkpSystem/Migrations/run_all_migrations.sql
  env:
    PGPASSWORD: postgres

- name: Run tests
  run: dotnet test
```
