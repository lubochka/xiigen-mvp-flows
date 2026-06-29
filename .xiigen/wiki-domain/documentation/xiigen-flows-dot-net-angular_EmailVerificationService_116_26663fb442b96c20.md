# xiigen-flows-dot-net-angular::EmailVerificationService.ResendAsync

## Meaning
xiigen-flows-dot-net-angular::EmailVerificationService.ResendAsync implements local project behavior with return type Task<DataProcessResult<EmailVerificationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:155::xiigen-flows-dot-net-angular::EmailVerificationService.IssueTokenAsync, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:208::xiigen-flows-dot-net-angular::EmailVerificationService.SupersedeActiveTokensAsync, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:228::xiigen-flows-dot-net-angular::EmailVerificationService.GetNumberAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:224::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_ResendRateLimit_IsTenantAndUserScoped.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:116

## Parameters
- string userId
- string email
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EmailVerificationOutcome>> returned by xiigen-flows-dot-net-angular::EmailVerificationService.ResendAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:224::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_ResendRateLimit_IsTenantAndUserScoped

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:155::xiigen-flows-dot-net-angular::EmailVerificationService.IssueTokenAsync
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:208::xiigen-flows-dot-net-angular::EmailVerificationService.SupersedeActiveTokensAsync
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:228::xiigen-flows-dot-net-angular::EmailVerificationService.GetNumberAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:116::xiigen-flows-dot-net-angular::EmailVerificationService.ResendAsync