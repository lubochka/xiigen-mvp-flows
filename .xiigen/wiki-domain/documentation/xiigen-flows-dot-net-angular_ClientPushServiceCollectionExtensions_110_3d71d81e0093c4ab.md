# xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.EventsForCurrentTenant

## Meaning
xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.EventsForCurrentTenant implements local project behavior with return type IReadOnlyList<ClientPushSseEvent>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:101::xiigen-flows-dot-net-angular::ClientPushServiceTests.BridgeEventStoresOutboxBeforeTenantScopedPoolPush.

## Location
flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:110

## Parameters
- string correlationId

## Return
IReadOnlyList<ClientPushSseEvent> returned by xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.EventsForCurrentTenant

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:101::xiigen-flows-dot-net-angular::ClientPushServiceTests.BridgeEventStoresOutboxBeforeTenantScopedPoolPush

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:110::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.EventsForCurrentTenant