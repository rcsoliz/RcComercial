using FluentValidation;
using MediatR;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Application.Ventas.Dtos;

namespace RcComercial.Application.Sync.Commands;

/// <summary>Espejo de CrearVentaCommand, pero con Numero obligatorio: toda
/// venta que llega por este lote viene de un rango reservado offline.</summary>
public record SyncVentaItem(
    Guid Id,
    string Numero,
    Guid? ClienteId,
    decimal Descuento,
    List<CrearVentaDetalleCommand> Detalles,
    List<CrearVentaPagoCommand> Pagos,
    CrearVentaRecetaCommand? Receta);

/// <summary>Estado: "aceptada" (recién creada), "duplicada" (el Id ya existía:
/// no se tocó nada, se devuelve la venta tal como quedó la primera vez) o
/// "rechazada" (Motivo explica por qué; nunca frena el resto del lote).</summary>
public record ResultadoSyncVentaDto(Guid Id, string Estado, string? Motivo, VentaDto? Venta);

public record SincronizarVentasCommand(List<SyncVentaItem> Ventas) : IRequest<List<ResultadoSyncVentaDto>>;

public class SincronizarVentasCommandValidator : AbstractValidator<SincronizarVentasCommand>
{
    public SincronizarVentasCommandValidator()
    {
        RuleFor(x => x.Ventas).NotEmpty();
        RuleForEach(x => x.Ventas).ChildRules(v => v.RuleFor(x => x.Numero).NotEmpty());
    }
}
