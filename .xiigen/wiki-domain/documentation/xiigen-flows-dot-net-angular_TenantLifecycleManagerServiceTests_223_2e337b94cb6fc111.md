# xiigen-flows-dot-net-angular::TenantLifecycleManagerServiceTests.FlowFixture

## Meaning
xiigen-flows-dot-net-angular::TenantLifecycleManagerServiceTests.FlowFixture implements local project behavior with return type record. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:240::xiigen-flows-dot-net-angular::CompositeScope.CompositeScope, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create. No internal caller was detected.

## Location
flows\FLOW-30-tenant-lifecycle-manager\server\Xiigen.Flow.TenantLifecycleManager.Tests\TenantLifecycleManagerServiceTests.cs:223

## Parameters
- TenantLifecycleManagerService Service
- InMemoryDocumentStore Documents
- InMemoryIdempotencyStore Idempotency
- InMemoryOutboxStore Outbox
- AsyncLocalTenantContextAccessor TenantAccessor
- AsyncLocalCorrelationContextAccessor CorrelationAccessor

## Return
record returned by xiigen-flows-dot-net-angular::TenantLifecycleManagerServiceTests.FlowFixture

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
- xiigen-flows-dot-net-angular::flows\FLOW-30-tenant-lifecycle-manager\server\Xiigen.Flow.TenantLifecycleManager.Tests\TenantLifecycleManagerServiceTests.cs:223::xiigen-flows-dot-net-angular::TenantLifecycleManagerServiceTests.FlowFixture