# xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject implements local project behavior with return type object?>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:39::xiigen-flows-dot-net-angular::EventManagementService.CreateEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:439::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateDocumentAsync, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:742::xiigen-flows-dot-net-angular::SchemaRegistryDagService.CreateSchemaBody, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:768::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateRegistrationRequest, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:392::xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasCoreAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:64::xiigen-flows-dot-net-angular::ClientPushService.RegisterConnectionAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:156::xiigen-flows-dot-net-angular::PlatformAgentService.PublishActionAsync, xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:181::xiigen-flows-dot-net-angular::PlatformAgentService.ContributePatternAsync.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981

## Parameters
- IReadOnlyDictionary<string
- object?> values
- string key

## Return
object?> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:39::xiigen-flows-dot-net-angular::EventManagementService.CreateEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:439::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateDocumentAsync
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:742::xiigen-flows-dot-net-angular::SchemaRegistryDagService.CreateSchemaBody
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:768::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateRegistrationRequest
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:392::xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasCoreAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:64::xiigen-flows-dot-net-angular::ClientPushService.RegisterConnectionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:156::xiigen-flows-dot-net-angular::PlatformAgentService.PublishActionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-46-platform-agent\server\Xiigen.Flow.PlatformAgent.Application\PlatformAgentService.cs:181::xiigen-flows-dot-net-angular::PlatformAgentService.ContributePatternAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject