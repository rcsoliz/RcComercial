using Microsoft.AspNetCore.Authorization;

namespace RcComercial.Api.Authorization;

/// <summary>
/// Policy provider dinámico: cualquier nombre de policy se interpreta como
/// un código de permiso (ej. "admin.usuarios") y se valida contra el claim
/// "permiso" del JWT. Así [Authorize(Policy = Permisos.XXX)] funciona sin
/// tener que registrar cada policy a mano en Program.cs.
/// </summary>
public class PermisoPolicyProvider : IAuthorizationPolicyProvider
{
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        Task.FromResult<AuthorizationPolicy?>(null);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim("permiso", policyName)
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
