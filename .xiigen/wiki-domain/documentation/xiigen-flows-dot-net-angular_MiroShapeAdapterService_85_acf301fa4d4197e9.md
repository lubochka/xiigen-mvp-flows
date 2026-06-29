# xiigen-flows-dot-net-angular::MiroShapeAdapterService.WriteToBoardAsync

## Meaning
xiigen-flows-dot-net-angular::MiroShapeAdapterService.WriteToBoardAsync implements local project behavior with return type Task<DataProcessResult<MiroAdapterWriteResult>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Infrastructure\CanvaTextAdapterServiceCollectionExtensions.cs:27::xiigen-flows-dot-net-angular::InMemoryCanvaWriter.WriteAsync, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:66::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapStyleToMiroPayload. It is called by xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:183::xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:165::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.WritePathCountsWriterFailuresWithoutAbortingBatch.

## Location
flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:85

## Parameters
- IReadOnlyList<ArchitectEnhancedOutput> enhanced
- IMiroWriter writer
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<MiroAdapterWriteResult>> returned by xiigen-flows-dot-net-angular::MiroShapeAdapterService.WriteToBoardAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:183::xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:165::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.WritePathCountsWriterFailuresWithoutAbortingBatch

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Infrastructure\CanvaTextAdapterServiceCollectionExtensions.cs:27::xiigen-flows-dot-net-angular::InMemoryCanvaWriter.WriteAsync
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:66::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapStyleToMiroPayload

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:85::xiigen-flows-dot-net-angular::MiroShapeAdapterService.WriteToBoardAsync