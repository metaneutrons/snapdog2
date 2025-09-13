import { useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { useAppStore } from '../store';
import { config } from '../services/config';

export function useSignalR() {
  const connectionRef = useRef<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const { setInitialZoneState, setInitialClientState, setZoneVolume, toggleZoneMute, moveClientToZone, updateZoneTrack, updateZonePlaylist, updateZonePlaybackState, updateZoneMute } = useAppStore();

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(config.signalr.hubUrl)
      .withAutomaticReconnect([0, 2000, 10000, config.signalr.reconnectDelay])
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    // Connection state handlers
    connection.onreconnecting(() => {
      console.log('SignalR: Reconnecting...');
      setIsConnected(false);
    });

    connection.onreconnected(() => {
      console.log('SignalR: Reconnected');
      setIsConnected(true);
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
      // Update client volume in store
      setInitialClientState(clientIndex, { volume });
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
      .then(() => {
        console.log('SignalR: Connected successfully to', config.signalr.hubUrl);
        setIsConnected(true);
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
