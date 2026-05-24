namespace Xiigen.Flow.SystemInitiationBootstrap.Application;

public static class SystemInitiationBootstrapConstants
{
    public const string FlowId = "FLOW-33";
    public const string FlowSlug = "system-initiation-bootstrap";
    public const string ImplementationAlias = "system-initiation-bootstrap";
    public const string DynamicEndpoint = "/api/dynamic/system-initiation-bootstrap";
    public const string PackageName = "Xiigen.Flow.SystemInitiationBootstrap";

    public static readonly string[] CanonicalRoles = ["anonymous", "platform_admin", "platform_support"];
    public static readonly string[] PublicRoles = ["anonymous"];
    public static readonly string[] OperatorRoles = ["platform_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support"];

    public static readonly string[] RequiredTaskTypes = Enumerable.Range(536, 7).Select(value => $"T{value}").ToArray();
    public static readonly string[] RequiredBfaRules = Enumerable.Range(1023, 4).Select(value => $"CF-{value}").ToArray();

    public const string BootstrapCollection = "xiigen-system-bootstrap-runs";
    public const string RegistryCollection = "xiigen-system-bootstrap-registries";
    public const string GapCollection = "xiigen-system-bootstrap-capability-gaps";
    public const string SmokeCollection = "xiigen-system-bootstrap-smoke-runs";
    public const string AuditCollection = "xiigen-system-bootstrap-audit";
}
