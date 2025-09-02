import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import type { TrackInfo, ZoneState, ClientState, PlaybackState, PlaylistInfo } from '../types';

interface AppState {
  zones: Record<number, ZoneState>;
  clients: Record<number, ClientState>;

  // Zone actions
  updateZoneProgress: (zoneIndex: number, progress: { position: number; progress: number }) => void;
  updateZoneTrack: (zoneIndex: number, track: TrackInfo) => void;
  updateZoneVolume: (zoneIndex: number, volume: number) => void;
  updateZonePlayback: (zoneIndex: number, playbackState: PlaybackState) => void;
  updateZoneMute: (zoneIndex: number, muted: boolean) => void;
  updateZoneRepeat: (zoneIndex: number, trackRepeat: boolean, playlistRepeat: boolean) => void;
  updateZoneShuffle: (zoneIndex: number, shuffle: boolean) => void;
  updateZonePlaylist: (zoneIndex: number, playlist: PlaylistInfo | null) => void;

  // Client actions
  updateClientConnection: (clientIndex: number, connected: boolean) => void;
  updateClientZone: (clientIndex: number, zoneIndex?: number) => void;
  updateClientVolume: (clientIndex: number, volume: number) => void;
  updateClientMute: (clientIndex: number, muted: boolean) => void;
  updateClientLatency: (clientIndex: number, latency: number) => void;

  // Utility actions
  setInitialZoneState: (zoneIndex: number, zoneState: any) => void;
  setInitialClientState: (clientIndex: number, clientState: any) => void;
  initializeZone: (zoneIndex: number) => void;
  initializeClient: (clientIndex: number) => void;
}

const defaultZoneState: ZoneState = {
  name: 'Unknown Zone',
  volume: 0,
  muted: false,
  playlistShuffle: false,
  trackRepeat: false,
  playlistRepeat: false,
  playbackState: 'stopped',
  clients: [],
};

const defaultClientState: ClientState = {
  name: 'Unknown Client',
  connected: false,
  volume: 0,
  muted: false,
};

export const useAppStore = create<AppState>()(
  devtools(
    (set) => ({
      zones: {},
      clients: {},

      // Zone actions
      updateZoneProgress: (zoneIndex, progress) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              progress
            },
          },
        })),

      updateZoneTrack: (zoneIndex, track) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              track: track
            },
          },
        })),

      updateZoneVolume: (zoneIndex, volume) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              volume
            },
          },
        })),

      updateZonePlayback: (zoneIndex, playbackState) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playbackState
            },
          },
        })),

      updateZoneMute: (zoneIndex, muted) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              muted
            },
          },
        })),

      updateZoneRepeat: (zoneIndex, trackRepeat, playlistRepeat) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              trackRepeat,
              playlistRepeat
            },
          },
        })),

      updateZoneShuffle: (zoneIndex, shuffle) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playlistShuffle: shuffle
            },
          },
        })),

      updateZonePlaylist: (zoneIndex, playlist) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playlist: playlist ?? undefined
            },
          },
        })),

      // Client actions
      updateClientConnection: (clientIndex, connected) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex] || defaultClientState,
              connected
            },
          },
        })),

      updateClientZone: (clientIndex, zoneIndex) => {
        set((state) => {
          const updatedZones = { ...state.zones };
          Object.keys(updatedZones).forEach(zIdStr => {
              const zId = parseInt(zIdStr);
              updatedZones[zId].clients = updatedZones[zId].clients.filter(c => c !== clientIndex);
          });

          if (zoneIndex !== undefined && zoneIndex !== null) {
              if (!updatedZones[zoneIndex]) {
                  updatedZones[zoneIndex] = { ...defaultZoneState };
              }
              updatedZones[zoneIndex].clients = [...updatedZones[zoneIndex].clients, clientIndex];
          }

          return {
            zones: updatedZones,
            clients: {
              ...state.clients,
              [clientIndex]: {
                ...state.clients[clientIndex] || defaultClientState,
                zoneIndex
              },
            },
          };
        });
      },

      updateClientVolume: (clientIndex, volume) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex] || defaultClientState,
              volume
            },
          },
        })),

      updateClientMute: (clientIndex, muted) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex] || defaultClientState,
              muted
            },
          },
        })),

      updateClientLatency: (clientIndex, latency) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex] || defaultClientState,
              latency
            },
          },
        })),
        
      // Utility actions
      setInitialZoneState: (zoneIndex, zoneData) => 
        set(state => {
          const playbackStateMap: Record<number, PlaybackState> = {
            0: 'stopped',
            1: 'playing',
            2: 'paused'
          };
          const initialPlaybackState = playbackStateMap[zoneData.playbackState as number] || 'stopped';

          return {
            zones: {
              ...state.zones,
              [zoneIndex]: {
                ...defaultZoneState,
                ...state.zones[zoneIndex],
                name: zoneData.name,
                volume: zoneData.volume,
                muted: zoneData.mute,
                playlistShuffle: zoneData.playlistShuffle,
                trackRepeat: zoneData.trackRepeat,
                playlistRepeat: zoneData.playlistRepeat,
                playbackState: initialPlaybackState,
                track: zoneData.track,
                playlist: zoneData.playlist,
                clients: zoneData.clients || [],
              }
            }
          };
        }),

      setInitialClientState: (clientIndex, clientData) =>
        set(state => ({
          clients: {
            ...state.clients,
            [clientIndex]: {
              ...defaultClientState,
              ...state.clients[clientIndex],
              name: clientData.name,
              connected: clientData.connected,
              zoneIndex: clientData.zoneIndex,
              volume: clientData.volume,
              muted: clientData.mute,
              latency: clientData.latencyMs,
            }
          }
        })),

      initializeZone: (zoneIndex) =>
        set((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: state.zones[zoneIndex] || { ...defaultZoneState, name: `Zone ${zoneIndex}` },
          },
        })),

      initializeClient: (clientIndex) =>
        set((state) => ({
          clients: {
            ...state.clients,
            [clientIndex]: state.clients[clientIndex] || { ...defaultClientState, name: `Client ${clientIndex}` },
          },
        })),
    }),
    { name: 'snapdog-store' }
  )
);

export const useZone = (zoneIndex: number) => useAppStore((state) => state.zones[zoneIndex]);
export const useClient = (clientIndex: number) => useAppStore((state) => state.clients[clientIndex]);
export const useUnassignedClients = () => useAppStore((state) => 
    Object.entries(state.clients)
        .filter(([, client]: [string, ClientState]) => client.zoneIndex === undefined || client.zoneIndex === null)
        .map(([index]) => parseInt(index))
);