# xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_SsoBypassCompletesWithoutExtraOutboxEvent

## Meaning
xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_SsoBypassCompletesWithoutExtraOutboxEvent implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:51::xiigen-flows-dot-net-angular::OnboardingDeliveryService.CompleteAsync, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:37::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.ListAsync. No internal caller was detected.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:260

## Parameters
- none recorded

## Return
Task returned by xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_SsoBypassCompletesWithoutExtraOutboxEvent

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:51::xiigen-flows-dot-net-angular::OnboardingDeliveryService.CompleteAsync
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:37::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.ListAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:260::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_SsoBypassCompletesWithoutExtraOutboxEvent