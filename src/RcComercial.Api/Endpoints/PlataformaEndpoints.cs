using MediatR;
using RcComercial.Application.Plataforma.Commands;
using RcComercial.Application.Plataforma.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public record CambiarActivoEmpresaRequest(bool Activo);

/// <summary>Back-office del proveedor SaaS: alta/baja de tenants. Todo detrás de PoliciesEspeciales.SoloPlataforma (claim es_superadmin).</summary>
public static class PlataformaEndpoints
{
    public static void MapPlataformaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/plataforma").RequireAuthorization(PoliciesEspeciales.SoloPlataforma);

        group.MapGet("/empresas", async (string? estado, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarEmpresasPlataformaQuery(estado))));

        group.MapPost("/empresas", async (CrearEmpresaPlataformaCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)));

        group.MapPatch("/empresas/{id:guid}/activo", async (Guid id, CambiarActivoEmpresaRequest request, IMediator mediator) =>
        {
            var ok = await mediator.Send(new CambiarActivoEmpresaCommand(id, request.Activo));
            return ok ? Results.Ok() : Results.NotFound();
        });
    }
}
