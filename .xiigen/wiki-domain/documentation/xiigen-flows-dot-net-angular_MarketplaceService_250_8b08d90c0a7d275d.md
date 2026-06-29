# xiigen-flows-dot-net-angular::MarketplaceService.ResolvePriceCapAsync

## Meaning
xiigen-flows-dot-net-angular::MarketplaceService.ResolvePriceCapAsync implements local project behavior with return type Task<decimal?>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal. It is called by xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:38::xiigen-flows-dot-net-angular::MarketplaceService.PublishListingAsync.

## Location
flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:250

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<decimal?> returned by xiigen-flows-dot-net-angular::MarketplaceService.ResolvePriceCapAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:38::xiigen-flows-dot-net-angular::MarketplaceService.PublishListingAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:250::xiigen-flows-dot-net-angular::MarketplaceService.ResolvePriceCapAsync