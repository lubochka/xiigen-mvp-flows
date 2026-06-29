# xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToElement

## Meaning
xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToElement implements local project behavior with return type DataProcessResult<FramerTextElement>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:98::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizeNumber. It is called by xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:177::xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:88::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.TextElementFallsBackToIdCharactersAndZeroDimensions, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:157::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.TextAttributeBuilderAndUpdateMirrorFramerRuntimePayload, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:311::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.OutputFixture.

## Location
flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:149

## Parameters
- FramerNode node

## Return
DataProcessResult<FramerTextElement> returned by xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToElement

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:177::xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:88::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.TextElementFallsBackToIdCharactersAndZeroDimensions
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:157::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.TextAttributeBuilderAndUpdateMirrorFramerRuntimePayload
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:311::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.OutputFixture

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:98::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizeNumber

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:149::xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToElement