using FluentValidation;
using MediatR;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Categorias.Commands;

public record CrearCategoriaCommand(string Nombre, Guid? PadreId) : IRequest<CategoriaDto>;

public class CrearCategoriaCommandValidator : AbstractValidator<CrearCategoriaCommand>
{
    public CrearCategoriaCommandValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public class CrearCategoriaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearCategoriaCommand, CategoriaDto>
{
    public async Task<CategoriaDto> Handle(CrearCategoriaCommand request, CancellationToken ct)
    {
        var categoria = new Categoria
        {
            EmpresaId = currentUser.EmpresaId!.Value,
            Nombre = request.Nombre,
            PadreId = request.PadreId,
        };
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);
        return new CategoriaDto(categoria.Id, categoria.Nombre, categoria.PadreId, categoria.Activo);
    }
}
