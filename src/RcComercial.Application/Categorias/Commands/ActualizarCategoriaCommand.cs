using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Categorias.Commands;

public record ActualizarCategoriaCommand(Guid Id, string Nombre, Guid? PadreId) : IRequest<bool>;

public class ActualizarCategoriaCommandValidator : AbstractValidator<ActualizarCategoriaCommand>
{
    public ActualizarCategoriaCommandValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public class ActualizarCategoriaCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ActualizarCategoriaCommand, bool>
{
    public async Task<bool> Handle(ActualizarCategoriaCommand request, CancellationToken ct)
    {
        var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (categoria is null) return false;

        categoria.Nombre = request.Nombre;
        categoria.PadreId = request.PadreId;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
