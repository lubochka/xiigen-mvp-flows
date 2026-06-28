namespace Xiigen.Flow.DesignConversionIntake.Application;

public static class DesignConversionIntakeAuthPolicy
{
    public static DesignConversionAuthDecision Evaluate(
        string role,
        string? authorization,
        string requestTenantId,
        IReadOnlyCollection<string> allowedRoles)
    {
        if (!allowedRoles.Contains(role, StringComparer.Ordinal))
        {
            return new DesignConversionAuthDecision(false, 403, "WRONG_ROLE", allowedRoles.ToArray());
        }

        if (string.IsNullOrWhiteSpace(authorization))
        {
            return new DesignConversionAuthDecision(false, 401, "MISSING_TOKEN", allowedRoles.ToArray());
        }

        if (!authorization.StartsWith("Bearer local-", StringComparison.Ordinal))
        {
            return new DesignConversionAuthDecision(false, 401, "INVALID_TOKEN", allowedRoles.ToArray());
        }

        var tokenPayload = authorization["Bearer local-".Length..];
        var tokenTenant = tokenPayload.EndsWith("-token", StringComparison.Ordinal)
            ? tokenPayload[..^"-token".Length]
            : tokenPayload;

        if (!string.Equals(tokenTenant, requestTenantId, StringComparison.Ordinal))
        {
            return new DesignConversionAuthDecision(false, 401, "AUTH_CROSS_TENANT_TOKEN", allowedRoles.ToArray());
        }

        return new DesignConversionAuthDecision(true, 200, null, allowedRoles.ToArray());
    }
}

