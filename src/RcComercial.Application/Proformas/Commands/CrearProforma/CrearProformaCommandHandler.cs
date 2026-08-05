using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Proformas;
using RcComercial.Application.Proformas.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Proformas.Commands.CrearProforma;

/// <summary>
/// A diferencia de CrearCompraCommandHandler/CrearVentaCommandHandler, esta
/// NUNCA toca Stock/Lote/MovimientoInventario: una proforma es una
/// cotización, no un movimiento de inventario. Eso pasa recién al convertir
/// (ver ConvertirProformaEnVentaCommandHandler, que reusa CrearVentaCommandHandler).
/// </summary>
public class CrearProformaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearProformaCommand, ProformaDto>
{
    public async Task<ProformaDto> Handle(CrearProformaCommand request, CancellationToken ct) =>
        // Solo por SiguienteNumeroAsync (SQL crudo, se ejecuta de inmediato):
        // sin esta transacción explícita, un número consumido podría quedar
        // "gastado" aunque el resto de la proforma falle después.
        await db.EjecutarEnTransaccionAsync(async ct => await CrearAsync(request, ct), ct);

    private async Task<ProformaDto> CrearAsync(CrearProformaCommand request, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var usuarioId = currentUser.UsuarioId!.Value;

        var sucursalId = await SucursalResolver.ResolverAsync(db, currentUser, request.SucursalId, ct)
            ?? throw new ValidationException("No se pudo determinar la sucursal: especifique 'sucursalId'.");

        var productoIds = request.Detalles.Select(d => d.ProductoId).Distinct().ToList();
        var productos = await db.Productos
            .Include(p => p.Presentaciones)
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var numero = await db.SiguienteNumeroAsync(empresaId, sucursalId, "PROFORMA", ct);

        var proforma = new Proforma
        {
            EmpresaId = empresaId,
            SucursalId = sucursalId,
            ClienteId = request.ClienteId,
            VehiculoId = request.VehiculoId,
            ValidaHasta = request.ValidaHasta,
            Numero = numero.ToString("00000000"),
            UsuarioId = usuarioId,
        };

        foreach (var d in request.Detalles)
        {
            if (!productos.TryGetValue(d.ProductoId, out var producto))
                throw new ValidationException($"El producto {d.ProductoId} no existe.");

            var factor = 1m;
            if (d.PresentacionId is { } presentacionId)
            {
                var presentacion = producto.Presentaciones.FirstOrDefault(p => p.Id == presentacionId)
                    ?? throw new ValidationException(
                        $"La presentación indicada no pertenece a '{producto.Nombre}'.");
                factor = presentacion.Factor;
            }
            var cantidadBase = d.Cantidad * factor;

            proforma.Detalles.Add(new ProformaDetalle
            {
                ProformaId = proforma.Id,
                ProductoId = producto.Id,
                PresentacionId = d.PresentacionId,
                Cantidad = d.Cantidad,
                CantidadBase = cantidadBase,
                PrecioUnitario = d.PrecioUnitario,
                Descuento = d.Descuento,
                Total = (d.Cantidad * d.PrecioUnitario) - d.Descuento,
            });
        }

        proforma.Subtotal = proforma.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        proforma.Descuento = proforma.Detalles.Sum(d => d.Descuento);
        proforma.Total = proforma.Subtotal - proforma.Descuento;

        db.Proformas.Add(proforma);
        await db.SaveChangesAsync(ct);

        return ProformaMapper.ToDto(proforma);
    }
}
