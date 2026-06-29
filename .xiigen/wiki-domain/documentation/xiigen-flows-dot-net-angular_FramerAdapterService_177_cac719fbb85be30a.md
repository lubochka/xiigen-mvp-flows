# xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes

## Meaning
xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes implements local project behavior with return type DataProcessResult<FramerTextReadResult>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:53::xiigen-flows-dot-net-angular::FramerAdapterService.IsFramerTextNode, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:149::xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToElement, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:159::xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToStyle. It is called by xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:240::xiigen-flows-dot-net-angular::FramerAdapterService.RunAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:73::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.ReadTextNodesFiltersMixedSelectionAndPreservesElementFields.

## Location
flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:177

## Parameters
- IReadOnlyList<FramerNode> nodes

## Return
DataProcessResult<FramerTextReadResult> returned by xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:240::xiigen-flows-dot-net-angular::FramerAdapterService.RunAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:73::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.ReadTextNodesFiltersMixedSelectionAndPreservesElementFields

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:53::xiigen-flows-dot-net-angular::FramerAdapterService.IsFramerTextNode
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:149::xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToElement
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:159::xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToStyle

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:177::xiigen-flows-dot-net-angular::FramerAdapterService.ReadTextNodes