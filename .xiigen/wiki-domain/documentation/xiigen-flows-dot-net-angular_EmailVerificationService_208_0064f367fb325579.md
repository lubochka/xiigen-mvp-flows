# xiigen-flows-dot-net-angular::EmailVerificationService.SupersedeActiveTokensAsync

## Meaning
xiigen-flows-dot-net-angular::EmailVerificationService.SupersedeActiveTokensAsync implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:116::xiigen-flows-dot-net-angular::EmailVerificationService.ResendAsync.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:208

## Parameters
- string userId
- CancellationToken cancellationToken

## Return
Task returned by xiigen-flows-dot-net-angular::EmailVerificationService.SupersedeActiveTokensAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:116::xiigen-flows-dot-net-angular::EmailVerificationService.ResendAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:208::xiigen-flows-dot-net-angular::EmailVerificationService.SupersedeActiveTokensAsync