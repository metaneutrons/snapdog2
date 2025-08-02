# 9. API Specification

## 9.1. API Design Philosophy

The SnapDog2 Application Programming Interface (API) is designed as a modern, **RESTful HTTP interface** providing comprehensive programmatic control over the audio management system. It serves as a primary integration point for web UIs, mobile applications, third-party services, and custom scripts operating within the local network.

The API structure and functionality directly map to the logical concepts defined in the **Command Framework (Section 9)**, ensuring consistency between different control methods (API, MQTT, KNX). It exposes resources representing the system's global state, individual zones, and connected clients.

Key design principles underpinning the API are:

1. **Command Framework Alignment**: API endpoints and their operations correspond directly to the defined Global, Zone, and Client commands and status updates. Retrieving a zone's state via the API (`GET /api/v1/zones/{zoneId}`) reflects the same information available via the `ZONE_STATE` status. Sending a command (`POST /api/v1/zones/{zoneId}/commands/play`) triggers the equivalent internal `PLAY` command logic.
2. **Resource-Oriented Design**: Follows standard REST conventions. Nouns identify resources (e.g., `/zones`, `/clients`, `/media/playlists`), and standard HTTP verbs dictate actions:
    * `GET`: Retrieve resource state or collections (safe, idempotent).
    * `PUT`: Update resource state or settings entirely (idempotent where applicable, e.g., setting volume, mute state, specific track/playlist).
    * `POST`: Trigger actions/commands that may not be idempotent (e.g., `play`, `pause`, `next_track`) or create new resources (though zone creation is handled via configuration in SnapDog2).
    * `DELETE`: Remove resources (not applicable to zones/clients in SnapDog2 due to static configuration).
3. **Consistent Response Structure**: All API responses, whether successful or indicating an error, adhere to a standardized JSON wrapper structure (`ApiResponse<T>`, defined in Section 11.4.1) containing status flags, data payload, error details, and a request ID for traceability.
4. **Statelessness**: The API is stateless. Each request from a client must contain all the information needed to understand and process the request. The server does not maintain client session state between requests. Authentication is handled per-request via API keys.
5. **Clear Versioning**: Uses URI path versioning (`/api/v1/`) to manage changes and ensure backward compatibility where possible.

## 9.2. Authentication and Authorization

Given SnapDog2's typical deployment within a trusted local network, the primary security mechanism focuses on preventing unauthorized *control* rather than complex user management or data protection.

* **API Key Authentication**: This is the **mandatory and sole implemented authentication method**. Clients **must** include a valid, pre-configured API key in the `X-API-Key` HTTP request header for all endpoints unless an endpoint is explicitly marked with `[AllowAnonymous]` (e.g., potentially `/health` endpoints). API keys are defined via `SNAPDOG_API_APIKEY_{n}` environment variables (See Section 10). The implementation details are found in Section 8 (`ApiKeyAuthenticationHandler`). Failure to provide a valid key results in a `401 Unauthorized` response.
* **Authorization**: Currently basic. Successful authentication grants access to all authorized endpoints. Finer-grained authorization (e.g., specific keys only allowed to control certain zones) is **not implemented** in the current scope but could be a future enhancement. A failed authorization check (if implemented later) would result in a `403 Forbidden` response.
* *(Future Considerations)*: While Bearer Tokens (JWT) or OAuth2 could be added later for more complex scenarios or third-party integrations, they are outside the current MVP scope.

## 9.3. API Structure (`/api/v1/`)

Base path: `/api/v1`. All endpoints below assume this prefix.

### 9.3.1. Global Endpoints

Endpoints for accessing system-wide information.

| Method | Path                   | Functionality ID  | Description                      | Success Response (`Data` field) |
| :----- | :--------------------- | :---------------- | :------------------------------- | :------------------------------ |
| `GET`  | `/system/status`       | `SYSTEM_STATUS`   | Get system online status         | `SystemStatusDto` { bool IsOnline, DateTime TimestampUtc } |
| `GET`  | `/system/errors`       | `ERROR_STATUS`    | Get recent system errors (TBD)   | `List<ErrorDetails>`            |
| `GET`  | `/system/version`      | `VERSION_INFO`    | Get software version             | `VersionDetails`                |
| `GET`  | `/system/stats`        | `SERVER_STATS`    | Get server performance statistics| `ServerStats`                   |

*(Note: DTO structures match Core Models where appropriate)*

### 9.3.2. Zone Endpoints

Endpoints for interacting with configured audio zones. **Zone creation/deletion/rename is not supported via API** as zones are defined via environment variables. **Indices are 1-based.**

| Method | Path                                       | Command/Status ID         | Description                        | Request Body / Params                           | Success Response (`Data` field) | HTTP Status |
| :----- | :----------------------------------------- | :------------------------ | :--------------------------------- | :---------------------------------------------- | :---------------------------- | :---------- |
| `GET`  | `/zones`                                   | -                         | List configured zones              | Query: `?page=1&pageSize=20&sortBy=name`      | Paginated `List<ZoneInfo>`    | 200 OK      |
| `GET`  | `/zones/{zoneId}`                          | `ZONE_STATE` (Full)     | Get details & full state for zone  | Path: `{zoneId}` (int)                        | `ZoneState`                   | 200 OK      |
| `GET`  | `/zones/{zoneId}/state`                    | `ZONE_STATE` (Full)     | Get full state JSON (alias for above)| Path: `{zoneId}` (int)                        | `ZoneState`                   | 200 OK      |
| `POST` | `/zones/{zoneId}/commands/play`            | `PLAY`                    | Start/resume playback              | Path: `{zoneId}`; Optional Body: `PlayRequest`| `object` (null or status update)| 202 Accepted|
| `POST` | `/zones/{zoneId}/commands/pause`           | `PAUSE`                   | Pause playback                     | Path: `{zoneId}`                              | `object` (null)               | 202 Accepted|
| `POST` | `/zones/{zoneId}/commands/stop`            | `STOP`                    | Stop playback                      | Path: `{zoneId}`                              | `object` (null)               | 202 Accepted|
| `POST` | `/zones/{zoneId}/commands/next_track`      | `TRACK_NEXT`              | Play next track                    | Path: `{zoneId}`                              | `object` (null)               | 202 Accepted|
| `POST` | `/zones/{zoneId}/commands/prev_track`      | `TRACK_PREVIOUS`          | Play previous track                | Path: `{zoneId}`                              | `object` (null)               | 202 Accepted|
| `PUT`  | `/zones/{zoneId}/track`                    | `TRACK`                   | Set track by **1-based** index     | Path: `{zoneId}`; Body: `SetTrackRequest`     | `object` (null or status update)| 202 Accepted|
| `PUT`  | `/zones/{zoneId}/playlist`                 | `PLAYLIST`                | Set playlist by **1-based** index/ID| Path: `{zoneId}`; Body: `SetPlaylistRequest`| `object` (null or status update)| 202 Accepted|
| `PUT`  | `/zones/{zoneId}/settings/volume`          | `VOLUME`                  | Set zone volume                    | Path: `{zoneId}`; Body: `VolumeSetRequest`   | `object` { int Volume }       | 200 OK      |
| `GET`  | `/zones/{zoneId}/settings/volume`          | `VOLUME_STATUS`           | Get current zone volume            | Path: `{zoneId}`                              | `object` { int Volume }       | 200 OK      |
| `POST` | `/zones/{zoneId}/settings/volume/up`       | `VOLUME_UP`               | Increase volume                    | Path: `{zoneId}`; Optional Body: `StepRequest`| `object` { int NewVolume }    | 200 OK      |
| `POST` | `/zones/{zoneId}/settings/volume/down`     | `VOLUME_DOWN`             | Decrease volume                    | Path: `{zoneId}`; Optional Body: `StepRequest`| `object` { int NewVolume }    | 200 OK      |
| `PUT`  | `/zones/{zoneId}/settings/mute`            | `MUTE`                    | Set mute state                     | Path: `{zoneId}`; Body: `MuteSetRequest`     | `object` { bool IsMuted }     | 200 OK      |
| `GET`  | `/zones/{zoneId}/settings/mute`            | `MUTE_STATUS`             | Get current mute state             | Path: `{zoneId}`                              | `object` { bool IsMuted }     | 200 OK      |
| `POST` | `/zones/{zoneId}/settings/mute/toggle`     | `MUTE_TOGGLE`             | Toggle mute state                  | Path: `{zoneId}`                              | `object` { bool IsMuted }     | 200 OK      |
| `PUT`  | `/zones/{zoneId}/settings/track_repeat`    | `TRACK_REPEAT`            | Set track repeat mode              | Path: `{zoneId}`; Body: `ModeSetRequest`     | `object` { bool TrackRepeat } | 200 OK      |
| `POST` | `/zones/{zoneId}/settings/track_repeat/toggle`|`TRACK_REPEAT_TOGGLE`    | Toggle track repeat mode           | Path: `{zoneId}`                              | `object` { bool TrackRepeat } | 200 OK      |
| `PUT`  | `/zones/{zoneId}/settings/playlist_repeat` | `PLAYLIST_REPEAT`         | Set playlist repeat mode           | Path: `{zoneId}`; Body: `ModeSetRequest`     | `object` { bool PlaylistRepeat } | 200 OK      |
| `POST` | `/zones/{zoneId}/settings/playlist_repeat/toggle`|`PLAYLIST_REPEAT_TOGGLE`| Toggle playlist repeat mode      | Path: `{zoneId}`                              | `object` { bool PlaylistRepeat } | 200 OK      |
| `PUT`  | `/zones/{zoneId}/settings/playlist_shuffle`| `PLAYLIST_SHUFFLE`        | Set playlist shuffle mode          | Path: `{zoneId}`; Body: `ModeSetRequest`     | `object` { bool PlaylistShuffle }| 200 OK      |
| `POST` | `/zones/{zoneId}/settings/playlist_shuffle/toggle`|`PLAYLIST_SHUFFLE_TOGGLE`| Toggle playlist shuffle mode     | Path: `{zoneId}`                              | `object` { bool PlaylistShuffle }| 200 OK      |

*Note on 200 vs 202: Simple state settings (Volume, Mute, Repeat, Shuffle) can return 200 OK with the updated state. Commands triggering potentially longer actions (Play, Pause, Stop, Next/Prev Track/Playlist) might return 202 Accepted immediately, with the actual state update reflected later via status endpoints or event streams.*

**Example Request DTOs (`/Api/Models` or `/Api/Controllers`):**

```csharp
namespace SnapDog2.Api.Models; // Example namespace

public record PlayRequest { public string? MediaUrl { get; set; } public int? TrackIndex { get; set; } } // 1-based index
public record SetTrackRequest { public required int Index { get; set; } } // 1-based index
public record SetPlaylistRequest { public string? Id { get; set; } public int? Index { get; set; } } // Provide Id OR 1-based index
public record VolumeSetRequest { public required int Level { get; set; } } // 0-100
public record MuteSetRequest { public required bool Enabled { get; set; } }
public record ModeSetRequest { public required bool Enabled { get; set; } } // For Repeat/Shuffle
public record StepRequest { public int Step { get; set; } = 5; } // Optional step for Vol Up/Down
```

**Example Response DTOs (subset examples):**

```csharp
// For GET /zones response item
public record ZoneInfo(int Id, string Name, string PlaybackStatus);
// Full GET /zones/{id} uses Core.Models.ZoneState

// For GET /clients response item
public record ClientInfo(int Id, string Name, bool Connected, int? ZoneId);
// Full GET /clients/{id} uses Core.Models.ClientState
```

### 9.3.3. Client Endpoints

Endpoints for interacting with discovered Snapcast clients.

| Method | Path                                    | Command/Status ID      | Description                | Request Body / Params           | Success Response (`Data` field) | HTTP Status |
| :----- | :-------------------------------------- | :--------------------- | :------------------------- | :------------------------------ | :---------------------------- | :---------- |
| `GET`  | `/clients`                              | -                      | List discovered clients    | Query: `?page=1&pageSize=20`    | Paginated `List<ClientInfo>`  | 200 OK      |
| `GET`  | `/clients/{clientId}`                   | `CLIENT_STATE` (Full)  | Get details for a client   | Path: `{clientId}` (int)      | `ClientState`                 | 200 OK      |
| `GET`  | `/clients/{clientId}/state`             | `CLIENT_STATE` (Full)  | Get full state JSON        | Path: `{clientId}` (int)      | `ClientState`                 | 200 OK      |
| `PUT`  | `/clients/{clientId}/settings/volume`   | `CLIENT_VOLUME`        | Set client volume          | Path: `{clientId}`; Body: `VolumeSetRequest` | `object` { int Volume }       | 200 OK      |
| `GET`  | `/clients/{clientId}/settings/volume`   | `CLIENT_VOLUME_STATUS` | Get client volume          | Path: `{clientId}`            | `object` { int Volume }       | 200 OK      |
| `PUT`  | `/clients/{clientId}/settings/mute`     | `CLIENT_MUTE`          | Set client mute state      | Path: `{clientId}`; Body: `MuteSetRequest` | `object` { bool IsMuted }     | 200 OK      |
| `GET`  | `/clients/{clientId}/settings/mute`     | `CLIENT_MUTE_STATUS`   | Get client mute state      | Path: `{clientId}`            | `object` { bool IsMuted }     | 200 OK      |
| `POST` | `/clients/{clientId}/settings/mute/toggle`| `CLIENT_MUTE_TOGGLE`   | Toggle client mute state   | Path: `{clientId}`            | `object` { bool IsMuted }     | 200 OK      |
| `PUT`  | `/clients/{clientId}/settings/latency`  | `CLIENT_LATENCY`       | Set client latency         | Path: `{clientId}`; Body: `LatencySetRequest`{ int Milliseconds } | `object` { int Latency }    | 200 OK      |
| `GET`  | `/clients/{clientId}/settings/latency`  | `CLIENT_LATENCY_STATUS`| Get client latency         | Path: `{clientId}`            | `object` { int Latency }    | 200 OK      |
| `PUT`  | `/clients/{clientId}/settings/zone`     | `CLIENT_ZONE`          | Assign client to zone      | Path: `{clientId}`; Body: `AssignZoneRequest`{ int ZoneId } (1-based) | `object` (null)           | 202 Accepted|
| `GET`  | `/clients/{clientId}/settings/zone`     | `CLIENT_ZONE_STATUS`   | Get client assigned zone   | Path: `{clientId}`            | `object` { int? ZoneId }    | 200 OK      |
| `PUT`  | `/clients/{clientId}/settings/name`     | `RENAME_CLIENT`        | Rename client in Snapcast  | Path: `{clientId}`; Body: `RenameRequest`{ string Name } | `object` { string Name }      | 200 OK      |

### 9.3.4. Media Management Endpoints

Endpoints for browsing available media sources (initially Subsonic and Radio).

| Method | Path                                    | Description                    | Success Response (`Data` field)     |
| :----- | :-------------------------------------- | :----------------------------- | :-------------------------------- |
| `GET`  | `/media/sources`                        | List configured media sources  | `List<MediaSourceInfo>` { string Id, string Type, string Name } |
| `GET`  | `/media/playlists`                      | List all available playlists   | Paginated `List<PlaylistInfo>`    |
| `GET`  | `/media/playlists/{playlistIdOrIndex}`  | Get details for a playlist     | `PlaylistWithTracks` { PlaylistInfo Info, List<TrackInfo> Tracks } |
| `GET`  | `/media/playlists/{playlistIdOrIndex}/tracks` | List tracks in a playlist    | Paginated `List<TrackInfo>`       |
| `GET`  | `/media/tracks/{trackId}`               | Get details for a track        | `TrackInfo`                       |

*(Note: `playlistIdOrIndex` accepts `1` or `"radio"` for the Radio playlist, `2+` or Subsonic IDs for others. `trackId` format depends on the source.)*

## 9.4. Request and Response Format

### 9.4.1. Standard Response Wrapper (`ApiResponse<T>`)

All API responses use a consistent JSON wrapper.

```csharp
// Defined in /Api/Models or /Core/Models if shared
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; } // Nullable Data field
    public ApiError? Error { get; set; } // Null on success
    public string RequestId { get; set; } // For tracing

    public static ApiResponse<T> CreateSuccess(T data, string? requestId = null) => new() {
        Success = true, Data = data, Error = null, RequestId = requestId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString()
    };
    public static ApiResponse<object> CreateError(string code, string message, object? details = null, string? requestId = null) => new() {
        Success = false, Data = null, Error = new ApiError { Code = code, Message = message, Details = details }, RequestId = requestId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString()
    };
     // Non-generic factory for success without data
     public static ApiResponse<object> CreateSuccess(string? requestId = null) => new() {
          Success = true, Data = null, Error = null, RequestId = requestId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString()
     };
}

// Error details class
public class ApiError { public string Code { get; set; } public string Message { get; set; } public object? Details { get; set; } }
```

*(Success/Error examples remain as previously defined)*

## 9.5. HTTP Status Codes

Standard HTTP status codes are used:

* **2xx Success:** 200 OK, 201 Created, 202 Accepted, 204 No Content.
* **4xx Client Errors:** 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict, 422 Unprocessable Entity (Validation Errors).
* **5xx Server Errors:** 500 Internal Server Error, 503 Service Unavailable.

Error responses include the `ApiError` structure in the response body.

## 9.6. Pagination, Filtering, and Sorting

Endpoints returning collections support standard query parameters:

* `?page=1&pageSize=20`
* `?sortBy=name&sortOrder=asc`
* `?filterProperty=value` (Specific filters TBD per resource)

Paginated responses include metadata:

```json
{
  "success": true,
  "data": {
    "items": [ /* array of resources */ ],
    "pagination": {
      "page": 1, "pageSize": 20, "totalItems": 53, "totalPages": 3
    }
  }, /* ... */
}
```

## 9.7. Webhooks and Event Streams (Optional / Future)

Mechanisms for pushing real-time updates from SnapDog2:

* **Webhooks:** Server POSTs event data to client-registered URLs.
* **Server-Sent Events (SSE):** Clients maintain connection to `GET /api/v1/events` for a stream of updates.

*(Implementation details deferred)*

## 9.8. HATEOAS Links (Optional)

Responses *may* include a `_links` object with hypermedia controls for related actions.

```json
// Example _links within ZoneState response
"_links": {
  "self": { "href": "/api/v1/zones/1" },
  "play": { "href": "/api/v1/zones/1/commands/play", "method": "POST" },
  // ... other actions
}
```

*(Implementation details deferred)*

## 9.9. API Implementation Notes

* Implemented using ASP.NET Core Minimal APIs or MVC Controllers within the `/Api` folder.
* Controllers/Endpoints act as thin layers, translating HTTP to Cortex.Mediator requests (`_mediator.Send(...)`).
* Use `[ApiController]` attributes (for MVC) for standard behaviors.
* Leverage ASP.NET Core middleware for exception handling (converting to `ApiResponse`), authentication, authorization, rate limiting, security headers.
* Use built-in model binding and validation, potentially enhanced by FluentValidation integration.

## 9.10. API Versioning

* Uses URI path versioning (`/api/v1/`).
* Maintain backward compatibility within `v1.x`. Introduce breaking changes only in new major versions (`v2`).

## 9.11. API Documentation (Swagger/OpenAPI)

* Uses **Swashbuckle.AspNetCore** NuGet package.
* Generate OpenAPI specification automatically from code (controllers, DTOs, XML comments).
* Expose interactive Swagger UI at `/swagger/index.html`.
* Configure Swashbuckle in `/Worker/DI/ApiExtensions.cs` or `Program.cs` to include XML comments, describe security schemes (API Key), etc.
