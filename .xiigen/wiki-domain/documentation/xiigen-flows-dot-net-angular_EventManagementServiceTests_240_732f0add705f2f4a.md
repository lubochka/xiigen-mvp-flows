# xiigen-flows-dot-net-angular::EventManagementServiceTests.CF862_ConcurrentRegistrationCapacity_AllowsOneWinnerAndRejectsOneLoser

## Meaning
xiigen-flows-dot-net-angular::EventManagementServiceTests.CF862_ConcurrentRegistrationCapacity_AllowsOneWinnerAndRejectsOneLoser implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\UserRegistrationService.cs:38::xiigen-flows-dot-net-angular::UserRegistrationService.RegisterAsync, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:39::xiigen-flows-dot-net-angular::EventManagementService.CreateEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:333::xiigen-flows-dot-net-angular::EventManagementServiceTests.EventRequest. No internal caller was detected.

## Location
flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:240

## Parameters
- none recorded

## Return
Task returned by xiigen-flows-dot-net-angular::EventManagementServiceTests.CF862_ConcurrentRegistrationCapacity_AllowsOneWinnerAndRejectsOneLoser

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\UserRegistrationService.cs:38::xiigen-flows-dot-net-angular::UserRegistrationService.RegisterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:39::xiigen-flows-dot-net-angular::EventManagementService.CreateEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:333::xiigen-flows-dot-net-angular::EventManagementServiceTests.EventRequest

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:240::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF862_ConcurrentRegistrationCapacity_AllowsOneWinnerAndRejectsOneLoser