# xiigen-flows-dot-net-angular::ClientPushServiceTests.TimeoutRetainsOutboxWithoutPublishingToPool

## Meaning
xiigen-flows-dot-net-angular::ClientPushServiceTests.TimeoutRetainsOutboxWithoutPublishingToPool implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:37::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.ListAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:64::xiigen-flows-dot-net-angular::ClientPushService.RegisterConnectionAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:86::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.GetActiveConnections. No internal caller was detected.

## Location
flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:178

## Parameters
- none recorded

## Return
Task returned by xiigen-flows-dot-net-angular::ClientPushServiceTests.TimeoutRetainsOutboxWithoutPublishingToPool

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:37::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.ListAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:64::xiigen-flows-dot-net-angular::ClientPushService.RegisterConnectionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:86::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.GetActiveConnections

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:178::xiigen-flows-dot-net-angular::ClientPushServiceTests.TimeoutRetainsOutboxWithoutPublishingToPool