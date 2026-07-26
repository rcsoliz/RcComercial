using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Marcas.Commands;

public record ActualizarMarcaCommand(Guid Id, string Nombre) : IRequest<bool>;

public class ActualizarMarcaCommandValidator : AbstractValidator<ActualizarMarcaCommand>
{
    public ActualizarMarcaCommandValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public class ActualizarMarcaCommandHandler(IApplicationDbContext db) : IRequestHandler<ActualizarMarcaCommand, bool>
{
    public async Task<bool> Handle(ActualizarMarcaCommand request, CancellationToken ct)
    {
        var marca = await db.Marcas.FirstOrDefaultAsync(m => m.Id == request.Id, ct);
        if (marca is null) return false;

        marca.Nombre = request.Nombre;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
