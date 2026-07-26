using FluentValidation;
using MediatR;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Marcas.Commands;

public record CrearMarcaCommand(string Nombre) : IRequest<MarcaDto>;

public class CrearMarcaCommandValidator : AbstractValidator<CrearMarcaCommand>
{
    public CrearMarcaCommandValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public class CrearMarcaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearMarcaCommand, MarcaDto>
{
    public async Task<MarcaDto> Handle(CrearMarcaCommand request, CancellationToken ct)
    {
        var marca = new Marca { EmpresaId = currentUser.EmpresaId!.Value, Nombre = request.Nombre };
        db.Marcas.Add(marca);
        await db.SaveChangesAsync(ct);
        return new MarcaDto(marca.Id, marca.Nombre, marca.Activo);
    }
}
