# RcComercial

Sistema comercial multi-rubro (almacén / farmacia / ferretería) para pymes de Bolivia.
Multi-tenant, offline-first, con facturación SIAT como objetivo.

## Stack
- Backend: .NET 8, Clean Architecture, EF Core 8 + PostgreSQL (Npgsql)
- Frontend (fase posterior): Vue 3 + Vite + Tailwind (PWA)
- Convención BD: snake_case automático vía EFCore.NamingConventions

## Estructura
```
src/
  RcComercial.Domain          Entidades, interfaces y constantes (sin dependencias)
  RcComercial.Application     Casos de uso e interfaces (ICurrentUserService)
  RcComercial.Infrastructure  AppDbContext, configuraciones EF, interceptores
  RcComercial.Api             Minimal API + Swagger
database/
  00_esquema_base.sql         Esquema completo de referencia
  01_rubro_y_seguridad.sql    Rubro como catálogo + RBAC + auditoría
  02_revision_dba.sql         UUID v7, particionado, índices, seeds
```

## Primeros pasos
```bash
dotnet restore
dotnet build

# Crear la BD (requiere PostgreSQL local o Railway):
#   opción recomendada: migraciones EF
dotnet tool install --global dotnet-ef
dotnet ef migrations add Inicial -p src/RcComercial.Infrastructure -s src/RcComercial.Api
dotnet ef database update -p src/RcComercial.Infrastructure -s src/RcComercial.Api

# Luego aplicar manualmente los extras que EF no genera
# (extensión pg_trgm, función uuid_v7, tabla auditoria particionada,
#  índices trigram/BRIN y seeds de rubro/permiso/rol):
# ver database/02_revision_dba.sql y database/01_rubro_y_seguridad.sql

dotnet run --project src/RcComercial.Api
# Swagger: https://localhost:5001/swagger  |  Health: /health
```

## Reglas de arquitectura (no negociables)
1. `EmpresaId` sale SIEMPRE del JWT, jamás del request. El query filter
   global del AppDbContext filtra toda entidad `ITenantEntity`.
2. Stock y kardex (`movimiento_inventario`) se actualizan en la MISMA
   transacción. Sin triggers.
3. Nada se elimina físicamente: soft delete (`Activo = false`).
4. Todo stock/cantidad_base se expresa en unidad base; las presentaciones
   son factores de conversión.
5. IDs de venta se generan en el cliente con UUID v7 (modo offline).
