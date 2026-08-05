using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Ventas;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Application.Ventas.Dtos;
using RcComercial.Domain.Common;

namespace RcComercial.Application.Proformas.Commands.ConvertirProformaEnVenta;

/// <summary>
/// NO duplica la lógica de reserva de stock/FEFO/kardex: arma un
/// CrearVentaCommand a partir de las líneas de la proforma y lo manda vía
/// IMediator a CrearVentaCommandHandler sin modificarlo. Esa es la única
/// vía por la que una proforma convertida termina descontando stock de
/// verdad — CrearProformaCommandHandler nunca lo toca.
/// </summary>
public class ConvertirProformaEnVentaCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<ConvertirProformaEnVentaCommand, VentaDto>
{
    public async Task<VentaDto> Handle(ConvertirProformaEnVentaCommand request, CancellationToken ct)
    {
        var proforma = await db.Proformas
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == request.ProformaId, ct)
            ?? throw new ValidationException("La proforma no existe.");

        if (proforma.Estado == EstadosProforma.Convertida)
        {
            // Idempotente: reenviar la misma conversión no crea una segunda venta.
            var ventaExistente = await db.Ventas
                .Include(v => v.Detalles)
                .Include(v => v.Pagos)
                .FirstAsync(v => v.Id == proforma.VentaId, ct);
            return VentaMapper.ToDto(ventaExistente);
        }
        if (proforma.Estado == EstadosProforma.Rechazada)
            throw new ValidationException("No se puede convertir una proforma rechazada.");

        var detalles = proforma.Detalles
            .Select(d => new CrearVentaDetalleCommand(d.ProductoId, d.PresentacionId, d.Cantidad, d.PrecioUnitario, d.Descuento))
            .ToList();

        var ventaDto = await mediator.Send(new CrearVentaCommand(
            request.VentaId, proforma.ClienteId, request.DescuentoAdicional, detalles, request.Pagos,
            Receta: null, Numero: null, VehiculoId: proforma.VehiculoId), ct);

        proforma.Estado = EstadosProforma.Convertida;
        proforma.VentaId = ventaDto.Id;
        await db.SaveChangesAsync(ct);

        return ventaDto;
    }
}
