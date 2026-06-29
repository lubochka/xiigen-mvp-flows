# xiigen-flows-dot-net-angular::OnboardingDeliveryService.CompleteAsync

## Meaning
xiigen-flows-dot-net-angular::OnboardingDeliveryService.CompleteAsync implements local project behavior with return type Task<DataProcessResult<OnboardingOutcome>>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:240::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_OnboardingRequiresAllMaterialsBeforeCompletionEvent, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:260::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_SsoBypassCompletesWithoutExtraOutboxEvent.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:51

## Parameters
- string userId
- string email
- int existingOnboardingMaterialsCount
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<OnboardingOutcome>> returned by xiigen-flows-dot-net-angular::OnboardingDeliveryService.CompleteAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:240::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_OnboardingRequiresAllMaterialsBeforeCompletionEvent
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:260::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_SsoBypassCompletesWithoutExtraOutboxEvent

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:51::xiigen-flows-dot-net-angular::OnboardingDeliveryService.CompleteAsync