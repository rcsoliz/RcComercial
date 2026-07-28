// Espejo de Domain/Common/Constantes.cs (Permisos): mismos códigos que llegan
// en el claim "permiso" del JWT. Solo para mostrar/ocultar UI — la
// autorización real siempre la valida el backend con policies.
export const Permisos = {
  VentasCrear: 'ventas.crear',
  VentasAnular: 'ventas.anular',
  VentasDescuento: 'ventas.descuento',
  VentasVerHistorial: 'ventas.ver_historial',
  CajaAbrirCerrar: 'caja.abrir_cerrar',
  CajaVerTodas: 'caja.ver_todas',
  InventarioVer: 'inventario.ver',
  InventarioAjustar: 'inventario.ajustar',
  InventarioVerCostos: 'inventario.ver_costos',
  ComprasCrear: 'compras.crear',
  ComprasAnular: 'compras.anular',
  ProductosCrearEditar: 'productos.crear_editar',
  ProductosEliminar: 'productos.eliminar',
  ProductosCambiarPrecios: 'productos.cambiar_precios',
  ClientesCrearEditar: 'clientes.crear_editar',
  ClientesEliminar: 'clientes.eliminar',
  ProveedoresCrearEditar: 'proveedores.crear_editar',
  ProveedoresEliminar: 'proveedores.eliminar',
  ReportesVer: 'reportes.ver',
  AdminUsuarios: 'admin.usuarios',
  AdminRoles: 'admin.roles',
  AdminConfiguracion: 'admin.configuracion',
  AdminSucursales: 'admin.sucursales',
}
