using MediatR;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Application.Ventas.Dtos;

namespace RcComercial.Application.Proformas.Commands.ConvertirProformaEnVenta;

/// <summary>
/// VentaId lo genera el CLIENTE (Uuid7), mismo contrato de idempotencia que
/// ya usa CrearVentaCommand.Id para ventas offline: si esta conversión se
/// reintenta después de que la venta ya se creó pero antes de marcar la
/// proforma como CONVERTIDA, CrearVentaCommandHandler devuelve la venta ya
/// existente en vez de duplicarla.
/// </summary>
public record ConvertirProformaEnVentaCommand(
    Guid ProformaId,
    Guid VentaId,
    List<CrearVentaPagoCommand> Pagos,
    decimal DescuentoAdicional = 0) : IRequest<VentaDto>;
