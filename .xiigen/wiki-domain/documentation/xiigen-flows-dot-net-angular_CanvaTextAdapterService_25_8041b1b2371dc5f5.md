# xiigen-flows-dot-net-angular::CanvaTextAdapterService.ReadSelection

## Meaning
xiigen-flows-dot-net-angular::CanvaTextAdapterService.ReadSelection implements local project behavior with return type DataProcessResult<CanvaAdapterReadResult>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:38::xiigen-flows-dot-net-angular::CanvaTextAdapterService.MapCanvaToElement, xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:173::xiigen-flows-dot-net-angular::CanvaTextAdapterService.MapCanvaToStyle. It is called by xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:77::xiigen-flows-dot-net-angular::CanvaTextAdapterService.RunAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Tests\CanvaTextAdapterServiceTests.cs:63::xiigen-flows-dot-net-angular::CanvaTextAdapterServiceTests.ReadPathMapsCanvaTextElementsToSharedContract.

## Location
flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:25

## Parameters
- IReadOnlyList<CanvaTextElement> canvaElements

## Return
DataProcessResult<CanvaAdapterReadResult> returned by xiigen-flows-dot-net-angular::CanvaTextAdapterService.ReadSelection

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:77::xiigen-flows-dot-net-angular::CanvaTextAdapterService.RunAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Tests\CanvaTextAdapterServiceTests.cs:63::xiigen-flows-dot-net-angular::CanvaTextAdapterServiceTests.ReadPathMapsCanvaTextElementsToSharedContract

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:38::xiigen-flows-dot-net-angular::CanvaTextAdapterService.MapCanvaToElement
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:173::xiigen-flows-dot-net-angular::CanvaTextAdapterService.MapCanvaToStyle

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:25::xiigen-flows-dot-net-angular::CanvaTextAdapterService.ReadSelection