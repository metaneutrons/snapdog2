import React from 'react';
import { useZone, useAppStore } from '../store';
import { TransportControls } from './TransportControls';
import { VolumeSlider } from './VolumeSlider';
import { ClientList } from './ClientList';
import { PlaylistSelector } from './PlaylistSelector';

const formatTime = (ms: number): string => {
  if (!ms || ms < 0) return '0:00';
  const totalSeconds = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
};

interface ZoneCardProps {
  zoneIndex: number;
  draggingClientIndex: number | null;
  onClientDragStart: (clientIndex: number) => void;
  onClientDragEnd: () => void;
  onDrop?: (zoneIndex: number) => void;
}

export const ZoneCard: React.FC<ZoneCardProps> = ({ zoneIndex, draggingClientIndex, onClientDragStart, onClientDragEnd, onDrop }) => {
  const zone = useZone(zoneIndex);
  const { setZoneVolume, toggleZoneMute } = useAppStore();

  if (!zone) {
    return (
      <div className="bg-theme-secondary rounded-lg shadow-theme p-6 border border-theme-primary">
        <h3 className="text-lg font-semibold mb-4 text-theme-primary">Zone {zoneIndex}</h3>
        <p className="text-theme-secondary">Loading...</p>
      </div>
    );
  }

  const handleVolumeChange = (volume: number) => setZoneVolume(zoneIndex, volume).catch(console.error);
  const handleMuteToggle = () => toggleZoneMute(zoneIndex).catch(console.error);

  return (
    <div className="bg-theme-secondary rounded-lg shadow-theme p-6 border border-theme-primary">
      <h3 className="text-lg font-semibold mb-4 text-theme-primary">{zone.name}</h3>
      
      {/* Current track info */}
      <div className="mb-4 p-3 bg-theme-tertiary rounded border border-theme-primary">
        <div className="flex gap-3 mb-2">
          <div className="w-16 h-16 bg-theme-primary rounded flex-shrink-0 flex items-center justify-center border border-theme-secondary">
            {zone.track?.coverArtUrl ? (
              <img 
                src={zone.track.coverArtUrl} 
                alt="Album cover"
                className="w-full h-full object-cover rounded"
              />
            ) : (
              <span className="text-theme-tertiary text-xs">No cover</span>
            )}
          </div>
          <div className="flex-1 min-w-0">
            <h4 className="font-medium truncate text-theme-primary">{zone.track?.title || 'No track'}</h4>
            <p className="text-sm text-theme-secondary truncate">{zone.track?.artist || 'Unknown artist'}</p>
            <p className="text-xs text-theme-tertiary truncate">{zone.track?.album || 'Unknown album'}</p>
          </div>
        </div>
        
        {/* Progress display */}
        {zone.track && (
          <div className="space-y-1">
            {zone.track.durationMs ? (
              <>
                <div className="w-full bg-theme-primary rounded-full h-1 border border-theme-secondary">
                  <div 
                    className="bg-blue-500 h-1 rounded-full transition-all duration-1000" 
                    style={{ 
                      width: `${zone.progress?.progress || 0}%` 
                    }}
                  />
                </div>
                <div className="flex justify-between text-xs text-theme-tertiary">
                  <span>{formatTime(zone.progress?.position || 0)}</span>
                  <span>{formatTime(zone.track.durationMs)}</span>
                </div>
              </>
            ) : (
              <div className="text-xs text-theme-tertiary">
                {formatTime(zone.progress?.position || 0)}
              </div>
            )}
          </div>
        )}
      </div>

      <PlaylistSelector 
        zoneIndex={zoneIndex}
        currentPlaylistIndex={zone.playlist?.index}
        currentPlaylistName={zone.playlist?.name}
      />

      <TransportControls zoneIndex={zoneIndex} />
      
      <VolumeSlider 
        value={zone.volume || 0}
        muted={zone.muted || false}
        onChange={handleVolumeChange}
        onMuteToggle={handleMuteToggle}
      />
      
      <div className="mt-4">
        <h5 className="text-sm font-medium mb-2 text-theme-primary">Clients</h5>
        <ClientList 
          zoneIndex={zoneIndex}
          draggingClientIndex={draggingClientIndex}
          onClientDragStart={onClientDragStart}
          onClientDragEnd={onClientDragEnd}
          onDrop={onDrop}
        />
      </div>
    </div>
  );
};
