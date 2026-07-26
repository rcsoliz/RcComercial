# Contexto del proyecto para Claude Code

## Qué es
Sistema comercial SaaS multi-tenant para pymes bolivianas (almacenes,
farmacias, ferreterías). Backend .NET 8 Clean Architecture + PostgreSQL.
Frontend futuro: Vue 3 PWA offline-first. Facturación SIAT (Bolivia).

## Convenciones
- Dominio en español (Producto, Venta, MovimientoInventario) — es deliberado.
- BD en snake_case vía UseSnakeCaseNamingConvention; no escribir HasColumnName.
- Decimales: montos (14,2) por convención global; cantidades (14,3); costos
  y factores (14,4) — ya configurado.
- Estados como strings alineados a CHECK constraints (Domain/Common/Constantes.cs).
- UUID v7 con Domain/Common/Uuid7.cs (no usar Guid.NewGuid en entidades nuevas).

## Reglas que NUNCA se rompen
1. EmpresaId viene del claim del JWT, jamás de un DTO/request.
2. Toda entidad de negocio nueva implementa ITenantEntity (salvo catálogos
   globales: Rubro, Permiso, UnidadMedida, ProductoMaestro).
3. Stock + MovimientoInventario en la misma transacción, sin triggers.
4. Soft delete siempre (Activo = false); DELETE físico prohibido.
5. Autorización por permiso granular (codigo 'modulo.accion'), validada en
   el backend con policies; el frontend solo oculta botones.

## Roadmap (en orden)
1. Auth: login, JWT con claims (empresa_id, sucursal_id, permiso[]),
   refresh token con rotación, lockout. Tablas ya existen.
2. Módulo productos: CRUD + presentaciones + búsqueda trigram + import
   desde producto_maestro por código de barras.
3. Módulo ventas (POS): venta con FEFO para lotes, secuencia atómica de
   numeración, pagos (efectivo/QR), sesión de caja.
4. Panel del dueño: resumen del día, alertas de anulaciones/diferencias.
5. Notificaciones WhatsApp (cola en tabla notificacion).
6. Sugerido de compra. 7. Facturación SIAT. 8. PWA offline.

## Comandos
- Compilar: `dotnet build`
- Migración: `dotnet ef migrations add <Nombre> -p src/RcComercial.Infrastructure -s src/RcComercial.Api`
- La tabla auditoria está ExcludeFromMigrations: se crea con SQL
  (database/02_revision_dba.sql) porque es particionada.

## Preferencias del desarrollador
- Cambios paso a paso, con revisión en GitHub entre pasos.
- Parches mínimos y compatibles hacia atrás; no refactorizar sin pedirlo.
