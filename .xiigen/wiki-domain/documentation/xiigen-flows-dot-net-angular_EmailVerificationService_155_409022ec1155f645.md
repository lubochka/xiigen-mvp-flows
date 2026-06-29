# xiigen-flows-dot-net-angular::EmailVerificationService.IssueTokenAsync

## Meaning
xiigen-flows-dot-net-angular::EmailVerificationService.IssueTokenAsync implements local project behavior with return type Task<DataProcessResult<EmailVerificationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:228::xiigen-flows-dot-net-angular::EmailVerificationService.GetNumberAsync, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:251::xiigen-flows-dot-net-angular::EmailVerificationService.HashToken. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:38::xiigen-flows-dot-net-angular::EmailVerificationService.InitiateAsync, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:116::xiigen-flows-dot-net-angular::EmailVerificationService.ResendAsync.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:155

## Parameters
- string userId
- string email
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EmailVerificationOutcome>> returned by xiigen-flows-dot-net-angular::EmailVerificationService.IssueTokenAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:38::xiigen-flows-dot-net-angular::EmailVerificationService.InitiateAsync
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:116::xiigen-flows-dot-net-angular::EmailVerificationService.ResendAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:228::xiigen-flows-dot-net-angular::EmailVerificationService.GetNumberAsync
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:251::xiigen-flows-dot-net-angular::EmailVerificationService.HashToken

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:155::xiigen-flows-dot-net-angular::EmailVerificationService.IssueTokenAsync