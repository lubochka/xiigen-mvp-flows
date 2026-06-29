# xiigen-flows-dot-net-angular::FramerAdapterService.RunAdapterAsync

## Meaning
xiigen-flows-dot-net-angular::FramerAdapterService.RunAdapterAsync implements local project behavior with return type Task<DataProcessResult<FramerAdapterOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:191::xiigen-flows-dot-net-angular::CanvaTextAdapterService.ValidateRunRequest, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:177::xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:214::xiigen-flows-dot-net-angular::FramerAdapterService.WriteTextElements, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:235::xiigen-flows-dot-net-angular::FramerAdapterService.WriteStoredTextAdapterState. No internal caller was detected.

## Location
flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:240

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<FramerAdapterOutcome>> returned by xiigen-flows-dot-net-angular::FramerAdapterService.RunAdapterAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:191::xiigen-flows-dot-net-angular::CanvaTextAdapterService.ValidateRunRequest
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:177::xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:214::xiigen-flows-dot-net-angular::FramerAdapterService.WriteTextElements
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:235::xiigen-flows-dot-net-angular::FramerAdapterService.WriteStoredTextAdapterState

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:240::xiigen-flows-dot-net-angular::FramerAdapterService.RunAdapterAsync