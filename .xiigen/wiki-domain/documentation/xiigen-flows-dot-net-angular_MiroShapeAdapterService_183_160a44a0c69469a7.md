# xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync

## Meaning
xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync implements local project behavior with return type Task<DataProcessResult<MiroShapeAdapterOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:191::xiigen-flows-dot-net-angular::CanvaTextAdapterService.ValidateRunRequest, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:28::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ReadBoard, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:85::xiigen-flows-dot-net-angular::MiroShapeAdapterService.WriteToBoardAsync, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:122::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ClassifyMiroShapes, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:142::xiigen-flows-dot-net-angular::MiroShapeAdapterService.AnalyzeSpatialLayout. No internal caller was detected.

## Location
flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:183

## Parameters
- IReadOnlyDictionary<string
- object?> request
- IMiroWriter writer
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<MiroShapeAdapterOutcome>> returned by xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:191::xiigen-flows-dot-net-angular::CanvaTextAdapterService.ValidateRunRequest
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:28::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ReadBoard
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:85::xiigen-flows-dot-net-angular::MiroShapeAdapterService.WriteToBoardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:122::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ClassifyMiroShapes
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:142::xiigen-flows-dot-net-angular::MiroShapeAdapterService.AnalyzeSpatialLayout

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:183::xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync