using MediatR;
using RcComercial.Application.Empresas;

namespace RcComercial.Application.Empresas.Queries.ObtenerEmpresaActual;

public record ObtenerEmpresaActualQuery : IRequest<EmpresaActualDto?>;
