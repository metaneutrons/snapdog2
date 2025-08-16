# 10. API Specification

## 10.1. API Design Philosophy

The SnapDog2 Application Programming Interface (API) is designed as a modern, **RESTful HTTP interface** providing comprehensive programmatic control over the audio management system. It serves as a primary integration point for web UIs, mobile applications, third-party services, and custom scripts operating within the local network.

The API structure and functionality directly map to the logical concepts defined in the **Command Framework (Section 9)**, ensuring consistency between different control methods (API, MQTT, KNX). It exposes resources representing the system's global state, individual zones, and connected clients.

Key design principles underpinning the API are:

1. **Command Framework Alignment**: API endpoints and their operations correspond directly to the defined Global, Zone, and Client commands and status updates. Retrieving a zone's state via the API (`GET /api/v1/zones/{zoneIndex}`) reflects the same information available via the `ZONE_STATE` status. Sending a command (`POST /api/v1/zones/{zoneIndex}/play`) triggers the equivalent internal `PLAY` command logic.
2. **Resource-Oriented Design**: Follows standard REST conventions. Nouns identify resources (e.g., `/zones`, `/clients`, `/media/playlists`), and standard HTTP verbs dictate actions:
    * `GET`: Retrieve resource state or collections (safe, idempotent).
    * `PUT`: Update resource state or settings entirely (idempotent where applicable, e.g., setting volume, mute state, specific track/playlist).
    * `POST`: Trigger actions/commands that may not be idempotent (e.g., `play`, `pause`, `next_track`) or create new resources (though zone creation is handled via configuration in SnapDog2).
    * `DELETE`: Remove resources (not applicable to zones/clients in SnapDog2 due to static configuration).
3. **Modern Direct Response Design**: All API responses return data directly without wrapper objects, using HTTP status codes to indicate success or failure. This provides a cleaner, more intuitive API experience compared to traditional wrapper patterns. Error responses use Problem Details (RFC 7807) for standardized error information.
4. **Statelessness**: The API is stateless. Each request from a client must contain all the information needed to understand and process the request. The server does not maintain client session state between requests. Authentication is handled per-request via API keys.
5. **Clear Versioning**: Uses URI path versioning (`/api/v1/`) to manage changes and ensure backward compatibility where possible.

## 10.2. Authentication and Authorization

Given SnapDog2's typical deployment within a trusted local network, the primary security mechanism focuses on preventing unauthorized *control* rather than complex user management or data protection.

* **API Key Authentication**: This is the **mandatory and sole implemented authentication method**. Clients **must** include a valid, pre-configured API key in the `X-API-Key` HTTP request header for all endpoints unless an endpoint is explicitly marked with `[AllowAnonymous]` (e.g., potentially `/health` endpoints). API keys are defined via `SNAPDOG_API_APIKEY_{n}` environment variables (See Section 10). The implementation details are found in Section 8 (`ApiKeyAuthenticationHandler`). Failure to provide a valid key results in a `401 Unauthorized` response.
* **Authorization**: Currently basic. Successful authentication grants access to all authorized endpoints. Finer-grained authorization (e.g., specific keys only allowed to control certain zones) is **not implemented** in the current scope but could be a future enhancement. A failed authorization check (if implemented later) would result in a `403 Forbidden` response.
* *(Future Considerations)*: While Bearer Tokens (JWT) or OAuth2 could be added later for more complex scenarios or third-party integrations, they are outside the current MVP scope.

## 10.3. Server Configuration

The API server configuration is controlled via environment variables:

* **`SNAPDOG_API_ENABLED`**: Enable/disable the API server entirely (default: true)
* **`SNAPDOG_API_PORT`**: Port number for the HTTP API server (default: 5000)
* **`SNAPDOG_API_AUTH_ENABLED`**: Enable/disable API key authentication (default: true)
* **`SNAPDOG_API_APIKEY_{n}`**: API keys for authentication (where n is 1, 2, 3, etc.)

Example configuration:

```bash
SNAPDOG_API_ENABLED=true
SNAPDOG_API_PORT=5000
SNAPDOG_API_AUTH_ENABLED=true
SNAPDOG_API_APIKEY_1=your-secret-api-key-here
```

When `SNAPDOG_API_ENABLED=false`, the API server is completely disabled - no HTTP endpoints are exposed, no controllers are registered, and no port is opened. This is useful for deployments where only MQTT or KNX control is desired.

The server listens on all network interfaces (`0.0.0.0`) on the configured port, making it accessible from any device on the local network.

## 10.4. API Structure (`/api/v1/`)

Base path: `/api/v1`. All endpoints below assume this prefix.

### 10.4.1. Global Endpoints

Endpoints for accessing system-wide information.

| Method | Path                   | Functionality ID  | Description                      | Success Response (`Data` field) |
| :----- | :--------------------- | :---------------- | :------------------------------- | :------------------------------ |
| `GET`  | `/system/status`       | `SYSTEM_STATUS`   | Get system online status         | `SystemStatusDto` { bool IsOnline, DateTime TimestampUtc } |
| `GET`  | `/system/errors`       | `ERROR_STATUS`    | Get recent system errors (TBD)   | `List<ErrorDetails>`            |
| `GET`  | `/system/version`      | `VERSION_INFO`    | Get software version             | `VersionDetails`                |
| `GET`  | `/system/stats`        | `SERVER_STATS`    | Get server performance statistics| `ServerStats`                   |

*(Note: DTO structures match Core Models where appropriate)*

### 10.4.2. Zone Endpoints

Endpoints for interacting with configured audio zones. **Zone creation/deletion/rename is not supported via API** as zones are defined via environment variables. **Indices are 1-based.**

**Modern Design Philosophy:** Returns primitive values directly (int, bool, string) instead of wrapper objects for maximum simplicity and better developer experience.

| Method | Path                                       | Command/Status ID         | Description                          | Request Body / Params                           | Success Response (Direct Primitive)  | HTTP Status |
| :----- | :----------------------------------------- | :------------------------ | :----------------------------------- | :---------------------------------------------- | :----------------------------------- | :---------- |
| `GET`  | `/zones`                                   | -                         | List configured zones                | Query: `?page=1&size=20`                       | `Page<Zone>`                         | 200 OK      |
| `GET`  | `/zones/{zoneIndex}`                       | `ZONE_STATE` (Full)       | Get details & full state for zone    | Path: `{zoneIndex}`                        | `ZoneState`                          | 200 OK      |
| `POST` | `/zones/{zoneIndex}/play`                  | `PLAY`                    | Start/resume playback                | Path: `{zoneIndex}`                             | No content                           | 204 No Content|
| `POST` | `/zones/{zoneIndex}/play/track/{trackIndex}` | `PLAY`                  | Play specific track by index         | Path: `{zoneIndex}`, `{trackIndex}` (1-based)  | No content                           | 204 No Content|
| `POST` | `/zones/{zoneIndex}/play/url`              | `PLAY`                    | Play direct URL stream               | Path: `{zoneIndex}`; Body: `string` (URL)      | No content                           | 204 No Content|
| `POST` | `/zones/{zoneIndex}/pause`                 | `PAUSE`                   | Pause playback                       | Path: `{zoneIndex}`                             | No content                           | 204 No Content|
| `POST` | `/zones/{zoneIndex}/stop`                  | `STOP`                    | Stop playback                        | Path: `{zoneIndex}`                             | No content                           | 204 No Content|
| `POST` | `/zones/{zoneIndex}/next`                  | `TRACK_NEXT`              | Play next track                      | Path: `{zoneIndex}`                             | No content                           | 204 No Content|
| `POST` | `/zones/{zoneIndex}/previous`              | `TRACK_PREVIOUS`          | Play previous track                  | Path: `{zoneIndex}`                             | No content                           | 204 No Content|
| `GET`  | `/zones/{zoneIndex}/track`                 | `TRACK_INDEX`             | Get current track by **1-based index** | Path: `{zoneIndex}`                          | `int` (track index)                  | 200 OK      |
| `PUT`  | `/zones/{zoneIndex}/track`                 | `TRACK`                   | Set track by **1-based** index       | Path: `{zoneIndex}`; Body: `int` (track index) | No content                           | 204 No Content|
| `GET`  | `/zones/{zoneIndex}/track/metadata`        | `TRACK_METADATA`          | Get current track metadata           | Path: `{zoneIndex}`                             | `TrackInfo`                          | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/track/title`           | `TRACK_METADATA_TITLE`        | Get current track title              | Path: `{zoneIndex}`                             | `string` (track title)               | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/track/artist`          | `TRACK_METADATA_ARTIST`       | Get current track artist             | Path: `{zoneIndex}`                             | `string` (track artist)              | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/track/album`           | `TRACK_METADATA_ALBUM`        | Get current track album              | Path: `{zoneIndex}`                             | `string` (track album)               | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/track/duration`        | `TRACK_METADATA_DURATION`       | Get current track duration           | Path: `{zoneIndex}`                             | `long` (duration in ms)              | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/track/cover`           | `TRACK_METADATA_COVER`        | Get current track cover art URL      | Path: `{zoneIndex}`                             | `string` (cover URL)                 | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/track/position`        | `TRACK_POSITION_STATUS`   | Get current track position           | Path: `{zoneIndex}`                             | `long` (position in ms)              | 200 OK      |
| `PUT`  | `/zones/{zoneIndex}/track/position`        | `TRACK_POSITION`          | Seek to position in track            | Path: `{zoneIndex}`; Body: `long` (position ms) | No content                          | 204 No Content|
| `GET`  | `/zones/{zoneIndex}/track/playing`         | `TRACK_PLAYING_STATUS`    | Get current playing state            | Path: `{zoneIndex}`                             | `bool` (is playing)                  | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/track/progress`        | `TRACK_PROGRESS_STATUS`   | Get current track progress           | Path: `{zoneIndex}`                             | `float` (progress 0.0-1.0)           | 200 OK      |
| `PUT`  | `/zones/{zoneIndex}/track/progress`        | `TRACK_PROGRESS`          | Seek to progress percentage          | Path: `{zoneIndex}`; Body: `float` (0.0-1.0)   | No content                           | 204 No Content|
| `PUT`  | `/zones/{zoneIndex}/playlist`              | `PLAYLIST`                | Set playlist by **1-based** index    | Path: `{zoneIndex}`; Body: `int` (playlist index) | No content                        | 204 No Content|
| `GET`  | `/zones/{zoneIndex}/playlist`              | `PLAYLIST_INDEX`          | Get current playlist index           | Path: `{zoneIndex}`                             | `int` (playlist index)               | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/playlist/info`         | `PLAYLIST_INFO`           | Get current playlist info            | Path: `{zoneIndex}`                             | `PlaylistInfo`                       | 200 OK      |
| `POST` | `/zones/{zoneIndex}/playlist/next`         | `PLAYLIST_NEXT`           | Play next playlist                   | Path: `{zoneIndex}`                             | No content                           | 204 No Content|
| `POST` | `/zones/{zoneIndex}/playlist/previous`     | `PLAYLIST_PREVIOUS`       | Play previous playlist               | Path: `{zoneIndex}`                             | No content                           | 204 No Content|
| `PUT`  | `/zones/{zoneIndex}/volume`                | `VOLUME`                  | Set zone volume                      | Path: `{zoneIndex}`; Body: `int` (0-100)       | `int` (volume level)                 | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/volume`                | `VOLUME_STATUS`           | Get current zone volume.             | Path: `{zoneIndex}`                             | `int` (volume level)                 | 200 OK      |
| `POST` | `/zones/{zoneIndex}/volume/up`             | `VOLUME_UP`               | Increase volume                      | Path: `{zoneIndex}`; Body: `int` (step, default 5) | `int` (new volume)               | 200 OK      |
| `POST` | `/zones/{zoneIndex}/volume/down`           | `VOLUME_DOWN`             | Decrease volume                      | Path: `{zoneIndex}`; Body: `int` (step, default 5) | `int` (new volume)               | 200 OK      |
| `PUT`  | `/zones/{zoneIndex}/mute`                  | `MUTE`                    | Set mute state                       | Path: `{zoneIndex}`; Body: `bool` (muted)       | `bool` (mute state)                  | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/mute`                  | `MUTE_STATUS`             | Get current mute state               | Path: `{zoneIndex}`                             | `bool` (mute state)                  | 200 OK      |
| `POST` | `/zones/{zoneIndex}/mute/toggle`           | `MUTE_TOGGLE`             | Toggle mute state                    | Path: `{zoneIndex}`                             | `bool` (new mute state)              | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/repeat/track`          | `TRACK_REPEAT_STATUS`     | Get track repeat mode                | Path: `{zoneIndex}`                             | `bool` (repeat enabled)              | 200 OK      |
| `PUT`  | `/zones/{zoneIndex}/repeat/track`          | `TRACK_REPEAT`            | Set track repeat mode                | Path: `{zoneIndex}`; Body: `bool` (enabled)     | `bool` (repeat enabled)              | 200 OK      |
| `POST` | `/zones/{zoneIndex}/repeat/track/toggle`   | `TRACK_REPEAT_TOGGLE`     | Toggle track repeat mode             | Path: `{zoneIndex}`                             | `bool` (new repeat state)            | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/repeat`       | `PLAYLIST_REPEAT_STATUS`  | Get playlist repeat mode             | Path: `{zoneIndex}`                             | `bool` (repeat enabled)              | 200 OK      |
| `PUT`  | `/zones/{zoneIndex}/repeat`       | `PLAYLIST_REPEAT`         | Set playlist repeat mode             | Path: `{zoneIndex}`; Body: `bool` (enabled)     | `bool` (repeat enabled)              | 200 OK      |
| `POST` | `/zones/{zoneIndex}/repeat/toggle`| `PLAYLIST_REPEAT_TOGGLE`  | Toggle playlist repeat mode          | Path: `{zoneIndex}`                             | `bool` (new repeat state)            | 200 OK      |
| `GET`  | `/zones/{zoneIndex}/shuffle`      | `PLAYLIST_SHUFFLE_STATUS` | Get playlist shuffle mode            | Path: `{zoneIndex}`                             | `bool` (shuffle enabled)             | 200 OK      |
| `PUT`  | `/zones/{zoneIndex}/shuffle`      | `PLAYLIST_SHUFFLE`        | Set playlist shuffle mode            | Path: `{zoneIndex}`; Body: `bool` (enabled)     | `bool` (shuffle enabled)             | 200 OK      |
| `POST` | `/zones/{zoneIndex}/shuffle/toggle`| `PLAYLIST_SHUFFLE_TOGGLE`| Toggle playlist shuffle mode         | Path: `{zoneIndex}`                             | `bool` (new shuffle state)           | 200 OK      |

**Modern API Benefits:**

* **Primitive Returns:** Volume endpoints return `int` directly (e.g., `75`) instead of `{"volume": 75}`
* **Boolean States:** Mute/repeat/shuffle return `bool` directly (e.g., `true`) instead of `{"enabled": true}`
* **Direct Parameters:** Use `int volume` instead of `{"level": 75}` request objects
* **Cleaner Client Code:** `const volume = await api.getVolume(1)` instead of `const volume = await api.getVolume(1).then(r => r.volume)`

*Note on HTTP Status Codes: State retrievals and settings return 200 OK with the primitive value. Actions (play, pause, stop, track navigation, playlist setting) return 204 No Content to indicate successful completion without response data.*

**Modern Response Design (Direct Primitives):**

```csharp
namespace SnapDog2.Api.Models;

// ═══════════════════════════════════════════════════════════════════════════════
// PRIMITIVE RESPONSES - Return values directly for maximum simplicity
// ═══════════════════════════════════════════════════════════════════════════════
//
// 🎯 PHILOSOPHY: Return the actual value, not a wrapper object
//
// ✅ GET /zones/1/volume        → 75 (int)
// ✅ GET /zones/1/mute          → false (bool)
// ✅ GET /zones/1/track         → 3 (int)
// ✅ GET /zones/1/repeat/track  → true (bool)
//
// This eliminates ALL single-property response wrappers!

// ═══════════════════════════════════════════════════════════════════════════════
// COLLECTION RESPONSES - Only when structure adds value
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Paginated collection with metadata.
/// </summary>
public record Page<T>(T[] Items, int Total, int PageSize = 20, int PageNumber = 1)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => PageNumber < TotalPages;
    public bool HasPrevious => PageNumber > 1;
}

/// <summary>
/// Zone summary for listings.
/// </summary>
public record Zone(string Name, int Index, bool Active, string Status);

/// <summary>
/// Client summary for listings.
/// </summary>
public record Client(int Id, string Name, bool Connected, int? Zone = null);

```

### 10.4.3. Client Endpoints

Endpoints for interacting with discovered Snapcast clients.

**Modern Design Philosophy:** Matches zones endpoints with direct primitive responses and zero request objects for maximum consistency and simplicity.

| Method | Path                                    | Command/Status ID      | Description                | Request Body / Params           | Success Response (Direct Primitive) | HTTP Status |
| :----- | :-------------------------------------- | :--------------------- | :------------------------- | :------------------------------ | :----------------------------------- | :---------- |
| `GET`  | `/clients`                              | -                      | List discovered clients    | Query: `?page=1&size=20`        | `Page<Client>`                       | 200 OK      |
| `GET`  | `/clients/{clientIndex}`                   | `CLIENT_STATE` (Full)  | Get details for a client   | Path: `{clientIndex}` (int)         | `ClientState`                        | 200 OK      |
| `PUT`  | `/clients/{clientIndex}/volume`            | `CLIENT_VOLUME`        | Set client volume          | Path: `{clientIndex}`; Body: `int` (0-100) | `int` (volume level)              | 200 OK      |
| `GET`  | `/clients/{clientIndex}/volume`            | `CLIENT_VOLUME_STATUS` | Get client volume          | Path: `{clientIndex}`               | `int` (volume level)                 | 200 OK      |
| `POST` | `/clients/{clientIndex}/volume/up`         | `CLIENT_VOLUME_UP`     | Increase client volume     | Path: `{clientIndex}`; Optional Query: `?step=5` | `int` (new volume)            | 200 OK      |
| `POST` | `/clients/{clientIndex}/volume/down`       | `CLIENT_VOLUME_DOWN`   | Decrease client volume     | Path: `{clientIndex}`; Optional Query: `?step=5` | `int` (new volume)            | 200 OK      |
| `PUT`  | `/clients/{clientIndex}/mute`              | `CLIENT_MUTE`          | Set client mute state      | Path: `{clientIndex}`; Body: `bool` | `bool` (mute state)                  | 200 OK      |
| `GET`  | `/clients/{clientIndex}/mute`              | `CLIENT_MUTE_STATUS`   | Get client mute state      | Path: `{clientIndex}`               | `bool` (mute state)                  | 200 OK      |
| `POST` | `/clients/{clientIndex}/mute/toggle`       | `CLIENT_MUTE_TOGGLE`   | Toggle client mute state   | Path: `{clientIndex}`               | `bool` (new mute state)              | 200 OK      |
| `PUT`  | `/clients/{clientIndex}/latency`           | `CLIENT_LATENCY`       | Set client latency         | Path: `{clientIndex}`; Body: `int` (ms) | `int` (latency)                  | 200 OK      |
| `GET`  | `/clients/{clientIndex}/latency`           | `CLIENT_LATENCY_STATUS`| Get client latency         | Path: `{clientIndex}`               | `int` (latency)                      | 200 OK      |
| `PUT`  | `/clients/{clientIndex}/zone`              | `CLIENT_ZONE`          | Assign client to zone      | Path: `{clientIndex}`; Body: `int` (zoneIndex, 1-based) | No content                     | 204 No Content |
| `GET`  | `/clients/{clientIndex}/zone`              | `CLIENT_ZONE_STATUS`   | Get client assigned zone   | Path: `{clientIndex}`               | `int?` (zoneIndex)                      | 200 OK      |
| `PUT`  | `/clients/{clientIndex}/name`              | `CLIENT_NAME`          | Rename client in Snapcast  | Path: `{clientIndex}`; Body: `string` (name) | `string` (name)                | 200 OK      |

**Modern API Benefits:**

* **Primitive Returns:** Volume endpoints return `int` directly (e.g., `65`) instead of `{"volume": 65}`
* **Boolean States:** Mute returns `bool` directly (e.g., `true`) instead of `{"muted": true}`
* **Direct Parameters:** Use `int volume` instead of `{"level": 65}` request objects
* **Consistent Design:** Matches zones endpoints exactly for perfect API consistency
* **Zero Request Objects:** All operations use direct parameter binding

### 10.4.4. Media Management Endpoints

Endpoints for browsing available media sources (initially Subsonic and Radio).

| Method | Path                                    | Description                    | Success Response (`Data` field)     |
| :----- | :-------------------------------------- | :----------------------------- | :-------------------------------- |
| `GET`  | `/media/sources`                        | List configured media sources  | `List<MediaSourceInfo>` { string Id, string Type, string Name } |
| `GET`  | `/media/playlists`                      | List all available playlists   | Paginated `List<PlaylistInfo>`    |
| `GET`  | `/media/playlists/{playlistIndex}`  | Get details for a playlist     | `PlaylistWithTracks` { PlaylistInfo Info, List<TrackInfo> Tracks } |
| `GET`  | `/media/playlists/{playlistIndex}/tracks` | List tracks in a playlist    | Paginated `List<TrackInfo>`       |
| `GET`  | `/media/tracks/{trackIndex}`               | Get details for a track        | `TrackInfo`                       |

*(Note: `playlistIndex` accepts `1` for the Radio playlist, `2+` for others.*

## 10.5. Request and Response Format

### 10.5.1. Modern Direct Response Design

The SnapDog2 API uses a **modern direct response approach** where endpoints return data directly without wrapper objects. This provides a cleaner, more intuitive API experience:

**Success Responses:**

* **Data Endpoints**: Return the requested data directly (e.g., `VolumeResponse`, `ZoneState`, `TrackInfo`)
* **Action Endpoints**: Return `204 No Content` for successful actions without response data
* **Setting Endpoints**: Return the updated state directly (e.g., `VolumeResponse` after setting volume)

**Error Responses:**

* Use **Problem Details (RFC 7807)** for standardized error information
* Return appropriate HTTP status codes (400, 404, 500, etc.)
* Include structured error details in the response body

**Example Success Response (GET /zones/1/volume):**

```json
{
  "volume": 75
}
```

**Example Error Response (404 Not Found):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Zone not found",
  "status": 404,
  "detail": "Zone 1 does not exist"
}
```

### 10.5.2. Legacy Response Wrapper (Deprecated)

*Note: The `ApiResponse<T>` wrapper pattern is deprecated in favor of direct responses. This section is maintained for reference only.*

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

## 10.6. HTTP Status Codes

Standard HTTP status codes are used:

* **2xx Success:** 200 OK, 201 Created, 202 Accepted, 204 No Content.
* **4xx Client Errors:** 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict, 422 Unprocessable Entity (Validation Errors).
* **5xx Server Errors:** 500 Internal Server Error, 503 Service Unavailable.

Error responses include the `ApiError` structure in the response body.

## 10.7. Pagination, Filtering, and Sorting

Endpoints returning collections support standard query parameters:

* `?page=1&size=20`
* `?sortBy=name&sortOrder=asc`
* `?filterProperty=value` (Specific filters TBD per resource)

Paginated responses include metadata:

```json
{
  "success": true,
  "data": {
    "items": [ /* array of resources */ ],
    "pagination": {
      "page": 1, "size": 20, "totalItems": 53, "totalPages": 3
    }
  }, /* ... */
}
```

## 10.8. Webhooks and Event Streams (Optional / Future)

Mechanisms for pushing real-time updates from SnapDog2:

* **Webhooks:** Server POSTs event data to client-registered URLs.
* **Server-Sent Events (SSE):** Clients maintain connection to `GET /api/v1/events` for a stream of updates.

*(Implementation details deferred)*

## 10.9. HATEOAS Links (Optional)

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

## 10.10. API Implementation Notes

* Implemented using ASP.NET Core Minimal APIs or MVC Controllers within the `/Api` folder.
* Controllers/Endpoints act as thin layers, translating HTTP to Cortex.Mediator requests (`_mediator.Send(...)`).
* Use `[ApiController]` attributes (for MVC) for standard behaviors.
* Leverage ASP.NET Core middleware for exception handling (converting to `ApiResponse`), authentication, authorization, rate limiting, security headers.
* Use built-in model binding and validation, potentially enhanced by FluentValidation integration.

## 10.11. API Versioning

* Uses URI path versioning (`/api/v1/`).
* Maintain backward compatibility within `v1.x`. Introduce breaking changes only in new major versions (`v2`).

## 10.12. API Documentation (Swagger/OpenAPI)

* Uses **Swashbuckle.AspNetCore** NuGet package.
* Generate OpenAPI specification automatically from code (controllers, DTOs, XML comments).
* Expose interactive Swagger UI at `/swagger/index.html`.
* Configure Swashbuckle in `/Worker/DI/ApiExtensions.cs` or `Program.cs` to include XML comments, describe security schemes (API Key), etc.
