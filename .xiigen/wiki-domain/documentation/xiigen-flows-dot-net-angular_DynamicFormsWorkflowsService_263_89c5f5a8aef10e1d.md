# xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.DispatchAutomationAsync

## Meaning
xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.DispatchAutomationAsync implements local project behavior with return type Task<DataProcessResult<DynamicFormsWorkflowsOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:547::xiigen-flows-dot-net-angular::was.ReplayIfDuplicateAsync, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant, xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:605::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ResolveAutomationBranch, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Tests\DynamicFormsWorkflowsServiceTests.cs:112::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsServiceTests.CF21_3_AutomationDispatchAcquiresSetNxEquivalentLockBeforeOutbox, xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Tests\DynamicFormsWorkflowsServiceTests.cs:134::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsServiceTests.CF21_3_DuplicateAutomationDispatchDoesNotDuplicateOutbox.

## Location
flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:263

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<DynamicFormsWorkflowsOutcome>> returned by xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.DispatchAutomationAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Tests\DynamicFormsWorkflowsServiceTests.cs:112::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsServiceTests.CF21_3_AutomationDispatchAcquiresSetNxEquivalentLockBeforeOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Tests\DynamicFormsWorkflowsServiceTests.cs:134::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsServiceTests.CF21_3_DuplicateAutomationDispatchDoesNotDuplicateOutbox

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:547::xiigen-flows-dot-net-angular::was.ReplayIfDuplicateAsync
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:605::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ResolveAutomationBranch
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:263::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.DispatchAutomationAsync