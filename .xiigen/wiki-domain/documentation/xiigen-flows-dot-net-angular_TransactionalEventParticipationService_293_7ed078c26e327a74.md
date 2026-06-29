# xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RedeemTokenAsync

## Meaning
xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RedeemTokenAsync implements local project behavior with return type Task<DataProcessResult<TransactionalEventParticipationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:124::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.CF0906_PlatformOnlyQrTokenAcceptsIssuedTokenAndRejectsCustomToken.

## Location
flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:293

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<TransactionalEventParticipationOutcome>> returned by xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RedeemTokenAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:124::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.CF0906_PlatformOnlyQrTokenAcceptsIssuedTokenAndRejectsCustomToken

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:293::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RedeemTokenAsync