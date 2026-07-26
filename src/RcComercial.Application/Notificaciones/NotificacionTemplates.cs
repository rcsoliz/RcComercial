using RcComercial.Application.Panel.Dtos;

namespace RcComercial.Application.Notificaciones;

/// <summary>Texto de cada tipo de notificación, centralizado (antes vivía inline en cada Handler).</summary>
public static class NotificacionTemplates
{
    public static string ReciboVenta(string numero, decimal total) =>
        $"Gracias por su compra. Total: {total:F2} Bs. N° {numero}.";

    public static string Anulacion(string numero, string motivo) =>
        $"Venta N° {numero} anulada. Motivo: {motivo}";

    public static string DiferenciaCaja(decimal declarado, decimal calculado) =>
        $"Diferencia de caja: declarado {declarado:F2}, calculado {calculado:F2}.";

    public static string ResumenDiario(PanelHoyDto r) =>
        $"Resumen del día: {r.NumeroVentas} ventas por Bs {r.TotalVendido:F2} " +
        $"(ticket promedio Bs {r.TicketPromedio:F2}). " +
        $"Anulaciones: {r.NumeroAnulaciones} (Bs {r.MontoAnulaciones:F2}). " +
        $"Descuentos: Bs {r.MontoDescuentos:F2}.";

    public static string StockMinimo(List<ProductoBajoMinimoDto> productos) =>
        "Productos bajo stock mínimo: " +
        string.Join(", ", productos.Select(p => $"{p.Nombre} ({p.StockTotal:F0}/{p.StockMinimo:F0})")) + ".";

    public static string Vencimientos(List<LotePorVencerDto> lotes) =>
        "Lotes por vencer en 30 días: " +
        string.Join(", ", lotes.Select(l => $"{l.ProductoNombre} lote {l.LoteNumero} vence {l.FechaVencimiento:dd/MM}")) + ".";

    public static string PedidoProveedor(List<(string ProductoNombre, decimal Cantidad)> items) =>
        "Pedido: " + string.Join(", ", items.Select(i => $"{i.ProductoNombre} x{i.Cantidad:F0}")) + ".";
}
