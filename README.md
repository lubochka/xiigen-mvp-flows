# XIIGen — Self-Building AI Code Generation Engine

## .NET microservices and Angular flows

XIIGen is a fabric-first AI code generation engine for agentic workflows. It turns business intent into application flows through planning, generation, orchestration, retrieval and review. This repository implements the same product as [xiigen-mvp](https://github.com/lubochka/xiigen-mvp) using .NET microservices and Angular clients.

Business behavior lives here. Shared result types, tenant context, fabric contracts and platform APIs come from [XIIGen Platform](https://github.com/lubochka/xiigen-platform-dot-net-angular). This split lets flows share infrastructure while keeping their own application boundaries.

## Explore the flows

| Area | Starting points |
| --- | --- |
| Onboarding and community | [Bundle activation](flows/FLOW-00-bundle-activation), [registration](flows/FLOW-01-user-registration), [groups](flows/FLOW-06-user-groups-communities) |
| Events and engagement | [Event management](flows/FLOW-03-event-management), [attendance](flows/FLOW-04-event-attendance), [gamification](flows/FLOW-05-completion-gamification) |
| Commerce | [Marketplace](flows/FLOW-08-marketplace), [billing](flows/FLOW-12-subscription-billing), [payments](flows/FLOW-16-marketplace-payments) |
| Workflow infrastructure | [Schema registry and DAG](flows/FLOW-11-schema-registry-dag), [visual flow engine](flows/FLOW-18-visual-flow-engine), [meta-flow engine](flows/FLOW-26-meta-flow-engine) |
| AI and human decisions | [Human interaction gate](flows/FLOW-27-human-interaction-gate), [adaptive RAG](flows/FLOW-29-adaptive-rag-deep-research), [arbitration](flows/FLOW-35-meta-arbitration-engine), [platform agent](flows/FLOW-46-platform-agent) |
| Content and extensibility | [Dynamic forms](flows/FLOW-21-dynamic-forms-workflows), [blog modules](flows/FLOW-28-blog-cms-modules), [plugin adapters](flows/FLOW-34-marketplace-plugin-adapter) |

Browse [flows/](flows) for the full source tree. Flow folders group application, infrastructure and API projects with client and deployment material. [FLOW-47's README](flows/FLOW-47-design-conversion-intake/README.md) describes the design-conversion intake boundary.

## Architecture and capabilities

XIIGen combines flow generation and orchestration with DAG descriptions, RAG-assisted reasoning, review and tenant-aware execution. Its meta-flows describe how applications are composed; business flows express the domain behavior being composed.

```text
Business intent + developer decisions
                |
Planning / generation / retrieval / arbitration
                |
Angular clients -> .NET flow APIs -> application services
                                      |
                         Shared XIIGen platform contracts
```

The .NET conventions are `DataProcessResult<T>` for results, `MicroserviceBase<TService>` for service context and shared fabric interfaces for documents, idempotency and the outbox. Provider-specific details belong behind infrastructure boundaries. The platform owns these shared contracts; each flow owns its business rules.

Claude, Codex, Cursor and other compatible coding agents can use the repository's [skills](docs/skills). The project context stays in files, while the developer remains responsible for intent, architecture and acceptance.

## Stack and getting started

Backends use C#, .NET 10 and ASP.NET Core. [global.json](global.json) selects SDK **10.0.300** with its configured roll-forward policy. [nuget.config](nuget.config) restores Xiigen packages from [local-packages/](local-packages) and public packages from NuGet.org.

From the repository root:

```sh
dotnet restore Xiigen.Flows.slnx
dotnet build Xiigen.Flows.slnx --configuration Release --no-restore
```

FLOW-00 is the starting point for the Angular client. Its [package manifest](flows/FLOW-00-bundle-activation/client/angular/package.json) specifies Angular **21.2**, TypeScript **5.9** and npm **11.4.2**. Use a Node.js version supported by that Angular toolchain, then run:

```sh
cd flows/FLOW-00-bundle-activation/client/angular
npm ci
npm start
```

The development client is served at `http://localhost:4200/`. See its [README](flows/FLOW-00-bundle-activation/client/angular/README.md) for Angular development commands. The root [PowerShell check script](scripts/pre-commit-check.ps1) contains the existing backend and FLOW-00 client check sequence.

## Repository and documentation map

- [flows/](flows) — business microservices, meta-flows, clients and flow-specific material.
- [Xiigen.Flows.slnx](Xiigen.Flows.slnx) — central .NET solution.
- [nuget.config](nuget.config) and [local-packages/](local-packages) — platform package consumption.
- [Skill usage guide](docs/skills/HOW-TO-USE-SKILLS.md) and [skills](docs/skills) — contributor and coding-agent guidance.
- [FAQ](docs/FAQ.md) — the same ten product questions as the reference XIIGen project, with .NET and Angular examples.

## Contributing

Read the [contributor guide](CONTRIBUTING.md), [support guidance](SUPPORT.md) and [security policy](SECURITY.md).

Use [issues](https://github.com/lubochka/xiigen-flows-dot-net-angular/issues) for bugs, documentation corrections and feature proposals. Identify the flow, intended user outcome and affected contract. Put shared infrastructure changes in the platform repository and coordinate package changes with consumers. Keep examples synthetic and exclude credentials and private tenant data.

## License and related projects

XIIGen's first-party licensing is GNU Affero General Public License version 3 only (`AGPL-3.0-only`). Third-party dependencies retain their own licenses. See [LICENSE](LICENSE), [NOTICE](NOTICE) and [third-party notices](THIRD_PARTY_NOTICES.md).

- [XIIGen Platform](https://github.com/lubochka/xiigen-platform-dot-net-angular) — shared .NET contracts, fabrics and APIs.
- [xiigen-mvp](https://github.com/lubochka/xiigen-mvp) — the parallel XIIGen implementation and product reference.

The engine consumes models and can produce learning signals. Model-weight training belongs to the separate XIIGen model core; see the [FAQ](docs/FAQ.md).
