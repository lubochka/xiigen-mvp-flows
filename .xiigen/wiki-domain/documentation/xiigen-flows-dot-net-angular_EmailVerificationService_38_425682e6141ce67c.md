# xiigen-flows-dot-net-angular::EmailVerificationService.InitiateAsync

## Meaning
xiigen-flows-dot-net-angular::EmailVerificationService.InitiateAsync implements local project behavior with return type Task<DataProcessResult<EmailVerificationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:155::xiigen-flows-dot-net-angular::EmailVerificationService.IssueTokenAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:181::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_EmailVerification_StoresOnlyTokenHashAndSchedulesExpiry, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:204::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_EmailVerification_ExpiredTokenIsMarkedUsedAndRejected.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:38

## Parameters
- string userId
- string email
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EmailVerificationOutcome>> returned by xiigen-flows-dot-net-angular::EmailVerificationService.InitiateAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:181::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_EmailVerification_StoresOnlyTokenHashAndSchedulesExpiry
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:204::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_EmailVerification_ExpiredTokenIsMarkedUsedAndRejected

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:155::xiigen-flows-dot-net-angular::EmailVerificationService.IssueTokenAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:38::xiigen-flows-dot-net-angular::EmailVerificationService.InitiateAsync