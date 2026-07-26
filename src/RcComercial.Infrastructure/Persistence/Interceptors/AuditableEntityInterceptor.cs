using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;

namespace RcComercial.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Llena CreadoEn/CreadoPor/ActualizadoEn/ActualizadoPor automáticamente
/// en cada SaveChanges para toda entidad IAuditable.
/// </summary>
public class AuditableEntityInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;
        var ahora = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreadoEn = ahora;
                entry.Entity.CreadoPor = currentUser.UsuarioId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ActualizadoEn = ahora;
                entry.Entity.ActualizadoPor = currentUser.UsuarioId;
            }
        }
    }
}
