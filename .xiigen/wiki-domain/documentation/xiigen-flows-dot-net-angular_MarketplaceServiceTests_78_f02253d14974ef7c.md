# xiigen-flows-dot-net-angular::MarketplaceServiceTests.CF0804_CrossTenantCatalogRead_DoesNotLeakListings

## Meaning
xiigen-flows-dot-net-angular::MarketplaceServiceTests.CF0804_CrossTenantCatalogRead_DoesNotLeakListings implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create, xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:38::xiigen-flows-dot-net-angular::MarketplaceService.PublishListingAsync, xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:185::xiigen-flows-dot-net-angular::MarketplaceService.GetCatalogAsync, xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:289::xiigen-flows-dot-net-angular::MarketplaceServiceTests.Listing. No internal caller was detected.

## Location
flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:78

## Parameters
- none recorded

## Return
Task returned by xiigen-flows-dot-net-angular::MarketplaceServiceTests.CF0804_CrossTenantCatalogRead_DoesNotLeakListings

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:38::xiigen-flows-dot-net-angular::MarketplaceService.PublishListingAsync
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:185::xiigen-flows-dot-net-angular::MarketplaceService.GetCatalogAsync
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:289::xiigen-flows-dot-net-angular::MarketplaceServiceTests.Listing

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:78::xiigen-flows-dot-net-angular::MarketplaceServiceTests.CF0804_CrossTenantCatalogRead_DoesNotLeakListings