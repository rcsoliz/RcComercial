using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Vehiculos.Commands;

public record EditarVehiculoCommand(
    Guid Id, string Placa, string? Marca, string? Modelo,
    short? Anio, string? Color, string? NumeroChasis) : IRequest<VehiculoDto?>;

public class EditarVehiculoCommandValidator : AbstractValidator<EditarVehiculoCommand>
{
    public EditarVehiculoCommandValidator()
    {
        RuleFor(x => x.Placa).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Marca).MaximumLength(60);
        RuleFor(x => x.Modelo).MaximumLength(60);
        RuleFor(x => x.Color).MaximumLength(40);
        RuleFor(x => x.NumeroChasis).MaximumLength(60);
    }
}

public class EditarVehiculoCommandHandler(IApplicationDbContext db)
    : IRequestHandler<EditarVehiculoCommand, VehiculoDto?>
{
    public async Task<VehiculoDto?> Handle(EditarVehiculoCommand request, CancellationToken ct)
    {
        var vehiculo = await db.Vehiculos.FirstOrDefaultAsync(v => v.Id == request.Id, ct);
        if (vehiculo is null) return null;

        vehiculo.Placa = request.Placa;
        vehiculo.Marca = request.Marca;
        vehiculo.Modelo = request.Modelo;
        vehiculo.Anio = request.Anio;
        vehiculo.Color = request.Color;
        vehiculo.NumeroChasis = request.NumeroChasis;
        await db.SaveChangesAsync(ct);

        return new VehiculoDto(
            vehiculo.Id, vehiculo.ClienteId, vehiculo.Placa, vehiculo.Marca, vehiculo.Modelo,
            vehiculo.Anio, vehiculo.Color, vehiculo.NumeroChasis, vehiculo.Activo);
    }
}
