using MediatR;
using RcComercial.Application.Compras.Dtos;

namespace RcComercial.Application.Compras.Queries.ObtenerSugeridoCompra;

public record ObtenerSugeridoCompraQuery(Guid ProveedorId, Guid? SucursalId) : IRequest<List<SugeridoCompraItemDto>>;
