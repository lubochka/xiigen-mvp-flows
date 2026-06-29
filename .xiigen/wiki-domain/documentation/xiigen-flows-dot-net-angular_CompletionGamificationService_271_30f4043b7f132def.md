# xiigen-flows-dot-net-angular::CompletionGamificationService.GetCompletionStatusAsync

## Meaning
xiigen-flows-dot-net-angular::CompletionGamificationService.GetCompletionStatusAsync implements local project behavior with return type Task<DataProcessResult<CompletionGamificationStatus>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:614::xiigen-flows-dot-net-angular::CompletionGamificationService.FindCompletionAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:623::xiigen-flows-dot-net-angular::CompletionGamificationService.FindLedgerAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:632::xiigen-flows-dot-net-angular::CompletionGamificationService.GetStreakAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:95::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0504_CompletionData_IsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:268::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_StatusReportsNotStartedAndCompletedStates.

## Location
flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:271

## Parameters
- string lessonId
- string learnerId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<CompletionGamificationStatus>> returned by xiigen-flows-dot-net-angular::CompletionGamificationService.GetCompletionStatusAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:95::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0504_CompletionData_IsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:268::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_StatusReportsNotStartedAndCompletedStates

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:614::xiigen-flows-dot-net-angular::CompletionGamificationService.FindCompletionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:623::xiigen-flows-dot-net-angular::CompletionGamificationService.FindLedgerAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:632::xiigen-flows-dot-net-angular::CompletionGamificationService.GetStreakAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:271::xiigen-flows-dot-net-angular::CompletionGamificationService.GetCompletionStatusAsync