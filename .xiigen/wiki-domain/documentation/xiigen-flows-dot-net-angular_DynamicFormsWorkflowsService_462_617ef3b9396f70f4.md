# xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ValidatePlanStepCoverage

## Meaning
xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ValidatePlanStepCoverage implements local project behavior with return type void. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:63::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.PublishSchemaAsync, xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:413::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ValidatePlanCoverage.

## Location
flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:462

## Parameters
- IReadOnlyList<DynamicFormsPlanStep> steps
- List<Dictionary<string
- object?>> errors

## Return
void returned by xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ValidatePlanStepCoverage

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:63::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.PublishSchemaAsync
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:413::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ValidatePlanCoverage

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:462::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ValidatePlanStepCoverage