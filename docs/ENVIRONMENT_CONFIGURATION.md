# Configuración de Entornos (Development vs Production)

## 🎯 Resumen

ASP.NET Core usa la variable de entorno `ASPNETCORE_ENVIRONMENT` para determinar qué archivo de configuración usar:

- **Development**: Carga `appsettings.json` + `appsettings.Development.json`
- **Production**: Carga solo `appsettings.json`

## 📋 Métodos para Cambiar el Entorno

### 1. Usando Visual Studio Code (Recomendado)

En la barra de depuración de VS Code, selecciona uno de los perfiles disponibles:

- **"http (Development)"** → Usa `appsettings.Development.json`
- **"http (Production)"** → Usa solo `appsettings.json`

### 2. Desde la Línea de Comandos

#### Ejecutar en Development (por defecto):
```bash
dotnet run --project DkpSystem
```

#### Ejecutar en Production:
```bash
dotnet run --project DkpSystem --launch-profile "http (Production)"
```

### 3. Variable de Entorno Manual

#### En macOS/Linux:
```bash
# Configurar para la sesión actual
export ASPNETCORE_ENVIRONMENT=Production
dotnet run --project DkpSystem

# Volver a Development
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project DkpSystem
```

#### Inline (una sola ejecución):
```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run --project DkpSystem
```

### 4. En Windows PowerShell:
```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run --project DkpSystem
```

### 5. En Windows CMD:
```cmd
set ASPNETCORE_ENVIRONMENT=Production
dotnet run --project DkpSystem
```

## 🔍 Verificar el Entorno Activo

Cuando inicies la aplicación, verás en los logs:

```
🚀 Starting application in Development environment
📁 Using configuration from appsettings.json + appsettings.Development.json
```

o

```
🚀 Starting application in Production environment
📁 Using configuration from appsettings.json
```

## 📁 Archivos de Configuración

### `appsettings.json`
- Configuración base para **todos** los entornos
- Se carga siempre primero
- Debe contener valores seguros para producción

### `appsettings.Development.json`
- Configuración específica para desarrollo local
- Sobrescribe valores de `appsettings.json`
- Solo se carga cuando `ASPNETCORE_ENVIRONMENT=Development`
- **NO debe incluirse en producción**

### `appsettings.Production.json` (Opcional)
- Puedes crear este archivo para configuración específica de producción
- Se carga automáticamente cuando `ASPNETCORE_ENVIRONMENT=Production`

## ⚙️ Configuración de Perfiles (launchSettings.json)

Los perfiles están definidos en `DkpSystem/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http (Development)": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5073",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "http (Production)": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5073",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  }
}
```

## 🔐 Mejores Prácticas

1. **Nunca** incluyas credenciales reales en `appsettings.Development.json`
2. Usa **User Secrets** para datos sensibles en desarrollo:
   ```bash
   dotnet user-secrets init --project DkpSystem
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "tu-connection-string"
   ```
3. En producción, usa **variables de entorno** o servicios de configuración seguros
4. Mantén `appsettings.Development.json` en `.gitignore` si contiene datos sensibles

## 🚀 Ejemplo de Uso

```bash
# Desarrollo local con base de datos de prueba
dotnet run --project DkpSystem --launch-profile "http (Development)"

# Probar localmente con configuración de producción
dotnet run --project DkpSystem --launch-profile "http (Production)"

# Desplegar en servidor (usa variables de entorno del sistema)
ASPNETCORE_ENVIRONMENT=Production dotnet DkpSystem/DkpSystem.dll
```

## 📚 Referencias

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Use multiple environments in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments)
