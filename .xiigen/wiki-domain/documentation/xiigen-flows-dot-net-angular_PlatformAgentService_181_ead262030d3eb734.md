# xiigen-flows-dot-net-angular::PlatformAgentService.ContributePatternAsync

## Meaning
xiigen-flows-dot-net-angular::PlatformAgentService.ContributePatternAsync implements local project behavior with return type object?>>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:55::xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:136::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.PatternContributorPathASharesGlobalRecordsWithoutConsent, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:150::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.PatternContributorSanitizerFailureAbortsWithoutGlobalWrite.

## Location
flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:181

## Parameters
- string sessionId
- IReadOnlyDictionary<string
- object?> contribution
- CancellationToken cancellationToken

## Return
object?>>> returned by xiigen-flows-dot-net-angular::PlatformAgentService.ContributePatternAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:55::xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:136::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.PatternContributorPathASharesGlobalRecordsWithoutConsent
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:150::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.PatternContributorSanitizerFailureAbortsWithoutGlobalWrite

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:181::xiigen-flows-dot-net-angular::PlatformAgentService.ContributePatternAsync