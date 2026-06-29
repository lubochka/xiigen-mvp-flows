# xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.CreatePlanBody

## Meaning
xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.CreatePlanBody implements local project behavior with return type object?>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:551::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadObjects, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:573::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadStrings. It is called by xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:44::xiigen-flows-dot-net-angular::SubscriptionBillingService.PublishPlanAsync, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:40::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.PublishPlanAsync, xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:40::xiigen-flows-dot-net-angular::EtlDataIntegrationService.PublishPlanAsync.

## Location
flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:347

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string planId

## Return
object?> returned by xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.CreatePlanBody

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:44::xiigen-flows-dot-net-angular::SubscriptionBillingService.PublishPlanAsync
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:40::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.PublishPlanAsync
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:40::xiigen-flows-dot-net-angular::EtlDataIntegrationService.PublishPlanAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:551::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadObjects
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:573::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadStrings

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:347::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.CreatePlanBody