#!/bin/bash

# Script para limpiar credenciales del historial de Git
# ADVERTENCIA: Este script reescribe el historial de Git

set -e

echo "🔒 Script de Limpieza de Credenciales del Historial de Git"
echo "============================================================"
echo ""
echo "⚠️  ADVERTENCIA: Este script reescribirá el historial de Git"
echo "⚠️  Asegúrate de tener un backup antes de continuar"
echo ""

# Verificar que estamos en un repositorio git
if [ ! -d .git ]; then
    echo "❌ Error: No estás en un repositorio Git"
    exit 1
fi

# Preguntar confirmación
read -p "¿Deseas continuar? (escribe 'SI' para confirmar): " confirm
if [ "$confirm" != "SI" ]; then
    echo "❌ Operación cancelada"
    exit 0
fi

echo ""
echo "📋 Paso 1: Creando backup del repositorio..."
BACKUP_DIR="../dkp_system_backup_$(date +%Y%m%d_%H%M%S)"
cp -r . "$BACKUP_DIR"
echo "✅ Backup creado en: $BACKUP_DIR"

echo ""
echo "📋 Paso 2: Verificando que los cambios actuales estén commiteados..."
if ! git diff-index --quiet HEAD --; then
    echo "⚠️  Hay cambios sin commitear. Commiteando cambios de seguridad..."
    git add DkpSystem/appsettings.json .gitignore SECURITY_INCIDENT_REMEDIATION.md cleanup_git_history.sh
    git commit -m "security: Remove exposed credentials from appsettings.json

- Move connection string to appsettings.Development.json (gitignored)
- Add placeholder in appsettings.json
- Add security incident documentation
- Add cleanup script for git history

SECURITY: Credentials in history must be rotated immediately"
    echo "✅ Cambios commiteados"
else
    echo "✅ No hay cambios pendientes"
fi

echo ""
echo "📋 Paso 3: Limpiando el historial de Git..."
echo "Esto puede tomar varios minutos..."

# Crear archivo temporal con la contraseña a remover
echo "npg_bYxoE8NaL7MS" > /tmp/passwords_to_remove.txt

# Opción 1: Usar BFG si está disponible (más rápido)
if command -v bfg &> /dev/null; then
    echo "✅ Usando BFG Repo-Cleaner (método rápido)..."
    bfg --replace-text /tmp/passwords_to_remove.txt
    git reflog expire --expire=now --all
    git gc --prune=now --aggressive
else
    echo "⚠️  BFG no encontrado, usando git filter-branch (más lento)..."
    echo "💡 Tip: Instala BFG con: brew install bfg"
    
    # Usar filter-branch para remover el archivo del historial
    git filter-branch --force --index-filter \
        "git rm --cached --ignore-unmatch DkpSystem/appsettings.json || true" \
        --prune-empty --tag-name-filter cat -- --all
    
    # Limpiar referencias
    rm -rf .git/refs/original/
    git reflog expire --expire=now --all
    git gc --prune=now --aggressive
fi

# Limpiar archivo temporal
rm -f /tmp/passwords_to_remove.txt

echo ""
echo "✅ Historial limpiado exitosamente"
echo ""
echo "📋 Paso 4: Próximos pasos CRÍTICOS:"
echo ""
echo "1. 🔴 ROTAR CREDENCIALES EN NEON.TECH (URGENTE)"
echo "   - Accede a: https://console.neon.tech/"
echo "   - Cambia la contraseña del usuario neondb_owner"
echo "   - Actualiza DkpSystem/appsettings.Development.json con la nueva contraseña"
echo ""
echo "2. 🔴 Forzar push al repositorio remoto:"
echo "   git push origin --force --all"
echo "   git push origin --force --tags"
echo ""
echo "3. ⚠️  Notificar a todos los colaboradores:"
echo "   - Deben hacer: git pull --rebase"
echo "   - O clonar el repositorio nuevamente"
echo ""
echo "4. 📝 Verificar que la aplicación funciona con las nuevas credenciales"
echo ""
echo "📄 Consulta SECURITY_INCIDENT_REMEDIATION.md para más detalles"
echo ""
