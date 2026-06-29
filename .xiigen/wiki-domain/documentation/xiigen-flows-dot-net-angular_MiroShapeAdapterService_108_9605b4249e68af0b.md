# xiigen-flows-dot-net-angular::MiroShapeAdapterService.ClassifyMiroShape

## Meaning
xiigen-flows-dot-net-angular::MiroShapeAdapterService.ClassifyMiroShape implements local project behavior with return type DataProcessResult<MiroShapeClassification>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:128::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ParseMiroContent. It is called by xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:41::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapMiroToElement, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:60::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapMiroToStyleResult, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:122::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ClassifyMiroShapes, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:270::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.AssertClassification.

## Location
flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:108

## Parameters
- MiroBoardItem item

## Return
DataProcessResult<MiroShapeClassification> returned by xiigen-flows-dot-net-angular::MiroShapeAdapterService.ClassifyMiroShape

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:41::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapMiroToElement
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:60::xiigen-flows-dot-net-angular::MiroShapeAdapterService.MapMiroToStyleResult
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:122::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ClassifyMiroShapes
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:270::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.AssertClassification

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:128::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ParseMiroContent

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:108::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ClassifyMiroShape