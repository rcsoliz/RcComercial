using MediatR;

namespace RcComercial.Application.Clientes.Queries.ListarClientes;

public record ListarClientesResultDto(List<ClienteListItemDto> Items, int Total, int Pagina, int TamanoPagina);

public record ListarClientesQuery(string? Buscar, int Pagina = 1, string? Estado = null)
    : IRequest<ListarClientesResultDto>;
