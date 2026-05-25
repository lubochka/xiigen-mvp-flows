namespace Xiigen.Flow.WebflowDesignerExtension.Application;

public static class WebflowDesignerExtensionConstants
{
    public const string FlowId = "FLOW-43";
    public const string FlowSlug = "webflow-designer-extension";
    public const string ImplementationAlias = "webflow-designer-extension";
    public const string DynamicEndpoint = "/api/dynamic/webflow-designer-extension";
    public const string PackageName = "@xiigen/webflow-designer-extension";
    public const string PackageVersion = "1.0.0";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] SupportedElementTypes =
    [
        "Section", "Container", "Div", "Block", "Heading", "Paragraph", "TextBlock", "RichText",
        "Link", "Button", "Image", "Video", "BackgroundVideo", "Form", "FormWrapper", "Input",
        "Textarea", "Select", "Checkbox", "Radio", "NavBar", "NavMenu", "NavLink", "NavButton",
        "Tab", "TabsMenu", "TabsContent", "TabLink", "TabPane", "Slider", "SliderMask", "Slide",
        "SliderArrow", "SliderNav", "Dropdown", "DropdownToggle", "DropdownList", "DropdownLink",
        "LightboxLink", "LightboxContainer", "LightboxCaption", "Symbol", "CommerceCart",
        "CommerceCartWrapper", "CommerceCheckout", "CommerceOrderConfirm", "CommerceItem",
        "CommercePayments", "CommerceSubscriptions", "HtmlEmbed", "Map", "Unknown"
    ];

    public static readonly string[] RequiredTaskTypes =
    [
        "T690-webflow-css-shorthand-parser",
        "T691-webflow-element-property-router",
        "T692-webflow-validation-diff",
        "T693-webflow-package-certification"
    ];

    public static readonly string[] RequiredBfaRules =
    [
        "CF-800_CSS_SHORTHANDS",
        "CF-801_ELEMENT_CLASSIFICATION",
        "CF-802_NO_DEPRECATED_WEBFLOW_API",
        "CF-803_VALIDATION_DIFF",
        "CF-W1-PACKAGE-01",
        "CF-W1-LIVE-AUTH-01",
        "CF-W1-SCOPE-01"
    ];

    public const string AdapterRunsCollection = "xiigen-webflow-adapter-runs";
    public const string PublicationAuditCollection = "xiigen-webflow-adapter-publications";
}
