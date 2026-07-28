using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Tests.Infraestructura;

/// <summary>Permisos reales por rol de sistema, espejo de RolPermisoConfig (SeguridadConfig.cs).</summary>
public static class PermisosPorRol
{
    public static readonly string[] Dueno =
    [
        Permisos.VentasCrear, Permisos.VentasAnular, Permisos.VentasDescuento, Permisos.VentasVerHistorial,
        Permisos.CajaAbrirCerrar, Permisos.CajaVerTodas, Permisos.InventarioVer, Permisos.InventarioAjustar,
        Permisos.InventarioVerCostos, Permisos.ComprasCrear, Permisos.ComprasAnular,
        Permisos.ProductosCrearEditar, Permisos.ProductosEliminar, Permisos.ProductosCambiarPrecios,
        Permisos.ClientesCrearEditar, Permisos.ClientesEliminar,
        Permisos.ProveedoresCrearEditar, Permisos.ProveedoresEliminar,
        Permisos.ReportesVer, Permisos.AdminUsuarios, Permisos.AdminRoles, Permisos.AdminConfiguracion,
        Permisos.AdminSucursales,
    ];

    public static readonly string[] Vendedor =
    [
        Permisos.VentasCrear, Permisos.VentasVerHistorial, Permisos.CajaAbrirCerrar, Permisos.InventarioVer,
        Permisos.ClientesCrearEditar,
    ];

    public static string[] Para(Guid rolId) =>
        rolId == RolesSistema.Vendedor ? Vendedor : Dueno;
}

/// <summary>
/// Una empresa de prueba completa: sucursal, usuarios (dueño/vendedor) y un
/// producto de cada variante relevante para el POS (simple, con lotes,
/// controlado, con presentaciones).
/// </summary>
public class ContextoPrueba
{
    public required Empresa Empresa { get; init; }
    public required Sucursal Sucursal { get; init; }
    public required Usuario Dueno { get; init; }
    public required Usuario Vendedor { get; init; }
    public required Producto ProductoSimple { get; init; }
    public required Producto ProductoConLotes { get; init; }
    public required Producto ProductoControlado { get; init; }
    public required Producto ProductoConPresentaciones { get; init; }
    public required ProductoPresentacion PresentacionCajaX10 { get; init; }

    /// <summary>Arma el ICurrentUserService "de este actor" para pasarle a un contexto/handler.</summary>
    public FakeCurrentUserService ComoUsuario(Usuario usuario, IEnumerable<string>? permisos = null) => new()
    {
        UsuarioId = usuario.Id,
        EmpresaId = Empresa.Id,
        SucursalId = Sucursal.Id,
        Permisos = [.. permisos ?? PermisosPorRol.Para(usuario.RolId)],
    };
}
