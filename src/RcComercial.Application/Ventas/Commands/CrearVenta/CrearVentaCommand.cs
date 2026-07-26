using MediatR;
using RcComercial.Application.Ventas.Dtos;

namespace RcComercial.Application.Ventas.Commands.CrearVenta;

public record CrearVentaDetalleCommand(
    Guid ProductoId, Guid? PresentacionId, decimal Cantidad, decimal PrecioUnitario, decimal Descuento);

public record CrearVentaPagoCommand(string Metodo, decimal Monto, string? ReferenciaQr);

public record CrearVentaRecetaCommand(
    string MedicoNombre,
    string MedicoMatricula,
    string PacienteNombre,
    string? PacienteCi,
    DateOnly FechaReceta,
    string? ImagenUrl);

/// <summary>
/// Id lo genera el cliente (UUID v7): reenviar el mismo Id es idempotente
/// (devuelve la venta ya creada, no duplica). No lleva Total: el backend
/// siempre lo calcula desde las líneas — el precio unitario sí lo manda el
/// cliente/POS (fuente de verdad para poder vender offline).
/// </summary>
public record CrearVentaCommand(
    Guid Id,
    Guid? ClienteId,
    decimal Descuento,
    List<CrearVentaDetalleCommand> Detalles,
    List<CrearVentaPagoCommand> Pagos,
    CrearVentaRecetaCommand? Receta) : IRequest<VentaDto>;
