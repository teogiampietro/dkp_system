# Sistema de Código de Invitación

## Descripción

Se ha implementado un sistema de códigos de invitación para controlar el registro de usuarios. Cada guild tiene su propio código de invitación único, y los usuarios deben proporcionar este código al registrarse para ser automáticamente asignados a la guild correspondiente.

## Características

- **Control de acceso**: Solo usuarios con el código correcto pueden registrarse
- **Asignación automática a guild**: Al registrarse con un código válido, el usuario se une automáticamente a esa guild
- **Gestión simple**: Puedes cambiar el código directamente en la base de datos cuando sea necesario
- **Seguridad**: Si un código se filtra, simplemente cámbialo en la base de datos

## Migración de Base de Datos

### Para bases de datos nuevas
No necesitas hacer nada. El sistema creará automáticamente la columna `invitation_code` al ejecutar las migraciones.

### Para bases de datos existentes
Ejecuta la migración 005:

```bash
psql -h <host> -U <usuario> -d <database> -f DkpSystem/Migrations/005_add_invitation_code.sql
```

O si usas el script completo:
```bash
psql -h <host> -U <usuario> -d <database> -f DkpSystem/Migrations/run_all_migrations.sql
```

## Código de Invitación por Defecto

El código de invitación por defecto para la guild "My Guild" es: **`MYGUILD2024`**

⚠️ **IMPORTANTE**: Debes cambiar este código después del despliegue.

## Cómo Cambiar el Código de Invitación

### Opción 1: Directamente en PostgreSQL

```sql
-- Ver el código actual
SELECT id, name, invitation_code FROM guilds;

-- Cambiar el código de invitación
UPDATE guilds 
SET invitation_code = 'TU-NUEVO-CODIGO-AQUI' 
WHERE name = 'My Guild';
```

### Opción 2: Usando psql desde la terminal

```bash
psql -h <host> -U <usuario> -d <database> -c "UPDATE guilds SET invitation_code = 'TU-NUEVO-CODIGO-AQUI' WHERE name = 'My Guild';"
```

### Opción 3: Usando una herramienta GUI
Si usas herramientas como pgAdmin, DBeaver, o TablePlus:
1. Conecta a tu base de datos
2. Abre la tabla `guilds`
3. Edita el campo `invitation_code`
4. Guarda los cambios

## Recomendaciones de Seguridad

1. **Cambia el código regularmente**: Si sospechas que el código se ha filtrado, cámbialo inmediatamente
2. **Usa códigos únicos**: No uses códigos obvios como "123456" o "password"
3. **Formato sugerido**: Usa un formato como `GUILDNAME-YEAR-RANDOM` (ej: `MYGUILD-2024-X7K9`)
4. **Comparte con cuidado**: Solo comparte el código con personas de confianza

## Flujo de Registro

1. El usuario accede a `/register`
2. Completa el formulario con:
   - Email
   - Username
   - Password
   - Confirm Password
   - **Invitation Code** (nuevo campo)
3. El sistema valida el código de invitación
4. Si el código es válido:
   - Se crea el usuario
   - Se asigna automáticamente a la guild correspondiente
   - Se inicia sesión automáticamente
5. Si el código es inválido:
   - Se muestra el error "Invalid invitation code"
   - El usuario no puede registrarse

## Soporte Multi-Guild

Este sistema está preparado para soportar múltiples guilds en el futuro. Cada guild puede tener su propio código de invitación único:

```sql
-- Ejemplo: Agregar una nueva guild con su código
INSERT INTO guilds (name, invitation_code) 
VALUES ('Segunda Guild', 'GUILD2-2024-ABC');
```

Los usuarios que se registren con `GUILD2-2024-ABC` se unirán automáticamente a "Segunda Guild".

## Archivos Modificados

- `DkpSystem/Models/Guild.cs` - Agregado campo `InvitationCode`
- `DkpSystem/Components/Pages/Auth/Register.razor` - Agregado campo de código de invitación
- `DkpSystem/Services/AuthenticationService.cs` - Validación de código y asignación de guild
- `DkpSystem/Data/Repositories/GuildRepository.cs` - Nuevo repositorio para buscar guilds
- `DkpSystem/Program.cs` - Registrado `GuildRepository`
- `DkpSystem/Migrations/005_add_invitation_code.sql` - Nueva migración
- `DkpSystem/Migrations/002_seed_guild.sql` - Actualizado con código por defecto
- `DkpSystem/Migrations/run_all_migrations.sql` - Incluye migración 005

## Troubleshooting

### Error: "Invalid invitation code"
- Verifica que el código esté escrito correctamente (es case-sensitive)
- Verifica que el código exista en la base de datos: `SELECT invitation_code FROM guilds;`

### Error: "Email is already registered"
- El email ya está en uso, intenta con otro email

### La migración falla
- Verifica que tengas permisos de ALTER TABLE en la base de datos
- Si la columna ya existe, la migración la omitirá automáticamente
