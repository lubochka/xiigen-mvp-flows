# xiigen-flows-dot-net-angular::MiroShapeAdapterService.Build

## Meaning
xiigen-flows-dot-net-angular::MiroShapeAdapterService.Build implements local project behavior with return type MiroLayoutNode. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:284::xiigen-flows-dot-net-angular::MiroShapeAdapterService.InferLayoutMode, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:294::xiigen-flows-dot-net-angular::MiroShapeAdapterService.EstimateGap, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:307::xiigen-flows-dot-net-angular::MiroShapeAdapterService.EstimatePadding. It is called by xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:142::xiigen-flows-dot-net-angular::MiroShapeAdapterService.AnalyzeSpatialLayout.

## Location
flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:163

## Parameters
- MiroBoardItem item

## Return
MiroLayoutNode returned by xiigen-flows-dot-net-angular::MiroShapeAdapterService.Build

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:142::xiigen-flows-dot-net-angular::MiroShapeAdapterService.AnalyzeSpatialLayout

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:284::xiigen-flows-dot-net-angular::MiroShapeAdapterService.InferLayoutMode
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:294::xiigen-flows-dot-net-angular::MiroShapeAdapterService.EstimateGap
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:307::xiigen-flows-dot-net-angular::MiroShapeAdapterService.EstimatePadding

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Application\MiroShapeAdapterService.cs:163::xiigen-flows-dot-net-angular::MiroShapeAdapterService.Build