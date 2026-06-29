# xiigen-flows-dot-net-angular::MarketplaceService.WriteAuditFirstAsync

## Meaning
xiigen-flows-dot-net-angular::MarketplaceService.WriteAuditFirstAsync implements local project behavior with return type Task<DataProcessResult<bool>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:38::xiigen-flows-dot-net-angular::MarketplaceService.PublishListingAsync.

## Location
flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:212

## Parameters
- string listingId
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<bool>> returned by xiigen-flows-dot-net-angular::MarketplaceService.WriteAuditFirstAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:38::xiigen-flows-dot-net-angular::MarketplaceService.PublishListingAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:212::xiigen-flows-dot-net-angular::MarketplaceService.WriteAuditFirstAsync