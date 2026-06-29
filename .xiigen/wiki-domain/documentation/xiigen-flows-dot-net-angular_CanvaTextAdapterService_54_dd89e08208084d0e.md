# xiigen-flows-dot-net-angular::CanvaTextAdapterService.WriteToCanvasAsync

## Meaning
xiigen-flows-dot-net-angular::CanvaTextAdapterService.WriteToCanvasAsync implements local project behavior with return type Task<DataProcessResult<CanvaAdapterWriteResult>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:48::xiigen-flows-dot-net-angular::CanvaTextAdapterService.MapStyleToCanvaPayload, xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Infrastructure\CanvaTextAdapterServiceCollectionExtensions.cs:27::xiigen-flows-dot-net-angular::InMemoryCanvaWriter.WriteAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:77::xiigen-flows-dot-net-angular::CanvaTextAdapterService.RunAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Tests\CanvaTextAdapterServiceTests.cs:115::xiigen-flows-dot-net-angular::CanvaTextAdapterServiceTests.WritePathCountsWriterFailuresWithoutAbortingBatch.

## Location
flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:54

## Parameters
- IReadOnlyList<CanvaEnhancedOutput> enhanced
- ICanvaWriter writer
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<CanvaAdapterWriteResult>> returned by xiigen-flows-dot-net-angular::CanvaTextAdapterService.WriteToCanvasAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:77::xiigen-flows-dot-net-angular::CanvaTextAdapterService.RunAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Tests\CanvaTextAdapterServiceTests.cs:115::xiigen-flows-dot-net-angular::CanvaTextAdapterServiceTests.WritePathCountsWriterFailuresWithoutAbortingBatch

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:48::xiigen-flows-dot-net-angular::CanvaTextAdapterService.MapStyleToCanvaPayload
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Infrastructure\CanvaTextAdapterServiceCollectionExtensions.cs:27::xiigen-flows-dot-net-angular::InMemoryCanvaWriter.WriteAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:54::xiigen-flows-dot-net-angular::CanvaTextAdapterService.WriteToCanvasAsync