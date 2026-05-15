namespace Xiigen.Flow.BundleActivation.Application;

public static class BundleActivationConstants
{
    public const string FlowId = "FLOW-00";
    public const string FlowSlug = "bundle-activation";
    public const string DynamicEndpoint = "/api/dynamic/bundle-activation";
    public const string SolutionBundles = "solution-bundles";
    public const string FlowLifecycle = "flow-lifecycle";
    public const string BundleActivations = "bundle-activations";
    public const string FreedomConfig = "freedom-config";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-842",
        "CF-843",
        "CF-844",
        "CF-845",
        "CF-846",
        "CF-847",
        "CF-848",
        "CF-849"
    };
}
