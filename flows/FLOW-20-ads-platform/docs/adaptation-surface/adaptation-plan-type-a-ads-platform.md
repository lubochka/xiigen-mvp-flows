# FLOW-20 Ads Platform Type A Adaptation Surface

Type A adaptation may change the visible module name, advertising lane labels, synthetic advertiser names, marketplace listing copy, audience examples, and review-queue copy.

Locked behavior:
- Consent gate enforcement covers T625: consent is checked first and is unconditional. Missing, revoked, or expired consent blocks the request before auction, budget, fraud, billing, or delivery state changes.
- Auction bid processing covers T626: bid locks use a SETNX-equivalent reservation before budget mutation, and budget mutation uses DECRBY/INCRBY-equivalent semantics.
- Fraud pre-billing validation covers T627: fraud scoring runs before billing, the threshold is resolved through FREEDOM, rejected bids restore budget, and raw payment account data is never stored.
- Political content review covers T628: two model scores converge through `Math.Min`, divergent or threshold-reaching political content requires human review, and ambiguous content is never auto-blocked without a human decision.
- FLOW-20 also covers the original T287 through T306 plan-step inventory. Every task type must have a single-responsibility plan step, use fabric interfaces, avoid entity-specific controllers, and return `DataProcessResult`.
- Tenant scope is read from tenant context; request payload tenant ids are rejected.
- Idempotent retries replay the cached response and create no duplicate consent, auction, budget, fraud, review, delivery, or event records.
- Timeout simulation returns `TIMEOUT` with `retryAfterMs` and retains a recovery outbox event without confirming delivery state.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
