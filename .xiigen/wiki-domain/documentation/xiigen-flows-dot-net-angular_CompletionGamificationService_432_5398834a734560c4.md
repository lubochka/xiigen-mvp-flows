# xiigen-flows-dot-net-angular::CompletionGamificationService.CalculateServerSidePointsAsync

## Meaning
xiigen-flows-dot-net-angular::CompletionGamificationService.CalculateServerSidePointsAsync implements local project behavior with return type Task<int>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:500::xiigen-flows-dot-net-angular::ProfileEnrichmentService.GetFreedomNumberAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:38::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitCompletionAsync.

## Location
flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:432

## Parameters
- int scorePercent
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<int> returned by xiigen-flows-dot-net-angular::CompletionGamificationService.CalculateServerSidePointsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:38::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitCompletionAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:500::xiigen-flows-dot-net-angular::ProfileEnrichmentService.GetFreedomNumberAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:432::xiigen-flows-dot-net-angular::CompletionGamificationService.CalculateServerSidePointsAsync