# xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToStyle

## Meaning
xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToStyle implements local project behavior with return type DataProcessResult<FramerTextStyle>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:58::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizeFontWeight, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:98::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizeNumber, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:128::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizePaint. It is called by xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:177::xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:140::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.StyleMappingPrefersFillsOverColorAndPreservesTypography, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:157::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.TextAttributeBuilderAndUpdateMirrorFramerRuntimePayload, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:311::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.OutputFixture.

## Location
flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:159

## Parameters
- FramerNode node

## Return
DataProcessResult<FramerTextStyle> returned by xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToStyle

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:177::xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:140::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.StyleMappingPrefersFillsOverColorAndPreservesTypography
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:157::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.TextAttributeBuilderAndUpdateMirrorFramerRuntimePayload
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:311::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.OutputFixture

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:58::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizeFontWeight
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:98::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizeNumber
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:128::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizePaint

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:159::xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToStyle