# xiigen-flows-dot-net-angular::ProfileEnrichmentService.ScoreMatchingAsync

## Meaning
xiigen-flows-dot-net-angular::ProfileEnrichmentService.ScoreMatchingAsync implements local project behavior with return type Task<DataProcessResult<MatchingResult>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:500::xiigen-flows-dot-net-angular::ProfileEnrichmentService.GetFreedomNumberAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:39::xiigen-flows-dot-net-angular::ProfileEnrichmentService.SubmitQuestionnaireAsync.

## Location
flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:368

## Parameters
- string userId
- string profileId
- bool forcePartial
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<MatchingResult>> returned by xiigen-flows-dot-net-angular::ProfileEnrichmentService.ScoreMatchingAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:39::xiigen-flows-dot-net-angular::ProfileEnrichmentService.SubmitQuestionnaireAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:500::xiigen-flows-dot-net-angular::ProfileEnrichmentService.GetFreedomNumberAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:368::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ScoreMatchingAsync