# DKP System

Sistema de gestión de DKP (Dragon Kill Points) desarrollado con Blazor Server y PostgreSQL.

## Descripción

DKP System es una aplicación web para gestionar puntos DKP en guilds de juegos MMORPG. Permite administrar eventos, subastas de items, y el seguimiento de puntos de los miembros del guild.

## Tecnologías

- **Framework**: .NET 8.0 / Blazor Server
- **Base de datos**: PostgreSQL
- **ORM**: Dapper
- **Autenticación**: ASP.NET Core Identity con Cookie Authentication

## Características

- ✅ Sistema de autenticación (Login/Register)
- ✅ Gestión de usuarios y roles
- ✅ Arquitectura modular con servicios y repositorios
- ✅ Migraciones de base de datos
- ✅ Tests unitarios
- 🚧 Gestión de eventos DKP
- 🚧 Sistema de subastas
- 🚧 Dashboard de estadísticas

## Estructura del Proyecto

```
dkp_system/
├── DkpSystem/              # Aplicación principal Blazor
│   ├── Components/         # Componentes Razor
│   ├── Data/              # Acceso a datos y repositorios
│   ├── Models/            # Modelos de dominio
│   ├── Services/          # Servicios de negocio
│   └── Migrations/        # Scripts SQL de migración
├── DkpSystem.Tests/       # Proyecto de tests
└── HashGenerator/         # Utilidad para generar hashes
```

## Requisitos Previos

- .NET 8.0 SDK
- PostgreSQL 12+
- Visual Studio 2022, Rider, o VS Code

## Configuración

1. Clonar el repositorio:
```bash
git clone https://github.com/TU_USUARIO/dkp_system.git
cd dkp_system
```

2. Configurar la base de datos PostgreSQL y actualizar la cadena de conexión en `appsettings.json`

3. Ejecutar las migraciones:
```bash
psql -U tu_usuario -d tu_base_de_datos -f DkpSystem/Migrations/run_all_migrations.sql
```

4. Ejecutar la aplicación:
```bash
cd DkpSystem
dotnet run
```

5. Acceder a la aplicación en `https://localhost:5001`

## Credenciales por Defecto

- **Usuario**: admin
- **Contraseña**: Admin123!

## Tests

Ejecutar los tests unitarios:
```bash
dotnet test
```

## Documentación

- [Documentación del Sistema](DKP_SYSTEM_DOC.md)
- [Playbook de Desarrollo](DKP_DEVELOPMENT_PLAYBOOK.md)

## Licencia

Este proyecto es de código abierto y está disponible bajo la licencia MIT.

## Autor

Desarrollado por Teo Giampietro
