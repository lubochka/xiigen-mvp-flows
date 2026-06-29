# xiigen-flows-dot-net-angular::AdsPlatformService.ReplayIfDuplicateAsync

## Meaning
xiigen-flows-dot-net-angular::AdsPlatformService.ReplayIfDuplicateAsync implements local project behavior with return type Task<AdsPlatformOutcome?>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. No internal caller was detected.

## Location
flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:590

## Parameters
- string operationKey
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<AdsPlatformOutcome?> returned by xiigen-flows-dot-net-angular::AdsPlatformService.ReplayIfDuplicateAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:590::xiigen-flows-dot-net-angular::AdsPlatformService.ReplayIfDuplicateAsync