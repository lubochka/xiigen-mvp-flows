# xiigen-flows-dot-net-angular::MiroShapeAdapterService.ReadBoard

## Meaning
xiigen-flows-dot-net-angular::MiroShapeAdapterService.ReadBoard implements local project behavior with return type DataProcessResult<MiroAdapterReadResult>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:41::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapMiroToElement, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:60::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapMiroToStyleResult. It is called by xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:183::xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:64::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.ReadPathMapsMiroBoardItemsToSharedArchitectureContracts.

## Location
flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:28

## Parameters
- IReadOnlyList<MiroBoardItem> boardItems

## Return
DataProcessResult<MiroAdapterReadResult> returned by xiigen-flows-dot-net-angular::MiroShapeAdapterService.ReadBoard

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:183::xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:64::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.ReadPathMapsMiroBoardItemsToSharedArchitectureContracts

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:41::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapMiroToElement
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:60::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapMiroToStyleResult

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:28::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ReadBoard