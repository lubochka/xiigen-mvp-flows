# xiigen-flows-dot-net-angular::AdsPlatformService.EvaluateFraudAsync

## Meaning
xiigen-flows-dot-net-angular::AdsPlatformService.EvaluateFraudAsync implements local project behavior with return type Task<FraudReviewResult>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble, xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Application\DurableSagasComplianceService.cs:533::xiigen-flows-dot-net-angular::was.ReadNumberFreedomAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:65::xiigen-flows-dot-net-angular::AdsPlatformService.ProcessAdRequestAsync.

## Location
flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:546

## Parameters
- IReadOnlyDictionary<string
- object?> request
- long bidAmount
- CancellationToken cancellationToken

## Return
Task<FraudReviewResult> returned by xiigen-flows-dot-net-angular::AdsPlatformService.EvaluateFraudAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:65::xiigen-flows-dot-net-angular::AdsPlatformService.ProcessAdRequestAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble
- xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Application\DurableSagasComplianceService.cs:533::xiigen-flows-dot-net-angular::was.ReadNumberFreedomAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:546::xiigen-flows-dot-net-angular::AdsPlatformService.EvaluateFraudAsync