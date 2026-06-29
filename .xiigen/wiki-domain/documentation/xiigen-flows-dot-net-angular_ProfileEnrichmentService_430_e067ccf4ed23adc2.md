# xiigen-flows-dot-net-angular::ProfileEnrichmentService.BuildPersonalizationAsync

## Meaning
xiigen-flows-dot-net-angular::ProfileEnrichmentService.BuildPersonalizationAsync implements local project behavior with return type Task<DataProcessResult<PersonalizationResult>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:491::xiigen-flows-dot-net-angular::ProfileEnrichmentService.LatestAsync, xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:528::xiigen-flows-dot-net-angular::ProfileEnrichmentService.AddSignalItem, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:39::xiigen-flows-dot-net-angular::ProfileEnrichmentService.SubmitQuestionnaireAsync.

## Location
flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:430

## Parameters
- string userId
- string profileId
- MatchingResult matching
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<PersonalizationResult>> returned by xiigen-flows-dot-net-angular::ProfileEnrichmentService.BuildPersonalizationAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:39::xiigen-flows-dot-net-angular::ProfileEnrichmentService.SubmitQuestionnaireAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:491::xiigen-flows-dot-net-angular::ProfileEnrichmentService.LatestAsync
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:528::xiigen-flows-dot-net-angular::ProfileEnrichmentService.AddSignalItem
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:430::xiigen-flows-dot-net-angular::ProfileEnrichmentService.BuildPersonalizationAsync