# xiigen-flows-dot-net-angular::BundleStatusTrackerService.OnFlowRegeneratedAsync

## Meaning
xiigen-flows-dot-net-angular::BundleStatusTrackerService.OnFlowRegeneratedAsync implements local project behavior with return type Task<DataProcessResult<int>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:102::xiigen-flows-dot-net-angular::BundleStatusTrackerService.IsVersionBelow, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:129::xiigen-flows-dot-net-angular::BundleStatusTrackerService.ReadMap, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:142::xiigen-flows-dot-net-angular::BundleActivationServiceTests.StatusTracker_UpdatesAllBundlesContainingRegeneratedFlow.

## Location
flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:27

## Parameters
- string flowId
- string newVersion
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<int>> returned by xiigen-flows-dot-net-angular::BundleStatusTrackerService.OnFlowRegeneratedAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:142::xiigen-flows-dot-net-angular::BundleActivationServiceTests.StatusTracker_UpdatesAllBundlesContainingRegeneratedFlow

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:102::xiigen-flows-dot-net-angular::BundleStatusTrackerService.IsVersionBelow
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:129::xiigen-flows-dot-net-angular::BundleStatusTrackerService.ReadMap
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:27::xiigen-flows-dot-net-angular::BundleStatusTrackerService.OnFlowRegeneratedAsync