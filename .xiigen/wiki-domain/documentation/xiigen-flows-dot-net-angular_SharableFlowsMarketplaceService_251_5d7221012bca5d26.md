# xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync

## Meaning
xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync implements local project behavior with return type Task<DataProcessResult<SharableFlowsMarketplaceOutcome>>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:47::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.PublishPackageAsync, xiigen-flows-dot-net-angular::flows\FLOW-33-system-initiation-bootstrap\server\Xiigen.Flow.SystemInitiationBootstrap.Application\SystemInitiationBootstrapService.cs:47::xiigen-flows-dot-net-angular::SystemInitiationBootstrapService.StartBootstrapAsync, xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Application\MarketplacePluginAdapterService.cs:47::xiigen-flows-dot-net-angular::MarketplacePluginAdapterService.StartAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-35-meta-arbitration-engine\server\Xiigen.Flow.MetaArbitrationEngine.Application\MetaArbitrationEngineService.cs:47::xiigen-flows-dot-net-angular::MetaArbitrationEngineService.ResolveConflictAsync, xiigen-flows-dot-net-angular::flows\FLOW-36-feature-registry\server\Xiigen.Flow.FeatureRegistry.Application\FeatureRegistryService.cs:47::xiigen-flows-dot-net-angular::FeatureRegistryService.RegisterFeatureAsync, xiigen-flows-dot-net-angular::flows\FLOW-37-design-system-governance\server\Xiigen.Flow.DesignSystemGovernance.Application\DesignSystemGovernanceService.cs:47::xiigen-flows-dot-net-angular::DesignSystemGovernanceService.RegisterGovernanceAsync, xiigen-flows-dot-net-angular::flows\FLOW-38-rag-quality-feedback\server\Xiigen.Flow.RagQualityFeedback.Application\RagQualityFeedbackService.cs:48::xiigen-flows-dot-net-angular::RagQualityFeedbackService.RecordFeedbackAsync, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:64::xiigen-flows-dot-net-angular::OssCurriculumService.AssignTierAsync, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:123::xiigen-flows-dot-net-angular::OssCurriculumService.SubmitShadowRunsAsync, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:259::xiigen-flows-dot-net-angular::OssCurriculumService.TrackProgressAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:64::xiigen-flows-dot-net-angular::ClientPushService.RegisterConnectionAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:180::xiigen-flows-dot-net-angular::ClientPushService.RunKeepaliveAsync.

## Location
flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:251

## Parameters
- string eventType
- string idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SharableFlowsMarketplaceOutcome>> returned by xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:47::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.PublishPackageAsync
- xiigen-flows-dot-net-angular::flows\FLOW-33-system-initiation-bootstrap\server\Xiigen.Flow.SystemInitiationBootstrap.Application\SystemInitiationBootstrapService.cs:47::xiigen-flows-dot-net-angular::SystemInitiationBootstrapService.StartBootstrapAsync
- xiigen-flows-dot-net-angular::flows\FLOW-34-marketplace-plugin-adapter\server\Xiigen.Flow.MarketplacePluginAdapter.Application\MarketplacePluginAdapterService.cs:47::xiigen-flows-dot-net-angular::MarketplacePluginAdapterService.StartAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-35-meta-arbitration-engine\server\Xiigen.Flow.MetaArbitrationEngine.Application\MetaArbitrationEngineService.cs:47::xiigen-flows-dot-net-angular::MetaArbitrationEngineService.ResolveConflictAsync
- xiigen-flows-dot-net-angular::flows\FLOW-36-feature-registry\server\Xiigen.Flow.FeatureRegistry.Application\FeatureRegistryService.cs:47::xiigen-flows-dot-net-angular::FeatureRegistryService.RegisterFeatureAsync
- xiigen-flows-dot-net-angular::flows\FLOW-37-design-system-governance\server\Xiigen.Flow.DesignSystemGovernance.Application\DesignSystemGovernanceService.cs:47::xiigen-flows-dot-net-angular::DesignSystemGovernanceService.RegisterGovernanceAsync
- xiigen-flows-dot-net-angular::flows\FLOW-38-rag-quality-feedback\server\Xiigen.Flow.RagQualityFeedback.Application\RagQualityFeedbackService.cs:48::xiigen-flows-dot-net-angular::RagQualityFeedbackService.RecordFeedbackAsync
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:64::xiigen-flows-dot-net-angular::OssCurriculumService.AssignTierAsync
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:123::xiigen-flows-dot-net-angular::OssCurriculumService.SubmitShadowRunsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:259::xiigen-flows-dot-net-angular::OssCurriculumService.TrackProgressAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:64::xiigen-flows-dot-net-angular::ClientPushService.RegisterConnectionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:180::xiigen-flows-dot-net-angular::ClientPushService.RunKeepaliveAsync

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:251::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync