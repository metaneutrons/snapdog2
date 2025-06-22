# SnapDog2

![SnapDog2 Logo](assets/icons/snapdog-icon.svg)

## Overview

SnapDog2 is an open-source .NET-based smart home automation API and controller platform. It provides endpoints for KNX, MQTT, and Snapcast services, designed with a modular, CQRS-driven architecture. This project is a work in progress; features may be broken or incomplete.

> [!WARNING]
> This is absolutely non-functional work in progress; it's not usable at the moment. Please give me some weeks!

## Documentation

- **Blueprints & Design**: [docs/blueprint](docs/blueprint/)
- **Implementation Details**: [docs/implementation](docs/implementation/)

## Roadmap

- [x] Phase 0: Foundation & AI Setup
- [x] Phase 1: Core Domain Configuration
- [x] Phase 2: Infrastructure & External Services
- [x] Phase 3: Server Layer & Business Logic
- [x] Phase 4: API Layer & Security
- [x] Phase 5: Integration Protocols
  - [ ] Subsonic Integration
- [ ] Phase 6: Observability & Operations
- [ ] Phase 7: Advanced Features & Optimization

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Access to KNX, MQTT, and Snapcast services (for integration testing)

### Build & Test

```bash
dotnet build
dotnet test
```

## License

This project is licensed under the GNU GPL v3.0. See [LICENSE](LICENSE) for details.
