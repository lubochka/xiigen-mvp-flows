# xiigen-flows-dot-net-angular::HistoryBootstrapService.SeedDocumentsAsync

## Meaning
xiigen-flows-dot-net-angular::HistoryBootstrapService.SeedDocumentsAsync implements local project behavior with return type Task<DataProcessResult<HistoryBootstrapOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:31::xiigen-flows-dot-net-angular::HistoryBootstrapService.ParseArchitectureSeed. It is called by xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:138::xiigen-flows-dot-net-angular::HistoryBootstrapService.RunBootstrapAsync, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:88::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.SeedDocumentsRoutesDesignReasoningAndArchitecturePatterns, xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:105::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.RetrieveArchitecturePatternsReturnsOnlyGlobalScope.

## Location
flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:63

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<HistoryBootstrapOutcome>> returned by xiigen-flows-dot-net-angular::HistoryBootstrapService.SeedDocumentsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:138::xiigen-flows-dot-net-angular::HistoryBootstrapService.RunBootstrapAsync
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:88::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.SeedDocumentsRoutesDesignReasoningAndArchitecturePatterns
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Tests\HistoryBootstrapServiceTests.cs:105::xiigen-flows-dot-net-angular::HistoryBootstrapServiceTests.RetrieveArchitecturePatternsReturnsOnlyGlobalScope

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:31::xiigen-flows-dot-net-angular::HistoryBootstrapService.ParseArchitectureSeed

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-45-history-bootstrap\server\Xiigen.Flow.HistoryBootstrap.Application\HistoryBootstrapService.cs:63::xiigen-flows-dot-net-angular::HistoryBootstrapService.SeedDocumentsAsync