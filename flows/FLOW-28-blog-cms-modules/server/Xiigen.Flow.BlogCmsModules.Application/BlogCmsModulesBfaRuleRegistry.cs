namespace Xiigen.Flow.BlogCmsModules.Application;

public static class BlogCmsModulesBfaRuleRegistry
{
    public static readonly IReadOnlyList<BlogCmsModulesBfaRule> Rules =
        BlogCmsModulesConstants.RequiredBfaRules
            .Select(ruleId => new BlogCmsModulesBfaRule(
                ruleId,
                BlogCmsModulesConstants.FlowId,
                RuleType(ruleId),
                Description(ruleId),
                Severity(ruleId),
                int.Parse(ruleId[3..], System.Globalization.CultureInfo.InvariantCulture) >= 1003 ? "FLOW_SCOPED" : "CROSS_FLOW",
                "GLOBAL",
                "TENANT_CONTEXT"))
            .ToArray();

    private static string RuleType(string ruleId)
    {
        var number = int.Parse(ruleId[3..], System.Globalization.CultureInfo.InvariantCulture);
        return number switch
        {
            566 or 569 or 573 or 581 or 590 => "CONTENT_SCOPE_AND_PRIVACY",
            568 or 574 or 580 or 589 => "CACHE_AND_INDEX_ISOLATION",
            570 or 585 or 595 => "AI_BUDGET_AND_MODEL_SCOPE",
            571 or 591 => "QUEUE_FANOUT_RESILIENCE",
            576 or 577 or 583 or 599 => "SECURITY_GATE",
            579 or 588 => "MEDIA_AND_CDN_POLICY",
            582 or 584 or 592 => "STATE_MACHINE_RACE_GUARD",
            587 or 594 => "AUTH_AND_ROLE_BOUNDARY",
            >= 593 and <= 600 => "MULTI_TENANT_VARIANT_RULE",
            >= 1003 and <= 1006 => "P10_EDGE_CASE_ENFORCEMENT",
            _ => "BLOG_CMS_BFA_RULE"
        };
    }

    private static string Severity(string ruleId)
    {
        var number = int.Parse(ruleId[3..], System.Globalization.CultureInfo.InvariantCulture);
        return number is 566 or 569 or 573 or 577 or 581 or 583 or 589 or 590 or 592 || (number >= 1003 && number <= 1006) ? "BUILD_FAILURE" : "WARN_CONTINUE";
    }

    private static string Description(string ruleId)
    {
        var number = int.Parse(ruleId[3..], System.Globalization.CultureInfo.InvariantCulture);
        return number switch
        {
            566 => "Blog content uses blog.* namespace and cannot be consumed by generic content ownership handlers.",
            567 => "Published events are fine-grained blog.content.published events, not generic status_changed notifications.",
            568 => "Editor session keys use blog:session prefix and cannot collide with visual editor sessions.",
            569 => "Media uses blog_media_asset and blog-media index, distinct from DAM media assets.",
            570 => "Spam and enhancement AI calls run budget pre-checks before provider calls.",
            571 => "Hook fan-out uses capped batches to prevent queue saturation.",
            572 => "Approval timeout records stay owned by publish gate and are not auto-retried from DLQ.",
            573 => "Public slugs are scoped under tenant blog base path and avoid reserved platform prefixes.",
            574 => "Search index cascade writes only to blog-content tenant index and never search-* directly.",
            575 => "Sitemap rebuilds are debounced and rate limited.",
            576 => "Webhook destinations are SSRF checked on every retry with skipCache true.",
            577 => "Comments and rich content are XSS sanitized before storage.",
            578 => "Site config is tenant-scoped and never bleeds across FREEDOM overlays.",
            579 => "Media uploads check tenant storage quota before accepting assets.",
            580 => "RSS/feed cache invalidates together with page cache.",
            581 => "Public reads enforce published visibility and tenant scope.",
            582 => "Scheduled and manual publish use revision optimistic locking.",
            583 => "Extension sandbox forbids direct network/env access and enforces timeout.",
            584 => "Taxonomy propagation is event-sourced and idempotent to avoid orphan references.",
            585 => "AI enhancement uses tenant-provider circuit breaker before feedback blacklisting.",
            586 => "Sitemap route registers through route table instead of stale static route.",
            587 => "Comment rate limit uses user key when authenticated and IP only for anonymous.",
            588 => "Media variant URLs follow CDN signing policy and TTL.",
            589 => "Permalink changes enqueue canonical cache invalidation for the tenant.",
            590 => "Draft content is never indexed; search cascade listens only to blog.content.published.",
            591 => "Webhook retry uses exponential backoff and capped retries before DLQ.",
            592 => "Comment moderation and publish approval use separate reviewer pools.",
            593 => "Plugin hook names are pluginId-prefixed.",
            594 => "SCIM user provisioning remains authoritative for author profile conflicts.",
            595 => "AI model config resolves per tenant, not from global default.",
            596 => "Comment threading depth is FREEDOM-configured.",
            597 => "Content type schema evolution keeps existing revisions readable with dictionary parsing.",
            598 => "Editor session takeover displays active-owner warning.",
            599 => "Media uploads call configurable virus scan service before commit.",
            600 => "Archive retention follows tenant-configured retention days before permanent delete.",
            1003 => "Optimistic concurrency on POST /api/dynamic/blog-cms-modules returns 201 winner and 409 loser.",
            1004 => "X-Idempotency-Key replay returns cached 200 without duplicate side effects.",
            1005 => "Boundary validation returns 400 field details and writes no partial state.",
            1006 => "Fabric timeout returns 504 and retains outbox state for retry.",
            _ => $"Blog/CMS Modules rule {ruleId}."
        };
    }
}
