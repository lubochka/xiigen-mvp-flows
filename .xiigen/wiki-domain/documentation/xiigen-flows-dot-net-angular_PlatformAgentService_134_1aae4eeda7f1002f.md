# xiigen-flows-dot-net-angular::PlatformAgentService.EvaluateSuperJudgeAsync

## Meaning
xiigen-flows-dot-net-angular::PlatformAgentService.EvaluateSuperJudgeAsync implements local project behavior with return type object?>>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:55::xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:73::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.SuperJudgeZeroEvidenceDefersToAf9WithZeroCost, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:88::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.SuperJudgeOverrideWritesDpoTripleWithConstraint.

## Location
flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:134

## Parameters
- string sessionId
- IReadOnlyDictionary<string
- object?> request
- IReadOnlyDictionary<string
- object?> enriched
- CancellationToken cancellationToken

## Return
object?>>> returned by xiigen-flows-dot-net-angular::PlatformAgentService.EvaluateSuperJudgeAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:55::xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:73::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.SuperJudgeZeroEvidenceDefersToAf9WithZeroCost
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:88::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.SuperJudgeOverrideWritesDpoTripleWithConstraint

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:134::xiigen-flows-dot-net-angular::PlatformAgentService.EvaluateSuperJudgeAsync