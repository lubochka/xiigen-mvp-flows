# xiigen-flows-dot-net-angular::PlatformAgentService.PublishActionAsync

## Meaning
xiigen-flows-dot-net-angular::PlatformAgentService.PublishActionAsync implements local project behavior with return type object?>>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject. It is called by xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:55::xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:104::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.TenantScopeGatewayExceptionWritesAuditBeforeTargetAction, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:120::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.ActionPublishingStoresStateBeforeCloudEvent.

## Location
flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:156

## Parameters
- string sessionId
- IReadOnlyDictionary<string
- object?> action
- CancellationToken cancellationToken

## Return
object?>>> returned by xiigen-flows-dot-net-angular::PlatformAgentService.PublishActionAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:55::xiigen-flows-dot-net-angular::PlatformAgentService.RunAgentAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:104::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.TenantScopeGatewayExceptionWritesAuditBeforeTargetAction
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Tests\PlatformAgentServiceTests.cs:120::xiigen-flows-dot-net-angular::PlatformAgentServiceTests.ActionPublishingStoresStateBeforeCloudEvent

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:156::xiigen-flows-dot-net-angular::PlatformAgentService.PublishActionAsync