# xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.CollectAnalyticsAsync

## Meaning
xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.CollectAnalyticsAsync implements local project behavior with return type Task<DataProcessResult<DynamicFormsWorkflowsOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:547::xiigen-flows-dot-net-angular::was.ReplayIfDuplicateAsync, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:785::xiigen-flows-dot-net-angular::VisualFlowEngineService.ReadDictionary, xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:769::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ContainsPii. It is called by xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Tests\DynamicFormsWorkflowsServiceTests.cs:151::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsServiceTests.CF21_4_AnalyticsAreAppendOnlyAndRejectPii, xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Tests\DynamicFormsWorkflowsServiceTests.cs:260::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsServiceTests.TenantScopePayloadFieldsAreRejectedAcrossMutations.

## Location
flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:357

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<DynamicFormsWorkflowsOutcome>> returned by xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.CollectAnalyticsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Tests\DynamicFormsWorkflowsServiceTests.cs:151::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsServiceTests.CF21_4_AnalyticsAreAppendOnlyAndRejectPii
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Tests\DynamicFormsWorkflowsServiceTests.cs:260::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsServiceTests.TenantScopePayloadFieldsAreRejectedAcrossMutations

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:547::xiigen-flows-dot-net-angular::was.ReplayIfDuplicateAsync
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:785::xiigen-flows-dot-net-angular::VisualFlowEngineService.ReadDictionary
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:769::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ContainsPii

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:357::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.CollectAnalyticsAsync