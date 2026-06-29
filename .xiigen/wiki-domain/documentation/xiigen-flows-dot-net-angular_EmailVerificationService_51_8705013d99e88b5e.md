# xiigen-flows-dot-net-angular::EmailVerificationService.VerifyAsync

## Meaning
xiigen-flows-dot-net-angular::EmailVerificationService.VerifyAsync implements local project behavior with return type Task<DataProcessResult<EmailVerificationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:251::xiigen-flows-dot-net-angular::EmailVerificationService.HashToken, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:30::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_Registration_HashesCredentialsStoresPrivateRecordAndEmitsEvent, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:204::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_EmailVerification_ExpiredTokenIsMarkedUsedAndRejected.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:51

## Parameters
- string userId
- string rawToken
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EmailVerificationOutcome>> returned by xiigen-flows-dot-net-angular::EmailVerificationService.VerifyAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:30::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_Registration_HashesCredentialsStoresPrivateRecordAndEmitsEvent
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:204::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_EmailVerification_ExpiredTokenIsMarkedUsedAndRejected

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:251::xiigen-flows-dot-net-angular::EmailVerificationService.HashToken
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\EmailVerificationService.cs:51::xiigen-flows-dot-net-angular::EmailVerificationService.VerifyAsync