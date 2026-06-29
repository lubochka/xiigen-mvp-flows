# xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.RegisterConnection

## Meaning
xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.RegisterConnection implements local project behavior with return type DataProcessResult<ClientPushConnectionInfo>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:135::xiigen-flows-dot-net-angular::ActiveConnection.ActiveConnection. It is called by xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:64::xiigen-flows-dot-net-angular::ClientPushService.RegisterConnectionAsync.

## Location
flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:31

## Parameters
- string correlationId
- IReadOnlyDictionary<string
- object?> activeFlowState

## Return
DataProcessResult<ClientPushConnectionInfo> returned by xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.RegisterConnection

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:64::xiigen-flows-dot-net-angular::ClientPushService.RegisterConnectionAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:135::xiigen-flows-dot-net-angular::ActiveConnection.ActiveConnection

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:31::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.RegisterConnection