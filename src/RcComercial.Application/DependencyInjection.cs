using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RcComercial.Application.Common.Behaviors;

namespace RcComercial.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(AssemblyReference.Assembly);

        return services;
    }
}
