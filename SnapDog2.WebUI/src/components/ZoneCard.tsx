import React, { useState, useEffect } from 'react';
import { useZone, useAppStore } from '../store';
import { useEventBus } from '../hooks/useEventBus';
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
}

export const ZoneCard: React.FC<ZoneCardProps> = ({ 
  zoneIndex, 
  draggingClientIndex
}) => {
  const zone = useZone(zoneIndex);
  const { setZoneVolume, toggleZoneMute } = useAppStore();
  const { emit } = useEventBus();
  const [imageError, setImageError] = useState<boolean>(false);

  const handleClientMove = (clientIndex: number, targetZoneIndex: number) => {
    emit('client.move', { clientIndex, targetZoneIndex });
  };

  const handleDragStart = (clientIndex: number) => {
    emit('client.drag.start', { clientIndex });
  };

  const handleDragEnd = () => {
    emit('client.drag.end', {});
  };

  const handleDrop = (clientIndex: number, targetZoneIndex: number) => {
    console.log(`Dropping client ${clientIndex} into zone ${targetZoneIndex}`);
    
    // Get current zone of the client
    const client = useAppStore.getState().clients[clientIndex];
    const currentZoneIndex = client?.zoneIndex;
    
    // Only move if dropping into a different zone
    if (currentZoneIndex !== targetZoneIndex) {
      handleClientMove(clientIndex, targetZoneIndex);
    } else {
      console.log(`Client ${clientIndex} dropped in same zone ${targetZoneIndex}, ignoring move`);
    }
  };

  if (!zone) {
    return (
      <div className="bg-theme-secondary rounded-lg shadow-theme p-6 border border-theme-primary">
        <div className="text-theme-tertiary">Zone {zoneIndex} not found</div>
      </div>
    );
  }

  const currentTrack = zone.track;
  const currentPlaylist = zone.playlist;

  // Reset image error when track changes
  useEffect(() => {
    setImageError(false);
  }, [currentTrack?.coverArtUrl]);

  return (
    <div className="bg-theme-secondary rounded-lg shadow-theme p-6 border border-theme-primary">
      {/* Zone Header */}
      <div className="flex justify-between items-center mb-4">
        <h3 className="text-lg font-semibold text-theme-primary">{zone.name}</h3>
        <div className="text-sm text-theme-tertiary">
          Zone {zoneIndex}
        </div>
      </div>

      {/* Current Track Info */}
      {currentTrack && (
        <div className="mb-4 p-3 bg-theme-tertiary rounded-lg">
          <div className="flex items-center space-x-3">
            {/* Cover Art */}
            {currentTrack.coverArtUrl && !imageError ? (
              <img 
                src={currentTrack.coverArtUrl} 
                alt="Album Cover"
                className="w-24 h-24 rounded-md object-cover flex-shrink-0"
                onError={() => setImageError(true)}
                onLoad={() => setImageError(false)}
              />
            ) : (
              <div className="w-24 h-24 bg-gray-300 dark:bg-gray-600 rounded-md flex items-center justify-center flex-shrink-0">
                <span className="text-gray-500 dark:text-gray-400 text-4xl">♪</span>
              </div>
            )}
            
            {/* Track Info */}
            <div className="flex-1 min-w-0">
              <div className="text-sm font-medium text-theme-primary truncate">
                {currentTrack.title || 'Unknown Track'}
              </div>
              <div className="text-xs text-theme-tertiary truncate">
                {currentTrack.artist || 'Unknown Artist'}
              </div>
              {currentTrack.durationMs && currentTrack.positionMs !== null && (
                <>
                  <div className="text-xs text-theme-tertiary mt-1">
                    {formatTime(currentTrack.positionMs)} / {formatTime(currentTrack.durationMs)}
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-1.5 mt-2">
                    <div 
                      className="bg-blue-600 h-1.5 rounded-full transition-all duration-300 ease-out"
                      style={{ 
                        width: `${Math.min(100, Math.max(0, (currentTrack.positionMs / currentTrack.durationMs) * 100))}%` 
                      }}
                    ></div>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Transport Controls */}
      <div className="mb-4">
        <TransportControls zoneIndex={zoneIndex} />
      </div>

      {/* Playlist Selector */}
      <div className="mb-4">
        <PlaylistSelector 
          zoneIndex={zoneIndex}
          currentPlaylistIndex={currentPlaylist?.index}
          currentPlaylistName={currentPlaylist?.name}
        />
      </div>

      {/* Client List */}
      <div>
        <h4 className="text-sm font-medium text-theme-primary mb-2">Clients</h4>
        <ClientList 
          zoneIndex={zoneIndex}
          draggingClientIndex={draggingClientIndex}
          onClientDragStart={handleDragStart}
          onClientDragEnd={handleDragEnd}
          onDrop={handleDrop}
        />
      </div>
    </div>
  );
};
