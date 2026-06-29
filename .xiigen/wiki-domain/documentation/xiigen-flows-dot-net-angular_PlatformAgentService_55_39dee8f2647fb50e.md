# xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync

## Meaning
xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync implements local project behavior with return type Task<DataProcessResult<PlatformAgentOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:113::xiigen-flows-dot-net-angular::PlatformAgentService.EnrichPlatformContextAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:134::xiigen-flows-dot-net-angular::PlatformAgentService.EvaluateSuperJudgeAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:156::xiigen-flows-dot-net-angular::PlatformAgentService.PublishActionAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:181::xiigen-flows-dot-net-angular::PlatformAgentService.ContributePatternAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:224::xiigen-flows-dot-net-angular::PlatformAgentService.ReadRecords. It is called by xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:61::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.RunAgentRequiresMasterTenantContext, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:167::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.RunAgentPersistsSessionBeforeCompletionEventAndIsIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:187::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.DashboardIsTenantScoped.

## Location
flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:55

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<PlatformAgentOutcome>> returned by xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:61::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.RunAgentRequiresMasterTenantContext
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:167::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.RunAgentPersistsSessionBeforeCompletionEventAndIsIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:187::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.DashboardIsTenantScoped

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:113::xiigen-flows-dot-net-angular::PlatformAgentService.EnrichPlatformContextAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:134::xiigen-flows-dot-net-angular::PlatformAgentService.EvaluateSuperJudgeAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:156::xiigen-flows-dot-net-angular::PlatformAgentService.PublishActionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:181::xiigen-flows-dot-net-angular::PlatformAgentService.ContributePatternAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:224::xiigen-flows-dot-net-angular::PlatformAgentService.ReadRecords

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:55::xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync