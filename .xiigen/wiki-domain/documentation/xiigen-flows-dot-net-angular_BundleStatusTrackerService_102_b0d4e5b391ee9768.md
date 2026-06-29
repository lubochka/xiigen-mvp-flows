# xiigen-flows-dot-net-angular::BundleStatusTrackerService.IsVersionBelow

## Meaning
xiigen-flows-dot-net-angular::BundleStatusTrackerService.IsVersionBelow implements local project behavior with return type bool. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:843::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ParseVersion. It is called by xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:27::xiigen-flows-dot-net-angular::BundleStatusTrackerService.OnFlowRegeneratedAsync.

## Location
flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:102

## Parameters
- string current
- string minimum

## Return
bool returned by xiigen-flows-dot-net-angular::BundleStatusTrackerService.IsVersionBelow

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:27::xiigen-flows-dot-net-angular::BundleStatusTrackerService.OnFlowRegeneratedAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:843::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ParseVersion

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:102::xiigen-flows-dot-net-angular::BundleStatusTrackerService.IsVersionBelow