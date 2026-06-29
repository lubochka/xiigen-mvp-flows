# xiigen-flows-dot-net-angular::UserRegistrationServiceTests.Registration

## Meaning
xiigen-flows-dot-net-angular::UserRegistrationServiceTests.Registration implements local project behavior with return type object?>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:30::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_Registration_HashesCredentialsStoresPrivateRecordAndEmitsEvent, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:60::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF850_ConcurrentSameEmailWrite_AllowsOneWinnerAndRejectsOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:81::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF851_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:131::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF853_DownstreamTimeoutReturnsRetryHintAndRetainsOutboxRecord, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:150::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_DuplicateEmail_IsTenantScoped.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:277

## Parameters
- string email
- string password
- bool simulateSlowWrite
- bool simulateFabricTimeout

## Return
object?> returned by xiigen-flows-dot-net-angular::UserRegistrationServiceTests.Registration

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:30::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_Registration_HashesCredentialsStoresPrivateRecordAndEmitsEvent
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:60::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF850_ConcurrentSameEmailWrite_AllowsOneWinnerAndRejectsOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:81::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF851_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:131::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF853_DownstreamTimeoutReturnsRetryHintAndRetainsOutboxRecord
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:150::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_DuplicateEmail_IsTenantScoped

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:277::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.Registration