# xiigen-flows-dot-net-angular::FramerAdapterService.NormalizePaint

## Meaning
xiigen-flows-dot-net-angular::FramerAdapterService.NormalizePaint implements local project behavior with return type DataProcessResult<FramerSolidPaint>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:117::xiigen-flows-dot-net-angular::FramerAdapterService.RgbaToPaint, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:369::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizeHexColor, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:381::xiigen-flows-dot-net-angular::FramerAdapterService.JsonElementToObject. It is called by xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:159::xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToStyle, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:126::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.PaintMappingConvertsRgbRgbaAndHex.

## Location
flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:128

## Parameters
- object? input
- double fallbackOpacity

## Return
DataProcessResult<FramerSolidPaint> returned by xiigen-flows-dot-net-angular::FramerAdapterService.NormalizePaint

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:159::xiigen-flows-dot-net-angular::FramerAdapterService.MapFramerTextNodeToStyle
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:126::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.PaintMappingConvertsRgbRgbaAndHex

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:117::xiigen-flows-dot-net-angular::FramerAdapterService.RgbaToPaint
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:369::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizeHexColor
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:381::xiigen-flows-dot-net-angular::FramerAdapterService.JsonElementToObject

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:128::xiigen-flows-dot-net-angular::FramerAdapterService.NormalizePaint