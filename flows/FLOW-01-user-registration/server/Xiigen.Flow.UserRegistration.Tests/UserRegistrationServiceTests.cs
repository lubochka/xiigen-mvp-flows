using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Auth;
using Xiigen.Flow.UserRegistration.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Fabrics.Scheduler;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.UserRegistration.Tests;

public sealed class UserRegistrationServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = UserRegistrationBfaRuleRegistry.Rules.Select(rule => rule.RuleId).Order().ToArray();

        Assert.Equal(UserRegistrationConstants.RuleIds.Order(), ids);
        Assert.All(UserRegistrationBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(UserRegistrationConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF01_Registration_HashesCredentialsStoresPrivateRecordAndEmitsEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "anonymous");

        var result = await fixture.Registration.RegisterAsync(
            Registration("Luba@Example.COM", "correct horse battery staple"),
            ifMatch: null,
            idempotencyKey: "register-1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("PENDING_VERIFICATION", result.Data?.Body["accountStatus"]);

        var stored = await fixture.Documents.SearchAsync(
            UserRegistrationConstants.UserRegistrations,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["normalized_email"] = "luba@example.com" });
        var body = Assert.Single(stored.Data ?? []);
        Assert.Equal("PRIVATE", body.Body["knowledge_scope"]);
        Assert.Equal(Array.Empty<string>(), body.Body["onboarding_materials"]);
        Assert.NotEqual("correct horse battery staple", body.Body["credentials_hash"]);

        var verification = await fixture.PasswordHasher.VerifyAsync("correct horse battery staple", Convert.ToString(body.Body["credentials_hash"])!);
        Assert.True(verification.Data?.Matches);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == "xiigen.user-registration.registration-initiated.v1");
    }

    [Fact]
    public async Task CF850_ConcurrentSameEmailWrite_AllowsOneWinnerAndRejectsOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "anonymous");

        var first = fixture.Registration.RegisterAsync(
            Registration("race@example.com", "password-one", simulateSlowWrite: true),
            ifMatch: null,
            idempotencyKey: "race-a");
        var second = fixture.Registration.RegisterAsync(
            Registration("race@example.com", "password-two", simulateSlowWrite: true),
            ifMatch: null,
            idempotencyKey: "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF851_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "anonymous");

        var first = await fixture.Registration.RegisterAsync(
            Registration("replay@example.com", "password-one"),
            ifMatch: null,
            idempotencyKey: "same-key");
        var second = await fixture.Registration.RegisterAsync(
            Registration("replay@example.com", "password-one"),
            ifMatch: null,
            idempotencyKey: "same-key");

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["userId"], second.Data?.Body["userId"]);

        var users = await fixture.Documents.SearchAsync(
            UserRegistrationConstants.UserRegistrations,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["normalized_email"] = "replay@example.com" });
        Assert.Single(users.Data ?? []);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Single(outbox.Data ?? [], record => record.Event.Type == "xiigen.user-registration.registration-initiated.v1");
    }

    [Fact]
    public async Task CF852_BoundaryValidationRejectsMissingEmailAndLeavesNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "anonymous");

        var result = await fixture.Registration.RegisterAsync(
            new Dictionary<string, object?> { ["email"] = "", ["password"] = "valid-password" },
            ifMatch: null,
            idempotencyKey: "bad-input");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);

        var users = await fixture.Documents.SearchAsync(
            UserRegistrationConstants.UserRegistrations,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(users.Data ?? []);
    }

    [Fact]
    public async Task CF853_DownstreamTimeoutReturnsRetryHintAndRetainsOutboxRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "anonymous");

        var result = await fixture.Registration.RegisterAsync(
            Registration("timeout@example.com", "password-one", simulateFabricTimeout: true),
            ifMatch: null,
            idempotencyKey: "timeout-key");

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error?.Code);
        Assert.Equal(5000, result.Error?.Details?["retryAfterMs"]);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == "xiigen.user-registration.registration-timeout.v1");
    }

    [Fact]
    public async Task CF01_DuplicateEmail_IsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "anonymous"))
        {
            var first = await fixture.Registration.RegisterAsync(
                Registration("tenant@example.com", "password-one"),
                ifMatch: null,
                idempotencyKey: "tenant-a-one");
            var duplicate = await fixture.Registration.RegisterAsync(
                Registration("tenant@example.com", "password-two"),
                ifMatch: null,
                idempotencyKey: "tenant-a-two");

            Assert.True(first.IsSuccess, first.Error?.Message);
            Assert.True(duplicate.IsFailure);
            Assert.Equal("DUPLICATE_EMAIL", duplicate.Error?.Code);
        }

        using (fixture.PushContext("globex", "anonymous"))
        {
            var otherTenant = await fixture.Registration.RegisterAsync(
                Registration("tenant@example.com", "password-three"),
                ifMatch: null,
                idempotencyKey: "tenant-b-one");

            Assert.True(otherTenant.IsSuccess, otherTenant.Error?.Message);
        }
    }

    [Fact]
    public async Task CF01_EmailVerification_StoresOnlyTokenHashAndSchedulesExpiry()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.EmailVerification.InitiateAsync("usr-1", "Member@Example.com");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);

        var tokens = await fixture.Documents.SearchAsync(
            UserRegistrationConstants.VerificationTokens,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["user_id"] = "usr-1" });
        var token = Assert.Single(tokens.Data ?? []);
        Assert.True(token.Body.ContainsKey("token_hash"));
        Assert.False(token.Body.ContainsKey("rawToken"));

        var jobs = await fixture.Scheduler.ListAsync();
        Assert.Single(jobs.Data ?? []);
        Assert.Equal("user-registration-token-expiry", jobs.Data![0].Action);
    }

    [Fact]
    public async Task CF01_EmailVerification_ExpiredTokenIsMarkedUsedAndRejected()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Freedom.SetAsync("flow01_email_verification_ttl_seconds", -1, "number");
        var issue = await fixture.EmailVerification.InitiateAsync("usr-expired", "expired@example.com");
        var rawToken = Convert.ToString(issue.Data?.Body["rawToken"])!;

        var verify = await fixture.EmailVerification.VerifyAsync("usr-expired", rawToken);

        Assert.True(verify.IsFailure);
        Assert.Equal("EXPIRED_TOKEN", verify.Error?.Code);

        var stored = await fixture.Documents.SearchAsync(
            UserRegistrationConstants.VerificationTokens,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["user_id"] = "usr-expired" });
        Assert.Equal("used", stored.Data![0].Body["status"]);
    }

    [Fact]
    public async Task CF01_ResendRateLimit_IsTenantAndUserScoped()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.EmailVerification.ResendAsync("usr-resend", "resend@example.com");
        var second = await fixture.EmailVerification.ResendAsync("usr-resend", "resend@example.com");
        var anotherUser = await fixture.EmailVerification.ResendAsync("usr-other", "resend@example.com");

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsFailure);
        Assert.Equal("RATE_LIMIT_EXCEEDED", second.Error?.Code);
        Assert.True(anotherUser.IsSuccess, anotherUser.Error?.Message);
    }

    [Fact]
    public async Task CF01_OnboardingRequiresAllMaterialsBeforeCompletionEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.Onboarding.DeliverWorkspaceAsync("usr-onboarding");
        await fixture.Onboarding.DeliverTutorialAsync("usr-onboarding");
        var incomplete = await fixture.Onboarding.CompleteAsync("usr-onboarding", "onboard@example.com");
        await fixture.Onboarding.DeliverInvitationAsync("usr-onboarding");
        var complete = await fixture.Onboarding.CompleteAsync("usr-onboarding", "onboard@example.com");

        Assert.True(incomplete.IsFailure);
        Assert.Equal("DELIVERY_INCOMPLETE", incomplete.Error?.Code);
        Assert.True(complete.IsSuccess, complete.Error?.Message);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == "xiigen.user-registration.onboarding-completed.v1");
    }

    [Fact]
    public async Task CF01_SsoBypassCompletesWithoutExtraOutboxEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Onboarding.CompleteAsync(
            "usr-sso",
            "sso@example.com",
            existingOnboardingMaterialsCount: 3);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.True((bool)result.Data!.Body["ssoBypass"]!);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Type == "xiigen.user-registration.onboarding-completed.v1");
    }

    private static Dictionary<string, object?> Registration(
        string email,
        string password,
        bool simulateSlowWrite = false,
        bool simulateFabricTimeout = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["email"] = email,
            ["password"] = password
        };
        if (simulateSlowWrite)
        {
            request["simulateSlowWrite"] = true;
        }

        if (simulateFabricTimeout)
        {
            request["simulateFabricTimeout"] = true;
        }

        return request;
    }

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemorySchedulerService scheduler,
            InMemoryFreedomConfigService freedom,
            Pbkdf2PasswordHasherService passwordHasher,
            UserRegistrationService registration,
            EmailVerificationService emailVerification,
            OnboardingDeliveryService onboarding,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Scheduler = scheduler;
            Freedom = freedom;
            PasswordHasher = passwordHasher;
            Registration = registration;
            EmailVerification = emailVerification;
            Onboarding = onboarding;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemorySchedulerService Scheduler { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public Pbkdf2PasswordHasherService PasswordHasher { get; }
        public UserRegistrationService Registration { get; }
        public EmailVerificationService EmailVerification { get; }
        public OnboardingDeliveryService Onboarding { get; }
        private ITenantContextAccessor TenantContextAccessor { get; }
        private ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var scheduler = new InMemorySchedulerService();
            var freedom = new InMemoryFreedomConfigService();
            var passwordHasher = new Pbkdf2PasswordHasherService();
            var tenantAccessor = new AsyncLocalTenantContextAccessor();
            var correlationAccessor = new AsyncLocalCorrelationContextAccessor();
            var registration = new UserRegistrationService(
                documents,
                idempotency,
                outbox,
                passwordHasher,
                tenantAccessor,
                correlationAccessor,
                NullLogger<UserRegistrationService>.Instance);
            var emailVerification = new EmailVerificationService(
                documents,
                outbox,
                scheduler,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<EmailVerificationService>.Instance);
            var onboarding = new OnboardingDeliveryService(
                documents,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<OnboardingDeliveryService>.Instance);

            return new FlowFixture(
                documents,
                outbox,
                scheduler,
                freedom,
                passwordHasher,
                registration,
                emailVerification,
                onboarding,
                tenantAccessor,
                correlationAccessor);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlation = $"corr-{Guid.NewGuid():N}";
            var tenant = TenantContextAccessor.Push(new TenantContext(
                tenantId,
                role == "anonymous" ? null : "user-1",
                new HashSet<string> { role },
                correlation,
                "test",
                DateTimeOffset.UtcNow));
            var correlationScope = CorrelationContextAccessor.Push(new CorrelationContext(correlation, "test", DateTimeOffset.UtcNow));
            return new CompositeScope(tenant, correlationScope);
        }

        private sealed class CompositeScope : IDisposable
        {
            private readonly IDisposable _first;
            private readonly IDisposable _second;

            public CompositeScope(IDisposable first, IDisposable second)
            {
                _first = first;
                _second = second;
            }

            public void Dispose()
            {
                _second.Dispose();
                _first.Dispose();
            }
        }
    }
}
