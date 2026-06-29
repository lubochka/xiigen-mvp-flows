# xiigen-flows-dot-net-angular::MarketplaceService.GetCatalogAsync

## Meaning
xiigen-flows-dot-net-angular::MarketplaceService.GetCatalogAsync implements local project behavior with return type Task<DataProcessResult<MarketplaceCatalog>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:327::xiigen-flows-dot-net-angular::MarketplaceService.CountByStatus. It is called by xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:78::xiigen-flows-dot-net-angular::MarketplaceServiceTests.CF0804_CrossTenantCatalogRead_DoesNotLeakListings, xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:174::xiigen-flows-dot-net-angular::MarketplaceServiceTests.CF900_IdempotencyDuplicateCoverage_StoresOneCatalogRow.

## Location
flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:185

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<MarketplaceCatalog>> returned by xiigen-flows-dot-net-angular::MarketplaceService.GetCatalogAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:78::xiigen-flows-dot-net-angular::MarketplaceServiceTests.CF0804_CrossTenantCatalogRead_DoesNotLeakListings
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:174::xiigen-flows-dot-net-angular::MarketplaceServiceTests.CF900_IdempotencyDuplicateCoverage_StoresOneCatalogRow

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:327::xiigen-flows-dot-net-angular::MarketplaceService.CountByStatus

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:185::xiigen-flows-dot-net-angular::MarketplaceService.GetCatalogAsync