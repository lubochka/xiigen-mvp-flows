# xiigen-flows-dot-net-angular::MarketplacePluginAdapterService.StartAdapterAsync

## Meaning
xiigen-flows-dot-net-angular::MarketplacePluginAdapterService.StartAdapterAsync implements local project behavior with return type Task<DataProcessResult<MarketplacePluginAdapterOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync, xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:332::xiigen-flows-dot-net-angular::HumanInteractionGateService.GetTenantDocumentAsync, xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:251::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:61::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.StartBootstrapRejectsBoundaryValuesAndFreeTenantPayloads, xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:86::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.StartBootstrapIsIdempotentAndStoresBeforeOutbox, xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:107::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.StartBootstrapOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox, xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:126::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.CapabilityGapsEmitSelfExtensionEventAndDashboardIsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:146::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.DashboardSummarizesBootstrapLifecycle.

## Location
flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Application\MarketplacePluginAdapterService.cs:47

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- string? expectedVersion
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<MarketplacePluginAdapterOutcome>> returned by xiigen-flows-dot-net-angular::MarketplacePluginAdapterService.StartAdapterAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:61::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.StartBootstrapRejectsBoundaryValuesAndFreeTenantPayloads
- xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:86::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.StartBootstrapIsIdempotentAndStoresBeforeOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:107::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.StartBootstrapOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:126::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.CapabilityGapsEmitSelfExtensionEventAndDashboardIsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Tests\MarketplacePluginAdapterServiceTests.cs:146::xiigen-flows-dot-net-angular::MarketplacePluginAdapterServiceTests.DashboardSummarizesBootstrapLifecycle

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:332::xiigen-flows-dot-net-angular::HumanInteractionGateService.GetTenantDocumentAsync
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:251::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Application\MarketplacePluginAdapterService.cs:47::xiigen-flows-dot-net-angular::MarketplacePluginAdapterService.StartAdapterAsync