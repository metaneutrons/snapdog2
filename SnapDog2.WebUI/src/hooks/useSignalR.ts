
import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { useAppStore } from '../store';
import type { TrackInfo, PlaybackState, PlaylistInfo } from '../types';

export function useSignalR(baseUrl: string = '') {
  const connectionRef = useRef<HubConnection | null>(null);
  const {
    updateZoneProgress,
    updateZoneTrack,
    updateZoneVolume,
    updateZonePlayback,
    updateZoneMute,
    updateZoneRepeat,
    updateZoneShuffle,
    updateZonePlaylist,
    updateClientConnection,
    updateClientZone,
    updateClientVolume,
    updateClientMute,
    updateClientLatency,
  } = useAppStore();

  useEffect(() => {
    const hubUrl = baseUrl ? `${baseUrl}/hubs/snapdog/v1` : '/hubs/snapdog/v1';
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    connectionRef.current = connection;
    
    // Zone event handlers
    connection.on('ZoneProgressChanged', (zoneIndex: number, position: number, progress: number) => {
      console.log('📡 SignalR: ZoneProgressChanged', { zoneIndex, position, progress });
      updateZoneProgress(zoneIndex, { position, progress });
    });
    
    connection.on('ZoneTrackMetadataChanged', (zoneIndex: number, track: TrackInfo) => {
      console.log('📡 SignalR: ZoneTrackMetadataChanged', { zoneIndex, track });
      updateZoneTrack(zoneIndex, track);
    });
    
    connection.on('ZoneVolumeChanged', (zoneIndex: number, volume: number) => {
      console.log('📡 SignalR: ZoneVolumeChanged', { zoneIndex, volume });
      updateZoneVolume(zoneIndex, volume);
    });
    
    connection.on('ZonePlaybackChanged', (zoneIndex: number, playbackState: PlaybackState) => {
      console.log('📡 SignalR: ZonePlaybackChanged', { zoneIndex, playbackState });
      updateZonePlayback(zoneIndex, playbackState);
    });
    
    connection.on('ZoneMuteChanged', (zoneIndex: number, muted: boolean) => {
      console.log('📡 SignalR: ZoneMuteChanged', { zoneIndex, muted });
      updateZoneMute(zoneIndex, muted);
    });
    
    connection.on('ZoneRepeatModeChanged', (zoneIndex: number, trackRepeat: boolean, playlistRepeat: boolean) => {
      console.log('📡 SignalR: ZoneRepeatModeChanged', { zoneIndex, trackRepeat, playlistRepeat });
      updateZoneRepeat(zoneIndex, trackRepeat, playlistRepeat);
    });
    
    connection.on('ZoneShuffleChanged', (zoneIndex: number, shuffle: boolean) => {
      console.log('📡 SignalR: ZoneShuffleChanged', { zoneIndex, shuffle });
      updateZoneShuffle(zoneIndex, shuffle);
    });
    
    connection.on('ZonePlaylistChanged', (zoneIndex: number, playlist: PlaylistInfo | null) => {
      console.log('📡 SignalR: ZonePlaylistChanged', { zoneIndex, playlist });
      updateZonePlaylist(zoneIndex, playlist);
    });

    // Client event handlers
    connection.on('ClientConnected', (clientIndex: number, connected: boolean) => {
      console.log('📡 SignalR: ClientConnected', { clientIndex, connected });
      updateClientConnection(clientIndex, connected);
    });
    
    connection.on('ClientZoneChanged', (clientIndex: number, zoneIndex?: number) => {
      console.log('📡 SignalR: ClientZoneChanged', { clientIndex, zoneIndex });
      updateClientZone(clientIndex, zoneIndex);
    });
    
    connection.on('ClientVolumeChanged', (clientIndex: number, volume: number) => {
      console.log('📡 SignalR: ClientVolumeChanged', { clientIndex, volume });
      updateClientVolume(clientIndex, volume);
    });
    
    connection.on('ClientMuteChanged', (clientIndex: number, muted: boolean) => {
      console.log('📡 SignalR: ClientMuteChanged', { clientIndex, muted });
      updateClientMute(clientIndex, muted);
    });
    
    connection.on('ClientLatencyChanged', (clientIndex: number, latency: number) => {
      console.log('📡 SignalR: ClientLatencyChanged', { clientIndex, latency });
      updateClientLatency(clientIndex, latency);
    });

    // System event handlers
    connection.on('ErrorOccurred', (errorCode: string, message: string, context?: string) => {
      console.error('📡 SignalR: ErrorOccurred', { errorCode, message, context });
    });
    
    connection.on('SystemStatusChanged', (status: any) => {
      console.log('📡 SignalR: SystemStatusChanged', status);
    });

    // Connection state handlers
    connection.onreconnecting((error) => {
      console.warn('🔄 SignalR reconnecting...', error);
    });

    connection.onreconnected((connectionId) => {
      console.log('✅ SignalR reconnected:', connectionId);
    });

    connection.onclose((error) => {
      console.error('❌ SignalR connection closed:', error);
    });

    const startConnection = async () => {
      try {
        console.log('🚀 Starting SignalR connection to:', hubUrl);
        await connection.start();
        console.log('✅ SignalR connected successfully');
        
        // Join system group to receive all notifications
        await connection.invoke('JoinSystem');
        console.log('📡 Joined SignalR system group');
        
        // Store connection globally for debugging
        (window as any).signalRConnection = connection;
        
      } catch (error) {
        console.error('❌ SignalR connection failed:', error);
        setTimeout(startConnection, 5000);
      }
    };

    startConnection();

    return () => {
      if (connectionRef.current) {
        console.log('🔌 Stopping SignalR connection');
        connectionRef.current.stop();
      }
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return connectionRef.current;
}