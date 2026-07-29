using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Empresas;

namespace RcComercial.Application.Empresas.Queries.ObtenerEmpresaActual;

public class ObtenerEmpresaActualQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ObtenerEmpresaActualQuery, EmpresaActualDto?>
{
    public async Task<EmpresaActualDto?> Handle(ObtenerEmpresaActualQuery request, CancellationToken ct)
    {
        var empresa = await db.Empresas
            .Include(e => e.Rubro)
            .FirstOrDefaultAsync(e => e.Id == currentUser.EmpresaId, ct);

        return empresa is null
            ? null
            : new EmpresaActualDto(
                empresa.Id, empresa.Nombre, empresa.Nit, empresa.TelefonoWhatsapp, empresa.RubroId, empresa.Rubro.Nombre,
                empresa.Rubro.UsaLotesPorDefecto, empresa.Rubro.UsaControlados,
                empresa.Rubro.UsaFichaFarmacia, empresa.Rubro.UsaDecimalesPorDefecto);
    }
}
