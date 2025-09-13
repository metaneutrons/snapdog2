import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import { api } from '../services/api';
import { playlistApi } from '../services/playlistApi';
import type { TrackInfo, ZoneState, ClientState, PlaybackState, PlaylistInfo } from '../types';

interface AppState {
  zones: Record<number, ZoneState>;
  clients: Record<number, ClientState>;
  playlists: PlaylistInfo[];
  loadingStates: Record<number, { changingPlaylist?: boolean; changingTrack?: boolean }>;

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

  // Zone update actions
  updateZoneTrack: (zoneIndex: number, track: any) => void;
  updateZonePlaylist: (zoneIndex: number, playlist: any) => void;
  updateZonePlaybackState: (zoneIndex: number, playbackState: PlaybackState) => void;
  updateZoneMute: (zoneIndex: number, muted: boolean) => void;
  setInitialClientState: (clientIndex: number, clientState: any) => void;
  initializeZone: (zoneIndex: number) => void;
  initializeClient: (clientIndex: number) => void;
  
  // Playlist actions
  setPlaylists: (playlists: PlaylistInfo[]) => void;
  setZoneLoadingState: (zoneIndex: number, loadingState: { changingPlaylist?: boolean; changingTrack?: boolean }) => void;
  
  // Zone control actions (API + optimistic updates)
  changeZonePlaylist: (zoneIndex: number, playlistIndex: number) => Promise<void>;
  setZoneVolume: (zoneIndex: number, volume: number) => Promise<void>;
  toggleZoneMute: (zoneIndex: number) => Promise<void>;
  playZone: (zoneIndex: number) => Promise<void>;
  pauseZone: (zoneIndex: number) => Promise<void>;
  nextTrack: (zoneIndex: number) => Promise<void>;
  prevTrack: (zoneIndex: number) => Promise<void>;
  toggleShuffle: (zoneIndex: number) => Promise<void>;
  toggleRepeat: (zoneIndex: number) => Promise<void>;
  moveClientToZone: (clientIndex: number, targetZoneIndex: number) => Promise<void>;
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
      playlists: [],
      loadingStates: {},

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
      // Zone update actions
      updateZoneTrack: (zoneIndex, track) =>
        set(state => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              track
            }
          }
        })),

      updateZonePlaylist: (zoneIndex, playlist) =>
        set(state => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playlist
            }
          }
        })),

      updateZonePlaybackState: (zoneIndex, playbackState) =>
        set(state => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playbackState
            }
          }
        })),

      updateZoneMute: (zoneIndex, muted) =>
        set(state => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              muted
            }
          }
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
        set(state => {
          const updatedZones = { ...state.zones };
          
          // If client has a zone assignment, add it to that zone's client list
          if (clientData.zoneIndex) {
            if (!updatedZones[clientData.zoneIndex]) {
              updatedZones[clientData.zoneIndex] = { ...defaultZoneState, name: `Zone ${clientData.zoneIndex}` };
            }
            if (!updatedZones[clientData.zoneIndex].clients.includes(clientIndex)) {
              updatedZones[clientData.zoneIndex].clients = [...updatedZones[clientData.zoneIndex].clients, clientIndex];
            }
          }

          return {
            zones: updatedZones,
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
          };
        }),

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

      // Playlist actions
      setPlaylists: (playlists) =>
        set(() => ({
          playlists,
        })),

      setZoneLoadingState: (zoneIndex, loadingState) =>
        set((state) => ({
          loadingStates: {
            ...state.loadingStates,
            [zoneIndex]: {
              ...state.loadingStates[zoneIndex],
              ...loadingState,
            },
          },
        })),

      // Zone control actions (API + optimistic updates)
      changeZonePlaylist: async (zoneIndex, playlistIndex) => {
        // Optimistic update
        useAppStore.setState((state) => ({
          loadingStates: {
            ...state.loadingStates,
            [zoneIndex]: { ...state.loadingStates[zoneIndex], changingPlaylist: true },
          },
        }));

        try {
          await playlistApi.setZonePlaylist(zoneIndex, playlistIndex);
          // SignalR will handle the actual playlist update
        } catch (error) {
          console.error('Failed to change playlist:', error);
          // Revert optimistic update on error
          useAppStore.setState((state) => ({
            loadingStates: {
              ...state.loadingStates,
              [zoneIndex]: { ...state.loadingStates[zoneIndex], changingPlaylist: false },
            },
          }));
          throw error;
        }
      },

      setZoneVolume: async (zoneIndex, volume) => {
        // Store original value for rollback
        const originalVolume = useAppStore.getState().zones[zoneIndex]?.volume || 0;
        
        // Optimistic update
        useAppStore.setState((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              volume,
            },
          },
        }));

        try {
          await api.zones.setVolume(zoneIndex, volume);
        } catch (error) {
          // Rollback on failure
          useAppStore.setState((state) => ({
            zones: {
              ...state.zones,
              [zoneIndex]: {
                ...state.zones[zoneIndex] || defaultZoneState,
                volume: originalVolume,
              },
            },
          }));
          console.error('Failed to set volume:', error);
          throw error;
        }
      },

      toggleZoneMute: async (zoneIndex) => {
        const currentZone = useAppStore.getState().zones[zoneIndex];
        const newMuted = !currentZone?.muted;
        
        // Optimistic update
        useAppStore.setState((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              muted: newMuted,
            },
          },
        }));

        try {
          await api.zones.toggleMute(zoneIndex);
        } catch (error) {
          console.error('Failed to toggle mute:', error);
          throw error;
        }
      },

      playZone: async (zoneIndex) => {
        // Optimistic update
        useAppStore.setState((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playbackState: 'playing',
            },
          },
        }));

        try {
          await api.zones.play(zoneIndex);
        } catch (error) {
          console.error('Failed to play:', error);
          throw error;
        }
      },

      pauseZone: async (zoneIndex) => {
        // Optimistic update
        useAppStore.setState((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playbackState: 'paused',
            },
          },
        }));

        try {
          await api.zones.pause(zoneIndex);
        } catch (error) {
          console.error('Failed to pause:', error);
          throw error;
        }
      },

      nextTrack: async (zoneIndex) => {
        try {
          await api.zones.next(zoneIndex);
        } catch (error) {
          console.error('Failed to skip to next track:', error);
          throw error;
        }
      },

      prevTrack: async (zoneIndex) => {
        try {
          await api.zones.previous(zoneIndex);
        } catch (error) {
          console.error('Failed to skip to previous track:', error);
          throw error;
        }
      },

      toggleShuffle: async (zoneIndex) => {
        const currentZone = useAppStore.getState().zones[zoneIndex];
        const newShuffle = !currentZone?.playlistShuffle;
        
        // Optimistic update
        useAppStore.setState((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              playlistShuffle: newShuffle,
            },
          },
        }));

        try {
          await api.zones.toggleShuffle(zoneIndex);
        } catch (error) {
          console.error('Failed to toggle shuffle:', error);
          throw error;
        }
      },

      toggleRepeat: async (zoneIndex) => {
        const currentZone = useAppStore.getState().zones[zoneIndex];
        const newRepeat = !currentZone?.trackRepeat;
        
        // Optimistic update
        useAppStore.setState((state) => ({
          zones: {
            ...state.zones,
            [zoneIndex]: {
              ...state.zones[zoneIndex] || defaultZoneState,
              trackRepeat: newRepeat,
            },
          },
        }));

        try {
          await api.zones.toggleRepeat(zoneIndex);
        } catch (error) {
          console.error('Failed to toggle repeat:', error);
          throw error;
        }
      },

      moveClientToZone: async (clientIndex, targetZoneIndex) => {
        const state = useAppStore.getState();
        const client = state.clients[clientIndex];
        const currentZoneIndex = client?.zoneIndex;

        // Optimistic update - remove from current zone and add to target zone
        useAppStore.setState((state) => {
          const newState = { ...state };
          
          // Update client's zone assignment
          newState.clients = {
            ...state.clients,
            [clientIndex]: {
              ...state.clients[clientIndex],
              zoneIndex: targetZoneIndex,
            },
          };

          // Remove from current zone's client list
          if (currentZoneIndex && newState.zones[currentZoneIndex]) {
            newState.zones[currentZoneIndex] = {
              ...newState.zones[currentZoneIndex],
              clients: newState.zones[currentZoneIndex].clients.filter(id => id !== clientIndex),
            };
          }

          // Add to target zone's client list
          if (newState.zones[targetZoneIndex]) {
            const targetClients = newState.zones[targetZoneIndex].clients || [];
            if (!targetClients.includes(clientIndex)) {
              newState.zones[targetZoneIndex] = {
                ...newState.zones[targetZoneIndex],
                clients: [...targetClients, clientIndex],
              };
            }
          }

          return newState;
        });

        try {
          await api.clients.moveToZone(clientIndex, targetZoneIndex);
        } catch (error) {
          console.error('Failed to move client:', error);
          throw error;
        }
      },
    }),
    { name: 'snapdog-store' }
  )
);

export const useZone = (zoneIndex: number) => useAppStore((state) => state.zones[zoneIndex]);
export const useClient = (clientIndex: number) => useAppStore((state) => state.clients[clientIndex]);
export const useZoneLoadingState = (zoneIndex: number) => useAppStore((state) => state.loadingStates[zoneIndex] || {});
export const usePlaylists = () => useAppStore((state) => state.playlists);
export const useUnassignedClients = () => useAppStore((state) => 
    Object.entries(state.clients)
        .filter(([, client]: [string, ClientState]) => client.zoneIndex === undefined || client.zoneIndex === null)
        .map(([index]) => parseInt(index))
);