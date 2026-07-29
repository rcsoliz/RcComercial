namespace RcComercial.Domain.Common;

/// <summary>Valores permitidos, alineados con los CHECK constraints de la BD.</summary>
public static class EstadosVenta
{
    public const string Completada = "COMPLETADA";
    public const string Anulada = "ANULADA";
}

public static class EstadosSiat
{
    public const string SinFactura = "SIN_FACTURA";
    public const string Pendiente = "PENDIENTE";
    public const string Emitida = "EMITIDA";
    public const string Rechazada = "RECHAZADA";
    public const string Contingencia = "CONTINGENCIA";
}

public static class MetodosPago
{
    public const string Efectivo = "EFECTIVO";
    public const string Qr = "QR";
    public const string Tarjeta = "TARJETA";
    public const string Transferencia = "TRANSFERENCIA";
}

public static class TiposDocumentoCliente
{
    public const string Ci = "CI";
    public const string Nit = "NIT";
    public const string Cex = "CEX"; // carnet de extranjero
    public const string Pas = "PAS"; // pasaporte

    public static readonly string[] Todos = [Ci, Nit, Cex, Pas];
}

public static class TiposMovimiento
{
    public const string Compra = "COMPRA";
    public const string Venta = "VENTA";
    public const string Ajuste = "AJUSTE";
    public const string Merma = "MERMA";
    public const string Vencimiento = "VENCIMIENTO";
    public const string Transferencia = "TRANSFERENCIA";
    public const string Devolucion = "DEVOLUCION";
    public const string InventarioInicial = "INVENTARIO_INICIAL";
}

/// <summary>Claves de empresa_configuracion (clave-valor): agregar una acá no pide ALTER TABLE.</summary>
public static class ClavesConfiguracion
{
    public const string VentaPermiteStockNegativo = "venta.permite_stock_negativo";
    public const string NotificacionesHoraResumen = "notificaciones.hora_resumen";
}

public static class TiposNotificacion
{
    public const string FacturaCliente = "FACTURA_CLIENTE";
    public const string Anulacion = "ANULACION";
    public const string DiferenciaCaja = "DIFERENCIA_CAJA";
    public const string ResumenDiario = "RESUMEN_DIARIO";
    public const string StockMinimo = "STOCK_MINIMO";
    public const string Vencimientos = "VENCIMIENTOS";
    public const string PedidoProveedor = "PEDIDO_PROVEEDOR";
}

public static class EstadosNotificacion
{
    public const string Pendiente = "PENDIENTE";
    public const string Enviada = "ENVIADA";
    public const string Fallida = "FALLIDA";
}

public static class ClasificacionesFarmacia
{
    public const string Libre = "LIBRE";
    public const string Receta = "RECETA";
    public const string Psicotropico = "PSICOTROPICO";
    public const string Estupefaciente = "ESTUPEFACIENTE";
}

/// <summary>Roles de sistema precargados (empresa_id NULL, es_sistema=true, no editables).</summary>
public static class RolesSistema
{
    public static readonly Guid Dueno = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Encargado = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    public static readonly Guid Vendedor = Guid.Parse("a0000000-0000-0000-0000-000000000003");
}

public static class Permisos
{
    public const string VentasCrear = "ventas.crear";
    public const string VentasAnular = "ventas.anular";
    public const string VentasDescuento = "ventas.descuento";
    public const string VentasVerHistorial = "ventas.ver_historial";
    public const string CajaAbrirCerrar = "caja.abrir_cerrar";
    public const string CajaVerTodas = "caja.ver_todas";
    public const string InventarioVer = "inventario.ver";
    public const string InventarioAjustar = "inventario.ajustar";
    public const string InventarioVerCostos = "inventario.ver_costos";
    public const string ComprasCrear = "compras.crear";
    public const string ComprasAnular = "compras.anular";
    public const string ProductosCrearEditar = "productos.crear_editar";
    public const string ProductosEliminar = "productos.eliminar";
    public const string ProductosCambiarPrecios = "productos.cambiar_precios";
    public const string ClientesCrearEditar = "clientes.crear_editar";
    public const string ClientesEliminar = "clientes.eliminar";
    public const string ProveedoresCrearEditar = "proveedores.crear_editar";
    public const string ProveedoresEliminar = "proveedores.eliminar";
    public const string ReportesVer = "reportes.ver";
    public const string AdminUsuarios = "admin.usuarios";
    public const string AdminRoles = "admin.roles";
    public const string AdminConfiguracion = "admin.configuracion";
    public const string AdminSucursales = "admin.sucursales";
}

/// <summary>
/// Policies que NO son un código de permiso.modulo del catálogo (no viven en
/// rol_permiso, no las tiene ninguna empresa): PermisoPolicyProvider las
/// resuelve como caso especial, contra el claim "es_superadmin" del JWT.
/// </summary>
public static class PoliciesEspeciales
{
    public const string SoloPlataforma = "SoloPlataforma";
}
