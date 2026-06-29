# xiigen-flows-dot-net-angular::HistoryBootstrapService.SummarizePatternsAsync

## Meaning
xiigen-flows-dot-net-angular::HistoryBootstrapService.SummarizePatternsAsync implements local project behavior with return type Task<DataProcessResult<SummarizeResult>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:224::xiigen-flows-dot-net-angular::PlatformAgentService.ReadRecords. It is called by xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:138::xiigen-flows-dot-net-angular::HistoryBootstrapService.RunBootstrapAsync, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:119::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.SummarizeGroupsByPatternTypeAndStoresBeforeQueue, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:138::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.SummarizeIsIdempotentPerRunId.

## Location
flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:103

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SummarizeResult>> returned by xiigen-flows-dot-net-angular::HistoryBootstrapService.SummarizePatternsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:138::xiigen-flows-dot-net-angular::HistoryBootstrapService.RunBootstrapAsync
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:119::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.SummarizeGroupsByPatternTypeAndStoresBeforeQueue
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:138::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.SummarizeIsIdempotentPerRunId

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:224::xiigen-flows-dot-net-angular::PlatformAgentService.ReadRecords

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:103::xiigen-flows-dot-net-angular::HistoryBootstrapService.SummarizePatternsAsync