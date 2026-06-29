# xiigen-flows-dot-net-angular::InMemoryCanvaWriter.WriteAsync

## Meaning
xiigen-flows-dot-net-angular::InMemoryCanvaWriter.WriteAsync implements local project behavior with return type Task<DataProcessResult<bool>>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:54::xiigen-flows-dot-net-angular::CanvaTextAdapterService.WriteToCanvasAsync, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:85::xiigen-flows-dot-net-angular::MiroShapeAdapterService.WriteToBoardAsync.

## Location
flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Infrastructure\CanvaTextAdapterServiceCollectionExtensions.cs:27

## Parameters
- CanvaWritePayload payload
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<bool>> returned by xiigen-flows-dot-net-angular::InMemoryCanvaWriter.WriteAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:54::xiigen-flows-dot-net-angular::CanvaTextAdapterService.WriteToCanvasAsync
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:85::xiigen-flows-dot-net-angular::MiroShapeAdapterService.WriteToBoardAsync

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Infrastructure\CanvaTextAdapterServiceCollectionExtensions.cs:27::xiigen-flows-dot-net-angular::InMemoryCanvaWriter.WriteAsync