# XIIGen FAQ

These are the ten product questions from XIIGen, adapted to its .NET microservices and Angular clients. Start with the [README](../README.md) for the repository map.

## 1. Does the agent actually check its own work, or just say it did?

XIIGen separates generation from validation, scoring and review. The intended result is checked against its contract; an author's claim of completion is not the acceptance decision. The developer remains responsible for deciding whether the result meets the requested goal.

The [meta-flow engine](../flows/FLOW-26-meta-flow-engine), [arbitration engine](../flows/FLOW-35-meta-arbitration-engine) and [platform agent](../flows/FLOW-46-platform-agent) are the relevant places to explore. Flow test projects and [review skills](skills) support that separation. A checked-in test or historical report is distinct from the result of a particular execution.

## 2. Is this a hybrid architecture, or a wrapper around one model?

XIIGen combines model-assisted reasoning with deterministic business services, retrieval and orchestration. Fabric-first design separates contracts from provider-specific implementations. A business flow is more than a prompt sent to one model.

The .NET microservices consume the shared platform's result, tenant, document-store, idempotency and outbox contracts. This repository owns domain behavior and infrastructure adapters. See [adaptive RAG](../flows/FLOW-29-adaptive-rag-deep-research), the [meta-flow engine](../flows/FLOW-26-meta-flow-engine) and the companion [platform](https://github.com/lubochka/xiigen-platform-dot-net-angular).

## 3. Can the AI invent the algorithm, or does it only type the code?

The product separates intent, planning, decomposition, generation and review. An agent can propose an approach, explain its structure and help generate its implementation. The human sets the goal and constraints; review determines whether the proposal satisfies them.

Explore the [schema registry and DAG](../flows/FLOW-11-schema-registry-dag), [visual flow engine](../flows/FLOW-18-visual-flow-engine), [meta-flow engine](../flows/FLOW-26-meta-flow-engine) and [arbitration](../flows/FLOW-35-meta-arbitration-engine). Explicit workflow and service contracts keep creative planning connected to reproducible application behavior.

## 4. Does this engine train models, or use new training methods?

The engine consumes models; it does not itself train model weights. XIIGen's learning loop concerns feedback, preference signals and retrieval memory. Model-weight training belongs to the separate XIIGen model core.

Here, [RAG quality feedback](../flows/FLOW-38-rag-quality-feedback), [adaptive RAG](../flows/FLOW-29-adaptive-rag-deep-research) and [history bootstrap](../flows/FLOW-45-history-bootstrap) are related application areas. Recording feedback and improving retrieved knowledge are different from updating a model's weights.

## 5. What does “agentic” mean here, concretely?

An agent works toward a stated intent through bounded steps: retrieving context, planning, proposing or generating changes, and obtaining review or a human decision. Its actions belong to an explicit execution and tenant context.

The [platform-agent flow](../flows/FLOW-46-platform-agent) and [human-interaction gate](../flows/FLOW-27-human-interaction-gate) express that product model. Shared tenant and correlation contracts come from the platform. Agentic behavior does not mean the agent owns the product requirements or can silently expand its assigned scope.

## 6. How does this change the developer's role?

The developer owns intent, architecture, constraints and acceptance. Agents can execute bounded implementation and analysis tasks, but product decisions remain human decisions.

For these repositories, that also means owning the boundary between a flow and the shared platform. Put business behavior in the relevant flow and coordinate shared contract changes with the platform. The [skill usage guide](skills/HOW-TO-USE-SKILLS.md) and [skills](skills) keep working guidance alongside the source.

## 7. Which coding agent does this bet on — Claude, Codex or Cursor?

The project context lives in source files, contracts, documentation and skills rather than in one assistant's chat history. Claude, Codex, Cursor and other compatible coding tools can work from that durable context.

Load the [skills](skills) through the host's supported mechanism and apply them to the current task. The coding agent used to develop the system is distinct from the AI provider used by a running flow; choosing one does not require choosing the other.

## 8. Is this a real system, or a demo?

The repository contains business microservices and clients, not only a product description. Start with [FLOW-00's .NET server](../flows/FLOW-00-bundle-activation/server) and [Angular client](../flows/FLOW-00-bundle-activation/client/angular), then explore the [flow tree](../flows) and [central solution](../Xiigen.Flows.slnx).

The source includes domain applications, meta-flows and integration boundaries. Follow the documentation of the particular flow you want to use; for example, [design-conversion intake](../flows/FLOW-47-design-conversion-intake/README.md) documents its own boundary. The shared foundation is in the companion platform repository.

## 9. Isn't this just vibe coding with extra steps?

The structure is the point: intent, workflow contracts, generated output, review and human decisions remain explicit. XIIGen organizes generation around reusable application patterns rather than accepting a plausible model response as the finished product.

The .NET conventions include `DataProcessResult<T>`, tenant-aware service context and infrastructure contracts. The [cross-flow governance](../flows/FLOW-25-bfa-cross-flow-governance), [arbitration](../flows/FLOW-35-meta-arbitration-engine) and [human-interaction](../flows/FLOW-27-human-interaction-gate) flows provide concrete places to follow those responsibilities.

## 10. How does this relate to local LLMs, agent frameworks and AI gateways?

These are complementary layers. Models supply reasoning or generation; frameworks coordinate actions; retrieval supplies context; gateways can route provider requests. XIIGen connects these concerns to business flows, tenant context and application contracts.

Explore [adaptive RAG](../flows/FLOW-29-adaptive-rag-deep-research), [RAG feedback](../flows/FLOW-38-rag-quality-feedback) and the [meta-flow engine](../flows/FLOW-26-meta-flow-engine). A local model is an integration choice at the appropriate provider boundary. The XIIGen model core remains separate from the engine, and model selection does not replace the flow's business or tenant contracts.

[Back to the flows README](../README.md)
