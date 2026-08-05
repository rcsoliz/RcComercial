using MediatR;
using RcComercial.Application.Proformas.Dtos;

namespace RcComercial.Application.Proformas.Queries.ListarProformas;

public record ListarProformasResultDto(List<ProformaListItemDto> Items, int Total, int Pagina, int TamanoPagina);

public record ListarProformasQuery(string? Buscar, int Pagina = 1, string? Estado = null, Guid? ClienteId = null)
    : IRequest<ListarProformasResultDto>;
