
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
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    connectionRef.current = connection;
    
    // Zone event handlers
    connection.on('ZoneProgressChanged', (zoneIndex: number, position: number, progress: number) => {
      updateZoneProgress(zoneIndex, { position, progress });
    });
    connection.on('ZoneTrackMetadataChanged', (zoneIndex: number, track: TrackInfo) => {
      updateZoneTrack(zoneIndex, track);
    });
    connection.on('ZoneVolumeChanged', (zoneIndex: number, volume: number) => {
      updateZoneVolume(zoneIndex, volume);
    });
    connection.on('ZonePlaybackChanged', (zoneIndex: number, playbackState: PlaybackState) => {
      updateZonePlayback(zoneIndex, playbackState);
    });
    connection.on('ZoneMuteChanged', (zoneIndex: number, muted: boolean) => {
      updateZoneMute(zoneIndex, muted);
    });
    connection.on('ZoneRepeatModeChanged', (zoneIndex: number, trackRepeat: boolean, playlistRepeat: boolean) => {
      updateZoneRepeat(zoneIndex, trackRepeat, playlistRepeat);
    });
    connection.on('ZoneShuffleChanged', (zoneIndex: number, shuffle: boolean) => {
      updateZoneShuffle(zoneIndex, shuffle);
    });
    connection.on('ZonePlaylistChanged', (zoneIndex: number, playlist: PlaylistInfo | null) => {
      updateZonePlaylist(zoneIndex, playlist);
    });

    // Client event handlers
    connection.on('ClientConnected', (clientIndex: number, connected: boolean) => {
      updateClientConnection(clientIndex, connected);
    });
    connection.on('ClientZoneChanged', (clientIndex: number, zoneIndex?: number) => {
      updateClientZone(clientIndex, zoneIndex);
    });
    connection.on('ClientVolumeChanged', (clientIndex: number, volume: number) => {
      updateClientVolume(clientIndex, volume);
    });
    connection.on('ClientMuteChanged', (clientIndex: number, muted: boolean) => {
      updateClientMute(clientIndex, muted);
    });
    connection.on('ClientLatencyChanged', (clientIndex: number, latency: number) => {
      updateClientLatency(clientIndex, latency);
    });

    // System event handlers
    connection.on('ErrorOccurred', (errorCode: string, message: string, context?: string) => {
      console.error(`System error ${errorCode}: ${message}`, context);
    });
    connection.on('SystemStatusChanged', (status: any) => {
      console.log('System status changed:', status);
    });

    const startConnection = async () => {
      try {
        await connection.start();
        console.log('SignalR connected');
        await connection.invoke('JoinSystem');
      } catch (error) {
        console.error('SignalR connection failed:', error);
        setTimeout(startConnection, 5000);
      }
    };

    startConnection();

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return connectionRef.current;
}