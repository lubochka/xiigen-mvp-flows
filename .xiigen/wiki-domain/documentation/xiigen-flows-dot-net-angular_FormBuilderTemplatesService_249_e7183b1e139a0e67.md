# xiigen-flows-dot-net-angular::FormBuilderTemplatesService.ValidateSchema

## Meaning
xiigen-flows-dot-net-angular::FormBuilderTemplatesService.ValidateSchema implements local project behavior with return type void. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:129::xiigen-flows-dot-net-angular::BundleStatusTrackerService.ReadMap, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:35::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.SaveTemplateAsync.

## Location
flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:249

## Parameters
- IReadOnlyDictionary<string
- object?> schema
- IReadOnlyList<string> requiredFields
- List<IReadOnlyDictionary<string
- object?>> errors

## Return
void returned by xiigen-flows-dot-net-angular::FormBuilderTemplatesService.ValidateSchema

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:35::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.SaveTemplateAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:129::xiigen-flows-dot-net-angular::BundleStatusTrackerService.ReadMap
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:249::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.ValidateSchema