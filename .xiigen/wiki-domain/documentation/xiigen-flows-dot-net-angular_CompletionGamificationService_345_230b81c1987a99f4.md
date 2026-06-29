# xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitPeerGradeAsync

## Meaning
xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitPeerGradeAsync implements local project behavior with return type Task<DataProcessResult<CompletionGamificationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:247::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_PeerGradeIsIdempotentAndEmitsCloudEvent.

## Location
flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:345

## Parameters
- string lessonId
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<CompletionGamificationOutcome>> returned by xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitPeerGradeAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:247::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_PeerGradeIsIdempotentAndEmitsCloudEvent

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:345::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitPeerGradeAsync