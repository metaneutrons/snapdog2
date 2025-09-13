import type { PlaylistInfo, TrackInfo } from '../types';
import { config } from './config';

const API_BASE = config.api.baseUrl;

export interface PlaylistsResponse {
  success: boolean;
  data: {
    items: PlaylistInfo[];
    total: number;
    pageSize: number;
    pageNumber: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
  error: null;
}

export interface PlaylistTracksResponse {
  success: boolean;
  data: {
    items: TrackInfo[];
    total: number;
    pageSize: number;
    pageNumber: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
  error: null;
}

export const playlistApi = {
  async getPlaylists(): Promise<PlaylistInfo[]> {
    const response = await fetch(`${API_BASE}/media/playlists`, {
      headers: {
        'X-API-Key': config.api.key
      }
    });
    
    if (!response.ok) {
      throw new Error(`Failed to fetch playlists: ${response.statusText}`);
    }
    
    // API returns direct array, not wrapped response
    const data: PlaylistInfo[] = await response.json();
    return data;
  },

  async getPlaylistTracks(playlistIndex: number): Promise<TrackInfo[]> {
    const response = await fetch(`${API_BASE}/media/playlists/${playlistIndex}/tracks`, {
      headers: {
        'X-API-Key': config.api.key
      }
    });
    
    if (!response.ok) {
      throw new Error(`Failed to fetch playlist tracks: ${response.statusText}`);
    }
    
    const data: PlaylistTracksResponse = await response.json();
    return data.data.items;
  },

  async setZonePlaylist(zoneIndex: number, playlistIndex: number): Promise<void> {
    const response = await fetch(`${API_BASE}/zones/${zoneIndex}/playlist`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': config.api.key
      },
      body: JSON.stringify(playlistIndex)
    });
    
    if (!response.ok) {
      throw new Error(`Failed to set playlist: ${response.statusText}`);
    }
  },

  async playTrackFromPlaylist(zoneIndex: number, playlistIndex: number, trackIndex: number): Promise<void> {
    const response = await fetch(`${API_BASE}/zones/${zoneIndex}/play/playlist/${playlistIndex}/track`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': config.api.key
      },
      body: JSON.stringify(trackIndex)
    });
    
    if (!response.ok) {
      throw new Error(`Failed to play track: ${response.statusText}`);
    }
  }
};
