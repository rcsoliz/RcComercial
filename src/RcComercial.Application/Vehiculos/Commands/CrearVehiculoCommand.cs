using FluentValidation;
using MediatR;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Vehiculos.Commands;

public record CrearVehiculoCommand(
    Guid ClienteId, string Placa, string? Marca, string? Modelo,
    short? Anio, string? Color, string? NumeroChasis) : IRequest<VehiculoDto>;

public class CrearVehiculoCommandValidator : AbstractValidator<CrearVehiculoCommand>
{
    public CrearVehiculoCommandValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Placa).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Marca).MaximumLength(60);
        RuleFor(x => x.Modelo).MaximumLength(60);
        RuleFor(x => x.Color).MaximumLength(40);
        RuleFor(x => x.NumeroChasis).MaximumLength(60);
    }
}

public class CrearVehiculoCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearVehiculoCommand, VehiculoDto>
{
    public async Task<VehiculoDto> Handle(CrearVehiculoCommand request, CancellationToken ct)
    {
        var vehiculo = new Vehiculo
        {
            EmpresaId = currentUser.EmpresaId!.Value,
            ClienteId = request.ClienteId,
            Placa = request.Placa,
            Marca = request.Marca,
            Modelo = request.Modelo,
            Anio = request.Anio,
            Color = request.Color,
            NumeroChasis = request.NumeroChasis,
        };
        db.Vehiculos.Add(vehiculo);
        await db.SaveChangesAsync(ct);
        return new VehiculoDto(
            vehiculo.Id, vehiculo.ClienteId, vehiculo.Placa, vehiculo.Marca, vehiculo.Modelo,
            vehiculo.Anio, vehiculo.Color, vehiculo.NumeroChasis, vehiculo.Activo);
    }
}
