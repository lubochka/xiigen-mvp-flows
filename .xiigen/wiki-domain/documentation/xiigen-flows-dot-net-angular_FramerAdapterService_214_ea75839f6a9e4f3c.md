# xiigen-flows-dot-net-angular::FramerAdapterService.WriteTextElements

## Meaning
xiigen-flows-dot-net-angular::FramerAdapterService.WriteTextElements implements local project behavior with return type DataProcessResult<FramerTextWriteResult>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:208::xiigen-flows-dot-net-angular::FramerAdapterService.CreateTextUpdate. It is called by xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:240::xiigen-flows-dot-net-angular::FramerAdapterService.RunAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:174::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.WriteTextElementsCountsSuccessfulAndFailedWritesWithoutThrowing.

## Location
flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:214

## Parameters
- IReadOnlyList<FramerTextOutput> outputs
- IReadOnlySet<string>? failingNodeIds

## Return
DataProcessResult<FramerTextWriteResult> returned by xiigen-flows-dot-net-angular::FramerAdapterService.WriteTextElements

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:240::xiigen-flows-dot-net-angular::FramerAdapterService.RunAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:174::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.WriteTextElementsCountsSuccessfulAndFailedWritesWithoutThrowing

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:208::xiigen-flows-dot-net-angular::FramerAdapterService.CreateTextUpdate

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:214::xiigen-flows-dot-net-angular::FramerAdapterService.WriteTextElements