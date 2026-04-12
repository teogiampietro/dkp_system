# Remediación de Incidente de Seguridad - Connection String Expuesta

## 🚨 Problema Identificado

Se subió un commit al repositorio con credenciales sensibles expuestas en `DkpSystem/appsettings.json`:
- Host de base de datos
- Nombre de base de datos
- Usuario
- **Contraseña en texto plano**

## ✅ Acciones Tomadas

### 1. Separación de Configuración Sensible
- ✅ Movida la connection string a `DkpSystem/appsettings.Development.json`
- ✅ Limpiado `DkpSystem/appsettings.json` con placeholder
- ✅ Verificado que `.gitignore` incluye `appsettings.Development.json`

### 2. Archivos Modificados

**`DkpSystem/appsettings.json`** (ahora seguro para commit):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "REPLACE_WITH_YOUR_CONNECTION_STRING"
  }
}
```

**`DkpSystem/appsettings.Development.json`** (ignorado por git):
- Contiene la connection string real
- No se subirá al repositorio

## ⚠️ ACCIONES CRÍTICAS PENDIENTES

### 🔴 URGENTE: Rotar Credenciales de Base de Datos

**Las credenciales expuestas DEBEN ser rotadas inmediatamente:**

1. **Acceder a Neon.tech Dashboard**
   - URL: https://console.neon.tech/
   - Proyecto: `ep-delicate-pine-a86zv591`

2. **Cambiar la contraseña del usuario `neondb_owner`**
   - Navegar a Settings → Database Users
   - Resetear la contraseña del usuario
   - Copiar la nueva contraseña

3. **Actualizar la Connection String Local**
   - Editar `DkpSystem/appsettings.Development.json`
   - Reemplazar el valor de `Password=` con la nueva contraseña
   - **NO COMMITEAR ESTE ARCHIVO**

4. **Verificar Conectividad**
   ```bash
   cd DkpSystem
   dotnet run
   ```

### 🔴 Limpiar el Historial de Git

Las credenciales siguen en el historial de git. Opciones:

#### Opción A: Reescribir Historial (Recomendado si es repositorio privado pequeño)
```bash
# ADVERTENCIA: Esto reescribe el historial
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch DkpSystem/appsettings.json" \
  --prune-empty --tag-name-filter cat -- --all

# Forzar push (requiere permisos)
git push origin --force --all
```

#### Opción B: Usar BFG Repo-Cleaner (Más rápido)
```bash
# Instalar BFG
brew install bfg  # macOS

# Crear archivo con el texto a remover
echo "npg_bYxoE8NaL7MS" > passwords.txt

# Limpiar el repositorio
bfg --replace-text passwords.txt

# Limpiar y forzar push
git reflog expire --expire=now --all
git gc --prune=now --aggressive
git push origin --force --all
```

#### Opción C: Si el repositorio es público o tiene muchos colaboradores
- Considerar crear un nuevo repositorio limpio
- Migrar el código sin el historial comprometido

## 📋 Checklist de Remediación

- [x] Mover credenciales a archivo ignorado por git
- [x] Limpiar archivo de configuración público
- [x] Verificar `.gitignore`
- [ ] **ROTAR CONTRASEÑA EN NEON.TECH**
- [ ] Actualizar connection string local con nueva contraseña
- [ ] Limpiar historial de git
- [ ] Verificar que la aplicación funciona con nuevas credenciales
- [ ] Documentar el incidente (este archivo)
- [ ] Revisar otros archivos por credenciales expuestas

## 🛡️ Prevención Futura

### 1. Variables de Entorno
Considerar usar variables de entorno para producción:

```csharp
// En Program.cs
builder.Configuration.AddEnvironmentVariables();
```

```bash
# En el servidor
export ConnectionStrings__DefaultConnection="Host=...;Password=NEW_PASSWORD"
```

### 2. Azure Key Vault / Secrets Manager
Para producción, usar un gestor de secretos:
- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault

### 3. Pre-commit Hooks
Instalar herramientas para detectar secretos:

```bash
# Instalar git-secrets
brew install git-secrets

# Configurar
git secrets --install
git secrets --register-aws
```

### 4. Escaneo de Repositorio
Usar herramientas como:
- **TruffleHog**: Detecta secretos en el historial
- **GitGuardian**: Monitoreo continuo
- **GitHub Secret Scanning**: Si usas GitHub

## 📞 Contactos de Emergencia

- **Administrador de Base de Datos**: [Agregar contacto]
- **Equipo de Seguridad**: [Agregar contacto]
- **Neon.tech Support**: https://neon.tech/docs/introduction/support

## 📝 Notas Adicionales

- **Fecha del Incidente**: 2026-04-12
- **Repositorio Afectado**: dkp_system
- **Archivo Comprometido**: `DkpSystem/appsettings.json`
- **Credenciales Expuestas**: Password de PostgreSQL en Neon.tech
- **Visibilidad del Repositorio**: [Verificar si es público o privado]

---

**IMPORTANTE**: Este documento debe mantenerse actualizado conforme se completen las acciones de remediación.
