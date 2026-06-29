# xiigen-flows-dot-net-angular::OssCurriculumService.CollectSignalsAsync

## Meaning
xiigen-flows-dot-net-angular::OssCurriculumService.CollectSignalsAsync implements local project behavior with return type Task<DataProcessResult<OssCurriculumOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:25::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.PublishAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:130::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.CollectSignalsStoresExactlyThreeCanonicalLearningSignals, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:147::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.ProgressPlateauRecommendsPreSeedingOnlyAndNeverAutoActs.

## Location
flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:194

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- string? expectedVersion
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<OssCurriculumOutcome>> returned by xiigen-flows-dot-net-angular::OssCurriculumService.CollectSignalsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:130::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.CollectSignalsStoresExactlyThreeCanonicalLearningSignals
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:147::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.ProgressPlateauRecommendsPreSeedingOnlyAndNeverAutoActs

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:25::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.PublishAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:194::xiigen-flows-dot-net-angular::OssCurriculumService.CollectSignalsAsync