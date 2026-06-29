# xiigen-flows-dot-net-angular::FlowFixture.CreateAsync

## Meaning
xiigen-flows-dot-net-angular::FlowFixture.CreateAsync implements local project behavior with return type Task<FlowFixture>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:169::xiigen-flows-dot-net-angular::FlowFixture.FlowFixture, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:218::xiigen-flows-dot-net-angular::FlowFixture.SeedAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:24::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_StoresBeforeOutboxAndRunsDryRunBeforeFull, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:50::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_TreatsEmptyIfMatchAsCurrentVersion, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:65::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_ReplaysCachedResponseForSameIdempotencyKey, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:88::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_ReturnsConcurrentUpdateForStaleVersion, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:108::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_RejectsBoundaryValidationWithoutPartialWrite, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:125::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_TimeoutRetainsOutboxRecord, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:142::xiigen-flows-dot-net-angular::BundleActivationServiceTests.StatusTracker_UpdatesAllBundlesContainingRegeneratedFlow.

## Location
flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:202

## Parameters
- none recorded

## Return
Task<FlowFixture> returned by xiigen-flows-dot-net-angular::FlowFixture.CreateAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:24::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_StoresBeforeOutboxAndRunsDryRunBeforeFull
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:50::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_TreatsEmptyIfMatchAsCurrentVersion
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:65::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_ReplaysCachedResponseForSameIdempotencyKey
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:88::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_ReturnsConcurrentUpdateForStaleVersion
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:108::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_RejectsBoundaryValidationWithoutPartialWrite
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:125::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_TimeoutRetainsOutboxRecord
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:142::xiigen-flows-dot-net-angular::BundleActivationServiceTests.StatusTracker_UpdatesAllBundlesContainingRegeneratedFlow

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:169::xiigen-flows-dot-net-angular::FlowFixture.FlowFixture
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:218::xiigen-flows-dot-net-angular::FlowFixture.SeedAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:202::xiigen-flows-dot-net-angular::FlowFixture.CreateAsync