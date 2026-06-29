# xiigen-flows-dot-net-angular::MarketplaceService.ValidateListing

## Meaning
xiigen-flows-dot-net-angular::MarketplaceService.ValidateListing implements local project behavior with return type DataProcessResult<MarketplaceOutcome>?. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal. It is called by xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:38::xiigen-flows-dot-net-angular::MarketplaceService.PublishListingAsync.

## Location
flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:232

## Parameters
- IReadOnlyDictionary<string
- object?> request

## Return
DataProcessResult<MarketplaceOutcome>? returned by xiigen-flows-dot-net-angular::MarketplaceService.ValidateListing

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:38::xiigen-flows-dot-net-angular::MarketplaceService.PublishListingAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:232::xiigen-flows-dot-net-angular::MarketplaceService.ValidateListing