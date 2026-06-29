# xiigen-flows-dot-net-angular::HistoryBootstrapService.ParseArchitectureSeed

## Meaning
xiigen-flows-dot-net-angular::HistoryBootstrapService.ParseArchitectureSeed implements local project behavior with return type DataProcessResult<IReadOnlyList<HistoryPattern>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:25::xiigen-flows-dot-net-angular::HistoryBootstrapService.ValidateSeedSource, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:224::xiigen-flows-dot-net-angular::PlatformAgentService.ReadRecords. It is called by xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:63::xiigen-flows-dot-net-angular::HistoryBootstrapService.SeedDocumentsAsync, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:72::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.ArchitectureSeedParsingForcesGlobalScopeAndFlowScopedConnection.

## Location
flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:31

## Parameters
- IReadOnlyDictionary<string
- object?> request

## Return
DataProcessResult<IReadOnlyList<HistoryPattern>> returned by xiigen-flows-dot-net-angular::HistoryBootstrapService.ParseArchitectureSeed

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:63::xiigen-flows-dot-net-angular::HistoryBootstrapService.SeedDocumentsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:72::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.ArchitectureSeedParsingForcesGlobalScopeAndFlowScopedConnection

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:25::xiigen-flows-dot-net-angular::HistoryBootstrapService.ValidateSeedSource
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:224::xiigen-flows-dot-net-angular::PlatformAgentService.ReadRecords

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:31::xiigen-flows-dot-net-angular::HistoryBootstrapService.ParseArchitectureSeed