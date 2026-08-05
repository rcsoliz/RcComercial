using MediatR;

namespace RcComercial.Application.Proveedores.Queries.ListarProveedores;

public record ListarProveedoresResultDto(List<ProveedorDto> Items, int Total, int Pagina, int TamanoPagina);

public record ListarProveedoresQuery(string? Buscar, int Pagina = 1, string? Estado = null)
    : IRequest<ListarProveedoresResultDto>;
