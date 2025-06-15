# Introduction

## 1.1 Purpose

SnapDog2 is engineered as a sophisticated and comprehensive **multi-zone audio management system**. Its primary function is to serve as a central control plane within a modern smart home environment, specifically designed to orchestrate audio playback across various physically distinct areas or "zones". It acts as an intelligent bridge connecting diverse audio sources – encompassing popular **music streaming services** (via protocols like Subsonic), **internet radio stations**, and **local media files** – with a **Snapcast server** infrastructure responsible for synchronized, multi-room audio output.

Beyond basic playback management, a core design goal of SnapDog2 is seamless integration into broader smart home ecosystems. This is achieved through robust support for standard automation protocols, primarily **MQTT** for flexible, topic-based messaging and eventing, and **KNX** for direct integration with established building automation systems. This allows SnapDog2's audio functionalities to become integral parts of home automation scenes, routines, and control interfaces.

## 1.2 Core Objectives

The development of SnapDog2 is guided by the following fundamental objectives:

1. **Unified Audio Management**: Establish a single, cohesive system capable of discovering, managing, controlling, and monitoring audio playback across multiple independent or synchronized zones within a home. This includes managing sources, playlists, volume levels (per zone and per client), and playback states (Play, Pause, Stop).
2. **Smart Home Integration**: Provide seamless, bidirectional communication with common home automation platforms and protocols. This involves receiving commands and publishing detailed status updates via **MQTT** topics and **KNX** group addresses, allowing audio control to be integrated into dashboards, automations, and physical switches.
3. **Flexible Media Sources**: Design the system to support a variety of audio sources beyond basic file playback. Initial support targets **Subsonic API compatible servers** (like Navidrome, Airsonic, etc.) for personal media libraries and configurable **internet radio streams**. The architecture should allow for the addition of further source types (e.g., Spotify Connect, UPnP/DLNA) in the future.
4. **Diverse User Control Options**: Enable users and external systems to interact with SnapDog2 through multiple standardized interfaces. This includes control via **MQTT** messages, **KNX** telegrams, a well-defined **RESTful HTTP API**, and potentially future interfaces like voice assistants (requiring separate integration layers).
5. **Reliable & Resilient Operation**: Engineer the application for stability suitable for continuous, **24/7 operation** within a home environment. This necessitates robust error handling (using the Result pattern internally), automatic recovery from transient network or external service failures (using Polly resilience policies), and clear logging for diagnostics.
6. **Extensibility & Maintainability**: Implement a modular software architecture (CQRS with MediatR, clear layering) that simplifies maintenance and allows for future expansion. Adding support for new audio sources, communication protocols, or features should be achievable with minimal disruption to existing functionality. Clean code conventions and comprehensive testing support this goal.

## 1.3 Target Use Cases

SnapDog2 is designed to address several common smart home audio scenarios:

* **Multi-Room Audio Playback**: Enabling users to play the same music perfectly synchronized across multiple rooms (e.g., party mode) or play different audio streams independently in different zones (e.g., news in the kitchen, music in the living room) using Snapcast clients (like Raspberry Pis with DACs connected to amplifiers/speakers).
* **Centralized Entertainment Hub**: Acting as the primary control point for music playback, accessible through various smart home interfaces (wall panels using MQTT/KNX, mobile apps via the REST API, voice assistants via custom integrations).
* **IoT & Programmatic Audio Control**: Allowing other applications or scripts on the local network to control audio playback programmatically using well-defined protocols (MQTT, REST API), enabling custom integrations or advanced automation scenarios beyond typical smart home platforms.
