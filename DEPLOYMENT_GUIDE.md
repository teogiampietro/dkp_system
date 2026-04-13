# Guía de Despliegue - DKP System

## 🐳 Opción 1: Docker Local (Testing)

### Prerrequisitos
- Docker y Docker Compose instalados

### Pasos
```bash
# 1. Construir y levantar los contenedores
docker-compose up -d

# 2. Ver logs
docker-compose logs -f app

# 3. Acceder a la aplicación
# http://localhost:8080

# 4. Detener
docker-compose down

# 5. Detener y eliminar volúmenes (limpieza completa)
docker-compose down -v
```

---

## 🚂 Opción 2: Railway.app (RECOMENDADO)

### Ventajas
- $5 USD gratis mensual
- PostgreSQL incluido
- Deploy automático
- SSL gratis

### Pasos

1. **Crear cuenta en Railway.app**
   - Visita: https://railway.app
   - Regístrate con GitHub

2. **Crear nuevo proyecto**
   - Click en "New Project"
   - Selecciona "Deploy from GitHub repo"
   - Conecta tu repositorio

3. **Agregar PostgreSQL**
   - Click en "+ New"
   - Selecciona "Database" → "PostgreSQL"
   - Railway creará automáticamente la base de datos

4. **Configurar variables de entorno**
   - En tu servicio de aplicación, ve a "Variables"
   - Agrega:
     ```
     ASPNETCORE_ENVIRONMENT=Production
     ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
     ```
   - Railway automáticamente reemplazará `${{Postgres.DATABASE_URL}}`

5. **Configurar el build (IMPORTANTE)**
   - Railway debe detectar automáticamente el `railway.toml` o `nixpacks.toml`
   - Si no lo hace, ve a Settings → Build y asegúrate que:
     - Builder: Dockerfile
     - Dockerfile Path: `Dockerfile`
   - Archivos de configuración incluidos en el repo:
     - `railway.toml` - Configuración principal de Railway
     - `nixpacks.toml` - Configuración alternativa para Nixpacks
     - `Dockerfile` - Imagen Docker multi-stage optimizada

6. **Deploy**
   - Railway detectará el Dockerfile automáticamente
   - El deploy se ejecutará automáticamente
   - Obtendrás una URL pública con SSL

### ⚠️ Solución de problemas comunes

**Error: "Script start.sh not found" o "Railpack could not determine how to build"**
- Asegúrate que los archivos `railway.toml` y `nixpacks.toml` estén en la raíz del proyecto
- Ve a Settings → Build y selecciona manualmente "Dockerfile" como builder
- Verifica que el `Dockerfile` esté en la raíz del proyecto

### Comandos útiles
```bash
# Instalar Railway CLI (opcional)
npm install -g @railway/cli

# Login
railway login

# Ver logs
railway logs

# Conectar a la base de datos
railway connect postgres
```

---

## 🎨 Opción 3: Render.com

### Ventajas
- Plan gratuito
- PostgreSQL incluido
- SSL gratis

### Desventajas
- Se duerme después de 15 min de inactividad
- Tarda ~30 segundos en despertar

### Pasos

1. **Crear cuenta en Render.com**
   - Visita: https://render.com
   - Regístrate con GitHub

2. **Crear PostgreSQL**
   - Click en "New +"
   - Selecciona "PostgreSQL"
   - Nombre: `dkp-postgres`
   - Plan: Free

3. **Crear Web Service**
   - Click en "New +"
   - Selecciona "Web Service"
   - Conecta tu repositorio
   - Configuración:
     - Name: `dkp-system`
     - Environment: Docker
     - Plan: Free
     - Dockerfile Path: `./Dockerfile`

4. **Variables de entorno**
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=[Copiar de PostgreSQL Internal Database URL]
   ```

5. **Deploy**
   - Click en "Create Web Service"
   - Render construirá y desplegará automáticamente

---

## ✈️ Opción 4: Fly.io

### Ventajas
- Plan gratuito generoso
- Excelente rendimiento
- PostgreSQL incluido

### Desventajas
- Requiere tarjeta de crédito (no cobra en plan free)

### Pasos

1. **Instalar Fly CLI**
   ```bash
   # macOS
   brew install flyctl
   
   # Linux
   curl -L https://fly.io/install.sh | sh
   
   # Windows
   powershell -Command "iwr https://fly.io/install.ps1 -useb | iex"
   ```

2. **Login**
   ```bash
   fly auth login
   ```

3. **Crear app**
   ```bash
   fly launch
   # Responde las preguntas:
   # - App name: dkp-system (o el que prefieras)
   # - Region: Elige la más cercana
   # - PostgreSQL: Yes
   # - Redis: No
   ```

4. **Configurar variables**
   ```bash
   fly secrets set ASPNETCORE_ENVIRONMENT=Production
   ```

5. **Deploy**
   ```bash
   fly deploy
   ```

6. **Ver logs**
   ```bash
   fly logs
   ```

---

## 🔧 Configuración de Producción

### Variables de Entorno Requeridas

```bash
# Obligatorias
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=xxx;Port=5432;Database=xxx;Username=xxx;Password=xxx

# Opcionales (recomendadas)
ASPNETCORE_URLS=http://+:8080
```

### Formato de Connection String para PostgreSQL

```
Host=hostname;Port=5432;Database=dkp_system;Username=user;Password=password;SSL Mode=Require
```

**Nota:** Algunas plataformas proveen la URL en formato `postgresql://`, necesitas convertirla:
```
postgresql://user:password@host:5432/database
↓
Host=host;Port=5432;Database=database;Username=user;Password=password
```

---

## 🔒 Seguridad

### Antes de desplegar:

1. **Cambiar contraseñas por defecto**
   - En `docker-compose.yml` cambia `dkp_password_change_in_production`
   - En producción usa variables de entorno

2. **Usar secretos**
   - No commitear contraseñas al repositorio
   - Usar variables de entorno de la plataforma

3. **SSL/HTTPS**
   - Railway, Render y Fly.io proveen SSL automático
   - Asegúrate que esté habilitado

4. **Actualizar appsettings.Production.json**
   - Usa variables de entorno en lugar de valores hardcodeados

---

## 📊 Monitoreo

### Railway
```bash
railway logs --tail
```

### Render
- Dashboard → Logs (en tiempo real)

### Fly.io
```bash
fly logs
fly status
```

---

## 🐛 Troubleshooting

### Error: "Connection refused" a PostgreSQL
- Verifica que el servicio de PostgreSQL esté corriendo
- Revisa el connection string
- Asegúrate que el host sea correcto (en Docker: `postgres`, en cloud: URL provista)

### Error: "Port already in use"
```bash
# Cambiar puerto en docker-compose.yml
ports:
  - "8081:8080"  # Usa 8081 en lugar de 8080
```

### La aplicación no inicia
```bash
# Ver logs detallados
docker-compose logs app

# O en la plataforma cloud, revisar logs en el dashboard
```

### Migraciones no se ejecutan
- Las migraciones se ejecutan automáticamente al iniciar
- Verifica los logs para ver si hay errores
- Asegúrate que el connection string sea correcto

---

## 📝 Checklist de Despliegue

- [ ] Código pusheado a GitHub
- [ ] Dockerfile creado
- [ ] Variables de entorno configuradas
- [ ] PostgreSQL creado en la plataforma
- [ ] Connection string configurado
- [ ] Deploy ejecutado exitosamente
- [ ] Migraciones ejecutadas
- [ ] Usuario admin creado (credenciales por defecto: admin@dkp.com / Admin123!)
- [ ] SSL habilitado
- [ ] Aplicación accesible públicamente

---

## 🎯 Recomendación Final

**Para empezar: Railway.app**
- Más fácil de configurar
- Mejor experiencia de desarrollo
- No se duerme como Render
- $5 USD gratis es suficiente para empezar

**Para escalar: Fly.io**
- Mejor rendimiento
- Más control
- Plan gratuito más generoso
- Requiere más configuración inicial

---

## 📚 Recursos Adicionales

- [Railway Docs](https://docs.railway.app/)
- [Render Docs](https://render.com/docs)
- [Fly.io Docs](https://fly.io/docs/)
- [Docker Docs](https://docs.docker.com/)
- [.NET Docker Guide](https://learn.microsoft.com/en-us/dotnet/core/docker/introduction)
