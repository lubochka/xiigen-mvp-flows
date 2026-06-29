# xiigen-flows-dot-net-angular::FreelancerMarketplaceService.ReplayIfDuplicateAsync

## Meaning
xiigen-flows-dot-net-angular::FreelancerMarketplaceService.ReplayIfDuplicateAsync implements local project behavior with return type Task<FreelancerMarketplaceOutcome?>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. No internal caller was detected.

## Location
flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:560

## Parameters
- string operationKey
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<FreelancerMarketplaceOutcome?> returned by xiigen-flows-dot-net-angular::FreelancerMarketplaceService.ReplayIfDuplicateAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:560::xiigen-flows-dot-net-angular::FreelancerMarketplaceService.ReplayIfDuplicateAsync