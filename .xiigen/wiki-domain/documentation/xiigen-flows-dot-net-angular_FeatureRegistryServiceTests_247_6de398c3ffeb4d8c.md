# xiigen-flows-dot-net-angular::FeatureRegistryServiceTests.FlowFixture

## Meaning
xiigen-flows-dot-net-angular::FeatureRegistryServiceTests.FlowFixture implements local project behavior with return type record. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:240::xiigen-flows-dot-net-angular::CompositeScope.CompositeScope, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create. No internal caller was detected.

## Location
flows\FLOW-36-feature-registry\server\Xiigen.Flow.FeatureRegistry.Tests\FeatureRegistryServiceTests.cs:247

## Parameters
- FeatureRegistryService Service
- InMemoryDocumentStore Documents
- InMemoryIdempotencyStore Idempotency
- InMemoryOutboxStore Outbox
- AsyncLocalTenantContextAccessor TenantAccessor
- AsyncLocalCorrelationContextAccessor CorrelationAccessor

## Return
record returned by xiigen-flows-dot-net-angular::FeatureRegistryServiceTests.FlowFixture

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:240::xiigen-flows-dot-net-angular::CompositeScope.CompositeScope
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-36-feature-registry\server\Xiigen.Flow.FeatureRegistry.Tests\FeatureRegistryServiceTests.cs:247::xiigen-flows-dot-net-angular::FeatureRegistryServiceTests.FlowFixture