
import type { ZoneState, ClientState } from '../types';

const BASE_URL = '/api/v1';

class ApiService {
  private async request(method: string, path: string, body?: unknown) {
    const headers: HeadersInit = {};
    let requestBody: BodyInit | undefined;

    if (body !== undefined) {
        headers['Content-Type'] = 'application/json';
        requestBody = JSON.stringify(body);
    }

    const response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: requestBody,
    });

    if (!response.ok) {
        const errorText = await response.text();
        console.error(`API Error: ${method} ${path} -> ${response.status}`, errorText);
        throw new Error(`${method} ${path} -> ${response.status}`);
    }

    if(response.status === 204) return;
    
    const contentType = response.headers.get("content-type");
    if (contentType && contentType.indexOf("application/json") !== -1) {
        return response.json();
    }
    return response.text();
  }

  // Zone controls
  zones = {
    play: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/play`),
    pause: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/pause`),
    stop: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/stop`),
    next: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/next`),
    previous: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/previous`),
    setVolume: (zoneIndex: number, volume: number) => this.request('PUT', `/zones/${zoneIndex}/volume`, volume),
    toggleMute: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/mute/toggle`),
    setMute: (zoneIndex: number, muted: boolean) => this.request('PUT', `/zones/${zoneIndex}/mute`, muted),
    toggleShuffle: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/shuffle/toggle`),
    toggleRepeat: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/repeat/toggle`),
    toggleTrackRepeat: (zoneIndex: number) => this.request('POST', `/zones/${zoneIndex}/repeat/track/toggle`),
    setPlaylist: (zoneIndex: number, playlistIndex: number) => this.request('PUT', `/zones/${zoneIndex}/playlist`, playlistIndex),
    setTrack: (zoneIndex: number, trackIndex: number) => this.request('PUT', `/zones/${zoneIndex}/track`, trackIndex),
    seekPosition: (zoneIndex: number, positionMs: number) => this.request('PUT', `/zones/${zoneIndex}/track/position`, positionMs),
  };

  // Client controls
  clients = {
    assignZone: (clientIndex: number, zoneIndex: number | null) => this.request('PUT', `/clients/${clientIndex}/zone`, zoneIndex),
    setVolume: (clientIndex: number, volume: number) => this.request('PUT', `/clients/${clientIndex}/volume`, volume),
    toggleMute: (clientIndex: number) => this.request('POST', `/clients/${clientIndex}/mute/toggle`),
    volumeUp: (clientIndex: number, step = 5) => this.request('POST', `/clients/${clientIndex}/volume/up?step=${step}`),
    volumeDown: (clientIndex: number, step = 5) => this.request('POST', `/clients/${clientIndex}/volume/down?step=${step}`),
  };

  // Scalar reads
  get = {
    zoneCount: (): Promise<number> => this.request('GET', '/zones/count'),
    clientCount: (): Promise<number> => this.request('GET', '/clients/count'),
    zone: (zoneIndex: number): Promise<ZoneState> => this.request('GET', `/zones/${zoneIndex}`),
    client: (clientIndex: number): Promise<ClientState> => this.request('GET', `/clients/${clientIndex}`),
    zoneMetadata: (zoneIndex: number) => this.request('GET', `/zones/${zoneIndex}/track/metadata`),
    zonePlaylist: (zoneIndex: number) => this.request('GET', `/zones/${zoneIndex}/playlist/info`),
    zoneName: (zoneIndex: number): Promise<string> => this.request('GET', `/zones/${zoneIndex}/name`),
    clientName: (clientIndex: number): Promise<string> => this.request('GET', `/clients/${clientIndex}/name`),
  };
}

export const api = new ApiService();