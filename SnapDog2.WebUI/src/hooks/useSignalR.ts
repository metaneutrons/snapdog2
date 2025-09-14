import { useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { useAppStore } from '../store';
import { config } from '../services/config';
import { api } from '../services/api';

export function useSignalR() {
  const connectionRef = useRef<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const { setInitialZoneState, setInitialClientState, setZoneVolume, toggleZoneMute, moveClientToZone, updateZoneTrack, updateZonePlaylist, updateZonePlaybackState, updateZoneMute, updateClientVolume, updateClientMute, initializeZone, initializeClient } = useAppStore();

  const refreshFullState = async () => {
    try {
      console.log('SignalR: Refreshing full state...');
      const zoneCount = await api.get.zoneCount();
      
      for (let i = 1; i <= zoneCount; i++) {
        const [zoneState, clients] = await Promise.all([
          api.get.zoneState(i),
          api.get.zoneClients(i)
        ]);
        
        initializeZone(i);
        setInitialZoneState(i, zoneState);
        clients.forEach(client => {
          initializeClient(client.index);
          setInitialClientState(client.index, client);
        });
      }
      
      console.log('SignalR: Full state refresh complete');
    } catch (error) {
      console.error('SignalR: Failed to refresh full state:', error);
    }
  };

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(config.signalr.hubUrl)
      .withAutomaticReconnect([0, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000])
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    // Connection state handlers
    connection.onreconnecting(() => {
      console.log('SignalR: Reconnecting...');
      setIsConnected(false);
    });

    connection.onreconnected(async () => {
      console.log('SignalR: Reconnected');
      setIsConnected(true);
      await refreshFullState();
    });

    connection.onclose(() => {
      console.log('SignalR: Connection closed');
      setIsConnected(false);
    });

    // Zone-related events (using actual SendAsync event names)
    connection.on('ZoneVolumeChanged', (zoneIndex: number, volume: number) => {
      console.log('SignalR: Zone volume changed', zoneIndex, volume);
      setZoneVolume(zoneIndex, volume);
    });

    connection.on('ZoneMuteChanged', (zoneIndex: number, isMuted: boolean) => {
      console.log('SignalR: Zone mute changed', zoneIndex, isMuted);
      updateZoneMute(zoneIndex, isMuted);
    });

    connection.on('ZonePlaybackStateChanged', (zoneIndex: number, playbackState: string) => {
      console.log('SignalR: Zone playback state changed', zoneIndex, playbackState);
      updateZonePlaybackState(zoneIndex, playbackState as any);
    });

    connection.on('ZoneTrackChanged', (zoneIndex: number, track: any) => {
      console.log('SignalR: Zone track changed', zoneIndex, track);
      updateZoneTrack(zoneIndex, track);
    });

    connection.on('ZoneTrackMetadataChanged', (zoneIndex: number, trackInfo: any) => {
      console.log('SignalR: Zone track metadata changed', zoneIndex, trackInfo);
      updateZoneTrack(zoneIndex, trackInfo);
    });

    connection.on('ZoneProgressChanged', (zoneIndex: number, progress: any) => {
      console.log('SignalR: Zone progress changed', zoneIndex, progress);
      // For progress, we only want to update the track's position, not replace the whole track
      updateZoneTrack(zoneIndex, progress);
    });

    connection.on('ZonePlaylistChanged', (zoneIndex: number, playlist: any) => {
      console.log('SignalR: Zone playlist changed', zoneIndex, playlist);
      updateZonePlaylist(zoneIndex, playlist);
    });

    // Client-related events (using actual SendAsync event names)
    connection.on('ClientVolumeChanged', (clientIndex: number, volume: number) => {
      console.log('SignalR: Client volume changed', clientIndex, volume);
      updateClientVolume(clientIndex, volume);
    });

    connection.on('ClientMuteChanged', (clientIndex: number, muted: boolean) => {
      console.log('SignalR: Client mute changed', clientIndex, muted);
      updateClientMute(clientIndex, muted);
    });

    connection.on('ClientConnectionChanged', (clientIndex: number, connected: boolean) => {
      console.log('SignalR: Client connection changed', clientIndex, connected);
      // Update client connection state
      setInitialClientState(clientIndex, { connected });
    });

    connection.on('ClientNameChanged', (clientIndex: number, name: string) => {
      console.log('SignalR: Client name changed', clientIndex, name);
      // Update client name
      setInitialClientState(clientIndex, { name });
    });

    connection.on('ClientLatencyChanged', (clientIndex: number, latencyMs: number) => {
      console.log('SignalR: Client latency changed', clientIndex, latencyMs);
      // Update client latency
      setInitialClientState(clientIndex, { latencyMs });
    });

    // Start connection
    connection.start()
      .then(async () => {
        console.log('SignalR: Connected successfully to', config.signalr.hubUrl);
        setIsConnected(true);
        await refreshFullState();
      })
      .catch((error) => {
        console.warn('SignalR: Connection failed', error.message);
        setIsConnected(false);
      });

    // Cleanup on unmount
    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
        console.log('SignalR: Connection stopped');
      }
    };
  }, [setInitialZoneState, setInitialClientState, setZoneVolume, toggleZoneMute, moveClientToZone, updateZoneTrack, updateZonePlaylist, updateZonePlaybackState, updateZoneMute]);

  return {
    connection: connectionRef.current,
    isConnected
  };
}
