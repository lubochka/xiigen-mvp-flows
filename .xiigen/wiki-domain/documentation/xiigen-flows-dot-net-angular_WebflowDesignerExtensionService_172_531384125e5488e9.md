# xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.ReadDesignerElements

## Meaning
xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.ReadDesignerElements implements local project behavior with return type DataProcessResult<WebflowAdapterReadResult>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:130::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.MapElementProperties, xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:153::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.ComputeValidationDiff. It is called by xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:186::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RunAdapterAsync, xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Tests\WebflowDesignerExtensionServiceTests.cs:139::xiigen-flows-dot-net-angular::WebflowDesignerExtensionServiceTests.ReadDesignerElementsRoutesPropertiesAndComputesDiffs.

## Location
flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:172

## Parameters
- IReadOnlyList<WebflowElement> elements

## Return
DataProcessResult<WebflowAdapterReadResult> returned by xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.ReadDesignerElements

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:186::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RunAdapterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Tests\WebflowDesignerExtensionServiceTests.cs:139::xiigen-flows-dot-net-angular::WebflowDesignerExtensionServiceTests.ReadDesignerElementsRoutesPropertiesAndComputesDiffs

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:130::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.MapElementProperties
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:153::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.ComputeValidationDiff

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:172::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.ReadDesignerElements