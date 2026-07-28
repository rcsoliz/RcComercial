using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Ventas.Commands.CrearVenta;

namespace RcComercial.Application.Sync.Commands;

/// <summary>
/// Procesa un lote de ventas offline UNA POR UNA, reusando
/// CrearVentaCommandHandler (misma idempotencia por Id, misma transacción
/// por venta): un ítem rechazado nunca frena ni revierte los demás.
/// </summary>
public class SincronizarVentasCommandHandler(IApplicationDbContext db, IMediator mediator)
    : IRequestHandler<SincronizarVentasCommand, List<ResultadoSyncVentaDto>>
{
    public async Task<List<ResultadoSyncVentaDto>> Handle(SincronizarVentasCommand request, CancellationToken ct)
    {
        var resultados = new List<ResultadoSyncVentaDto>();

        foreach (var item in request.Ventas)
        {
            var yaExistia = await db.Ventas.AnyAsync(v => v.Id == item.Id, ct);

            try
            {
                var comando = new CrearVentaCommand(
                    item.Id, item.ClienteId, item.Descuento, item.Detalles, item.Pagos, item.Receta, item.Numero);
                var venta = await mediator.Send(comando, ct);
                resultados.Add(new ResultadoSyncVentaDto(item.Id, yaExistia ? "duplicada" : "aceptada", null, venta));
            }
            catch (ValidationException ex)
            {
                db.LimpiarSeguimiento();
                // ValidationException(string) (el patrón que usa CrearVentaCommandHandler
                // para "stock insuficiente", etc.) deja Errors vacío: solo Message
                // trae el texto real en ese caso.
                var motivo = ex.Errors.Any() ? string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)) : ex.Message;
                resultados.Add(new ResultadoSyncVentaDto(item.Id, "rechazada", motivo, null));
            }
            catch (Exception)
            {
                db.LimpiarSeguimiento();
                resultados.Add(new ResultadoSyncVentaDto(item.Id, "rechazada", "Error inesperado procesando la venta.", null));
            }
        }

        return resultados;
    }
}
