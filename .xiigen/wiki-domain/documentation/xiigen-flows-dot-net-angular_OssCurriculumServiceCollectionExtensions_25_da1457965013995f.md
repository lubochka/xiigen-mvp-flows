# xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.PublishAsync

## Meaning
xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.PublishAsync implements local project behavior with return type Task<DataProcessResult<QueueDispatchRecord>>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:160::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF22_3_ScheduleUsesSetNxEquivalentLockAndDuplicateDoesNotDuplicateOutbox, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:64::xiigen-flows-dot-net-angular::OssCurriculumService.AssignTierAsync, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:123::xiigen-flows-dot-net-angular::OssCurriculumService.SubmitShadowRunsAsync, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:194::xiigen-flows-dot-net-angular::OssCurriculumService.CollectSignalsAsync, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:259::xiigen-flows-dot-net-angular::OssCurriculumService.TrackProgressAsync.

## Location
flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:25

## Parameters
- CloudEventEnvelope envelope
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<QueueDispatchRecord>> returned by xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.PublishAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:160::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF22_3_ScheduleUsesSetNxEquivalentLockAndDuplicateDoesNotDuplicateOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:64::xiigen-flows-dot-net-angular::OssCurriculumService.AssignTierAsync
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:123::xiigen-flows-dot-net-angular::OssCurriculumService.SubmitShadowRunsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:194::xiigen-flows-dot-net-angular::OssCurriculumService.CollectSignalsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:259::xiigen-flows-dot-net-angular::OssCurriculumService.TrackProgressAsync

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:25::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.PublishAsync