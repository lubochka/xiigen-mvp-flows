# xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.SubscriptionActivationValidatesPaymentAndDoesNotLeakAcrossTenants

## Meaning
xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.SubscriptionActivationValidatesPaymentAndDoesNotLeakAcrossTenants implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:459::xiigen-flows-dot-net-angular::EventAttendanceService.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:44::xiigen-flows-dot-net-angular::SubscriptionBillingService.PublishPlanAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:148::xiigen-flows-dot-net-angular::SubscriptionBillingService.SubscribeAsync. No internal caller was detected.

## Location
flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:146

## Parameters
- none recorded

## Return
Task returned by xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.SubscriptionActivationValidatesPaymentAndDoesNotLeakAcrossTenants

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:459::xiigen-flows-dot-net-angular::EventAttendanceService.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:44::xiigen-flows-dot-net-angular::SubscriptionBillingService.PublishPlanAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:148::xiigen-flows-dot-net-angular::SubscriptionBillingService.SubscribeAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:146::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.SubscriptionActivationValidatesPaymentAndDoesNotLeakAcrossTenants