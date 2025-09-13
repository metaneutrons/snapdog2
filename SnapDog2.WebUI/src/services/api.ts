import type { ZoneState, ClientState } from '../types';
import { config } from './config';

interface PaginatedResponse<T> {
  items: T[];
  total: number;
}

class ApiService {
  private async request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const headers: HeadersInit = {
      'X-API-Key': config.api.key
    };
    let requestBody: BodyInit | undefined;

    if (body !== undefined) {
      headers['Content-Type'] = 'application/json';
      requestBody = JSON.stringify(body);
    }

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), config.api.timeout);

    try {
      const response = await fetch(`${config.api.baseUrl}${path}`, {
        method,
        headers,
        body: requestBody,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`API Error ${response.status}: ${errorText}`);
      }

      return response.json();
    } catch (error) {
      clearTimeout(timeoutId);
      throw error;
    }
  }

  get = {
    zones: async (): Promise<ZoneState[]> => {
      const response = await this.request<PaginatedResponse<ZoneState>>('GET', '/zones');
      return response.items;
    },

    clients: async (): Promise<ClientState[]> => {
      const response = await this.request<PaginatedResponse<ClientState>>('GET', '/clients');
      return response.items;
    },

    zone: async (zoneIndex: number): Promise<ZoneState> => {
      return this.request<ZoneState>('GET', `/zones/${zoneIndex}`);
    },

    client: async (clientIndex: number): Promise<ClientState> => {
      return this.request<ClientState>('GET', `/clients/${clientIndex}`);
    },

    zoneCount: async (): Promise<number> => {
      const zones = await this.get.zones();
      return zones.length;
    },

    clientCount: async (): Promise<number> => {
      const clients = await this.get.clients();
      return clients.length;
    }
  };

  zones = {
    play: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/play`);
    },

    pause: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/pause`);
    },

    stop: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/stop`);
    },

    next: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/next`);
    },

    previous: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/previous`);
    },

    setVolume: async (zoneIndex: number, volume: number): Promise<void> => {
      await this.request('PUT', `/zones/${zoneIndex}/volume`, volume);
    },

    toggleMute: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/mute/toggle`);
    },

    setPlaylist: async (zoneIndex: number, playlistIndex: number): Promise<void> => {
      await this.request('PUT', `/zones/${zoneIndex}/playlist`, playlistIndex);
    },

    toggleShuffle: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/shuffle/toggle`);
    },

    toggleRepeat: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/repeat/toggle`);
    }
  };

  clients = {
    moveToZone: async (clientIndex: number, zoneIndex: number): Promise<void> => {
      await this.request('PUT', `/clients/${clientIndex}/zone`, zoneIndex);
    },

    setVolume: async (clientIndex: number, volume: number): Promise<void> => {
      await this.request('PUT', `/clients/${clientIndex}/volume`, volume);
    },

    toggleMute: async (clientIndex: number): Promise<void> => {
      await this.request('POST', `/clients/${clientIndex}/mute/toggle`);
    }
  };

  // Legacy support - will be removed
  post = {
    moveClientToZone: async (clientIndex: number, zoneIndex: number): Promise<void> => {
      await this.clients.moveToZone(clientIndex, zoneIndex);
    },

    setZoneVolume: async (zoneIndex: number, volume: number): Promise<void> => {
      await this.zones.setVolume(zoneIndex, volume);
    },

    toggleZoneMute: async (zoneIndex: number): Promise<void> => {
      await this.zones.toggleMute(zoneIndex);
    },

    setClientVolume: async (clientIndex: number, volume: number): Promise<void> => {
      await this.clients.setVolume(clientIndex, volume);
    },

    toggleClientMute: async (clientIndex: number): Promise<void> => {
      await this.clients.toggleMute(clientIndex);
    }
  };
}

export const api = new ApiService();
