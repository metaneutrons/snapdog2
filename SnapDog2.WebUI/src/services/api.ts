import type { ZoneState, ClientState } from '../types';

const BASE_URL = '/api/v1';

interface PaginatedResponse<T> {
  items: T[];
  total: number;
}

class ApiService {
  private async request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const headers: HeadersInit = {
      'X-API-Key': 'dev-key'
    };
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
      throw new Error(`API Error ${response.status}: ${errorText}`);
    }

    return response.json();
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

  post = {
    moveClientToZone: async (clientIndex: number, zoneIndex: number): Promise<void> => {
      await this.request('PUT', `/clients/${clientIndex}/zone`, zoneIndex);
    },

    setZoneVolume: async (zoneIndex: number, volume: number): Promise<void> => {
      await this.request('PUT', `/zones/${zoneIndex}/volume`, { volume });
    },

    toggleZoneMute: async (zoneIndex: number): Promise<void> => {
      await this.request('POST', `/zones/${zoneIndex}/mute/toggle`);
    }
  };
}

export const api = new ApiService();
