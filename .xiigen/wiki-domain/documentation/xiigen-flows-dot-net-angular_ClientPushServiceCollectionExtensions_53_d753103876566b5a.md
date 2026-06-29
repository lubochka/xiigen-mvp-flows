# xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.PushEvent

## Meaning
xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.PushEvent implements local project behavior with return type DataProcessResult<bool>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:180::xiigen-flows-dot-net-angular::ClientPushService.RunKeepaliveAsync.

## Location
flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:53

## Parameters
- string correlationId
- ClientPushSseEvent sseEvent

## Return
DataProcessResult<bool> returned by xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.PushEvent

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:180::xiigen-flows-dot-net-angular::ClientPushService.RunKeepaliveAsync

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:53::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.PushEvent