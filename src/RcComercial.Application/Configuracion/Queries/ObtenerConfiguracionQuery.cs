using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;

namespace RcComercial.Application.Configuracion.Queries;

public record ObtenerConfiguracionQuery : IRequest<ConfiguracionDto>;

public class ObtenerConfiguracionQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ObtenerConfiguracionQuery, ConfiguracionDto>
{
    public async Task<ConfiguracionDto> Handle(ObtenerConfiguracionQuery request, CancellationToken ct)
    {
        var valores = await db.EmpresaConfiguraciones
            .Where(c => c.EmpresaId == currentUser.EmpresaId)
            .ToDictionaryAsync(c => c.Clave, c => c.Valor, ct);

        var permiteStockNegativo = valores.TryGetValue(ClavesConfiguracion.VentaPermiteStockNegativo, out var v1)
            && bool.TryParse(v1, out var b) && b;
        var horaResumen = valores.TryGetValue(ClavesConfiguracion.NotificacionesHoraResumen, out var v2)
            && int.TryParse(v2, out var h) ? h : 21;

        return new ConfiguracionDto(permiteStockNegativo, horaResumen);
    }
}
