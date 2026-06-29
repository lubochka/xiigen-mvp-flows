# xiigen-flows-dot-net-angular::ValidateTokenTenantWhenPresent

## Meaning
xiigen-flows-dot-net-angular::ValidateTokenTenantWhenPresent implements local project behavior with return type IResult?. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Api\Program.cs:156::xiigen-flows-dot-net-angular::TryReadTenantFromLocalTestToken. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Api\Program.cs:192::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Api\Program.cs:95::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Api\Program.cs:152::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Api\Program.cs:178::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Api\Program.cs:140::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Api\Program.cs:159::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Api\Program.cs:131::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Api\Program.cs:107::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Api\Program.cs:173::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Api\Program.cs:188::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Api\Program.cs:209::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Api\Program.cs:152::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Api\Program.cs:136::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Api\Program.cs:136::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Api\Program.cs:137::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Api\Program.cs:147::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Api\Program.cs:157::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Api\Program.cs:163::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Api\Program.cs:182::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Api\Program.cs:160::xiigen-flows-dot-net-angular::RequireRole, xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Api\Program.cs:179::xiigen-flows-dot-net-angular::RequireRole.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Api\Program.cs:173

## Parameters
- HttpContext context

## Return
IResult? returned by xiigen-flows-dot-net-angular::ValidateTokenTenantWhenPresent

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Api\Program.cs:192::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Api\Program.cs:95::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Api\Program.cs:152::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Api\Program.cs:178::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Api\Program.cs:140::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Api\Program.cs:159::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Api\Program.cs:131::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Api\Program.cs:107::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Api\Program.cs:173::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Api\Program.cs:188::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Api\Program.cs:209::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Api\Program.cs:152::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Api\Program.cs:136::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Api\Program.cs:136::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Api\Program.cs:137::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Api\Program.cs:147::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Api\Program.cs:157::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Api\Program.cs:163::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Api\Program.cs:182::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Api\Program.cs:160::xiigen-flows-dot-net-angular::RequireRole
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Api\Program.cs:179::xiigen-flows-dot-net-angular::RequireRole

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Api\Program.cs:156::xiigen-flows-dot-net-angular::TryReadTenantFromLocalTestToken

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Api\Program.cs:173::xiigen-flows-dot-net-angular::ValidateTokenTenantWhenPresent