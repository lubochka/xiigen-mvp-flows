# xiigen-flows-dot-net-angular::HistoryBootstrapService.RunBootstrapAsync

## Meaning
xiigen-flows-dot-net-angular::HistoryBootstrapService.RunBootstrapAsync implements local project behavior with return type Task<DataProcessResult<HistoryBootstrapOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:63::xiigen-flows-dot-net-angular::HistoryBootstrapService.SeedDocumentsAsync, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:92::xiigen-flows-dot-net-angular::HistoryBootstrapService.RetrieveArchitecturePatternsAsync, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:103::xiigen-flows-dot-net-angular::HistoryBootstrapService.SummarizePatternsAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:155::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.RunBootstrapPersistsCompletionBeforeCloudEventAndIsIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:176::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.RunBootstrapRejectsMissingRunIdWithoutPartialState, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:190::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.DashboardIsTenantScoped.

## Location
flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:138

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<HistoryBootstrapOutcome>> returned by xiigen-flows-dot-net-angular::HistoryBootstrapService.RunBootstrapAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:155::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.RunBootstrapPersistsCompletionBeforeCloudEventAndIsIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:176::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.RunBootstrapRejectsMissingRunIdWithoutPartialState
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:190::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.DashboardIsTenantScoped

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:63::xiigen-flows-dot-net-angular::HistoryBootstrapService.SeedDocumentsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:92::xiigen-flows-dot-net-angular::HistoryBootstrapService.RetrieveArchitecturePatternsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:103::xiigen-flows-dot-net-angular::HistoryBootstrapService.SummarizePatternsAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:138::xiigen-flows-dot-net-angular::HistoryBootstrapService.RunBootstrapAsync