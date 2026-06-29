# xiigen-flows-dot-net-angular::MiroShapeAdapterService.AnalyzeSpatialLayout

## Meaning
xiigen-flows-dot-net-angular::MiroShapeAdapterService.AnalyzeSpatialLayout implements local project behavior with return type DataProcessResult<MiroSpatialAnalysis>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:163::xiigen-flows-dot-net-angular::MiroShapeAdapterService.Build, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:174::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ContainsItem, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:284::xiigen-flows-dot-net-angular::MiroShapeAdapterService.InferLayoutMode, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:294::xiigen-flows-dot-net-angular::MiroShapeAdapterService.EstimateGap, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:307::xiigen-flows-dot-net-angular::MiroShapeAdapterService.EstimatePadding. It is called by xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:183::xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:115::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.SpatialAnalyzerBuildsContainmentTreeAndRowLayout, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:134::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.SpatialAnalyzerRecognizesColumnAndAbsoluteLayouts.

## Location
flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:142

## Parameters
- IReadOnlyList<MiroBoardItem> items
- decimal tolerance

## Return
DataProcessResult<MiroSpatialAnalysis> returned by xiigen-flows-dot-net-angular::MiroShapeAdapterService.AnalyzeSpatialLayout

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:183::xiigen-flows-dot-net-angular::MiroShapeAdapterService.RunAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:115::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.SpatialAnalyzerBuildsContainmentTreeAndRowLayout
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:134::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.SpatialAnalyzerRecognizesColumnAndAbsoluteLayouts

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:163::xiigen-flows-dot-net-angular::MiroShapeAdapterService.Build
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:174::xiigen-flows-dot-net-angular::MiroShapeAdapterService.ContainsItem
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:284::xiigen-flows-dot-net-angular::MiroShapeAdapterService.InferLayoutMode
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:294::xiigen-flows-dot-net-angular::MiroShapeAdapterService.EstimateGap
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:307::xiigen-flows-dot-net-angular::MiroShapeAdapterService.EstimatePadding

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:142::xiigen-flows-dot-net-angular::MiroShapeAdapterService.AnalyzeSpatialLayout