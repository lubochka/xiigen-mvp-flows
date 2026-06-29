# xiigen-flows-dot-net-angular::CanvaTextAdapterService.RecordPublicationAsync

## Meaning
xiigen-flows-dot-net-angular::CanvaTextAdapterService.RecordPublicationAsync implements local project behavior with return type Task<DataProcessResult<CanvaTextAdapterOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Tests\CanvaTextAdapterServiceTests.cs:166::xiigen-flows-dot-net-angular::CanvaTextAdapterServiceTests.PublicationRequiresPackageEvidenceAndRecordsGlobalApproval, xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Tests\CanvaTextAdapterServiceTests.cs:183::xiigen-flows-dot-net-angular::CanvaTextAdapterServiceTests.DashboardIsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:217::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.PublicationRequiresPackageEvidenceAndRecordsGlobalApproval, xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:235::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.DashboardIsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Tests\WebflowDesignerExtensionServiceTests.cs:185::xiigen-flows-dot-net-angular::WebflowDesignerExtensionServiceTests.PublicationRequiresPackageEvidenceAndRecordsGlobalApproval, xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Tests\WebflowDesignerExtensionServiceTests.cs:202::xiigen-flows-dot-net-angular::WebflowDesignerExtensionServiceTests.DashboardIsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:235::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.PublicationRequiresPackageEvidenceAndRecordsGlobalApproval, xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:252::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.DashboardIsTenantScoped.

## Location
flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:104

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<CanvaTextAdapterOutcome>> returned by xiigen-flows-dot-net-angular::CanvaTextAdapterService.RecordPublicationAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Tests\CanvaTextAdapterServiceTests.cs:166::xiigen-flows-dot-net-angular::CanvaTextAdapterServiceTests.PublicationRequiresPackageEvidenceAndRecordsGlobalApproval
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Tests\CanvaTextAdapterServiceTests.cs:183::xiigen-flows-dot-net-angular::CanvaTextAdapterServiceTests.DashboardIsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:217::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.PublicationRequiresPackageEvidenceAndRecordsGlobalApproval
- xiigen-flows-dot-net-angular::flows\FLOW-42-miro-shape-adapter\server\Xiigen.Flow.MiroShapeAdapter.Tests\MiroShapeAdapterServiceTests.cs:235::xiigen-flows-dot-net-angular::MiroShapeAdapterServiceTests.DashboardIsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Tests\WebflowDesignerExtensionServiceTests.cs:185::xiigen-flows-dot-net-angular::WebflowDesignerExtensionServiceTests.PublicationRequiresPackageEvidenceAndRecordsGlobalApproval
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Tests\WebflowDesignerExtensionServiceTests.cs:202::xiigen-flows-dot-net-angular::WebflowDesignerExtensionServiceTests.DashboardIsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:235::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.PublicationRequiresPackageEvidenceAndRecordsGlobalApproval
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Tests\FramerAdapterServiceTests.cs:252::xiigen-flows-dot-net-angular::FramerAdapterServiceTests.DashboardIsTenantScoped

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:104::xiigen-flows-dot-net-angular::CanvaTextAdapterService.RecordPublicationAsync