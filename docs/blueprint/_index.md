# SnapDog2 Blueprint - Table of Contents

This directory contains the complete technical blueprint for SnapDog2, a sophisticated multi-zone audio management system. The blueprint has been split into individual chapters for better organization and navigation.

## Chapters

1. [Introduction](01-introduction.md)
2. [Coding Style & Conventions](02-coding-style-conventions.md)
3. [System Architecture](03-system-architecture.md)
4. [Core Components & State Management](04-core-components-state-management.md)
5. [Cross-Cutting Concerns](05-cross-cutting-concerns.md)
6. [MediatR Implementation (Server Layer)](06-mediatr-implementation-server-layer.md)
7. [Fault Tolerance Implementation (Infrastructure Layer)](07-fault-tolerance-implementation-infrastructure-layer.md)
8. [Security Implementation (API Layer)](08-security-implementation-api-layer.md)
9. [Command Framework](09-command-framework.md)
10. [Configuration System](10-configuration-system.md)
11. [API Specification](11-api-specification.md)
12. [Infrastructure Services Implementation](12-infrastructure-services-implementation.md)
13. [Metrics and Telemetry (Infrastructure Layer)](13-metrics-and-telemetry-infrastructure-layer.md)
14. [Zone-to-Client Mapping Strategy (Server Layer)](14-zone-to-client-mapping-strategy-server-layer.md)
15. [Development Environment](15-development-environment.md)
16. [Dependencies](16-dependencies.md)
17. [Docker Infrastructure](17-docker-infrastructure.md)
18. [Testing Strategy](18-testing-strategy.md)
19. [Deployment and Operations](19-deployment-and-operations.md)
20. [Appendices](20-appendices.md)
21. [Implementation Plan](21-implementation-plan.md)
22. [Achieving High-Quality Code (AI & Human Collaboration)](22-achieving-high-quality-code-ai-human-collaboration.md)

## Overview

SnapDog2 is engineered as a sophisticated and comprehensive **multi-zone audio management system**. Its primary function is to serve as a central control plane within a modern smart home environment, specifically designed to orchestrate audio playback across various physically distinct areas or "zones".

The system integrates with:
- **Snapcast server** infrastructure for synchronized, multi-room audio output
- **Music streaming services** via protocols like Subsonic
- **Internet radio stations**
- **Local media files**
- **MQTT** for flexible, topic-based messaging and eventing
- **KNX** for direct integration with building automation systems

## Architecture

The application employs a **modular, service-oriented architecture** using:
- **.NET 9.0** framework with modern C# features
- **CQRS pattern** with MediatR
- **Result pattern** for error handling
- **Dependency injection** with built-in .NET DI container
- **Comprehensive logging** with Serilog
- **OpenTelemetry** for observability

## Getting Started

For implementation details, start with:
1. [Introduction](01-introduction.md) - Core objectives and use cases
2. [Coding Style & Conventions](02-coding-style-conventions.md) - Development standards
3. [System Architecture](03-system-architecture.md) - High-level design overview
4. [Implementation Plan](21-implementation-plan.md) - Step-by-step development phases
