import React, { useEffect, useState } from 'react';
import { useZone, useAppStore, useZoneLoadingState } from '../store';
import { api } from '../services/api';
import { TransportControls } from './TransportControls';
import { VolumeSlider } from './VolumeSlider';
import { ClientList } from './ClientList';
import { PlaylistNavigation } from './PlaylistNavigation';

interface ZoneCardProps {
  zoneIndex: number;
  draggingClientIndex: number | null;
  onClientDragStart: (clientIndex: number) => void;
  onClientDragEnd: () => void;
}

const formatTime = (ms: number): string => {
  if (!ms || ms < 0) return '0:00';
  const totalSeconds = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
};

export const ZoneCard: React.FC<ZoneCardProps> = ({ zoneIndex, draggingClientIndex, onClientDragStart, onClientDragEnd }) => {
  const zone = useZone(zoneIndex);
  const { initializeZone } = useAppStore();
  const [isDropTarget, setIsDropTarget] = useState(false);

  useEffect(() => {
    initializeZone(zoneIndex);
  }, [zoneIndex, initializeZone]);

  const handleVolumeChange = (volume: number) => api.zones.setVolume(zoneIndex, volume).catch(console.error);
  const handleMuteToggle = () => api.zones.toggleMute(zoneIndex).catch(console.error);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    if(draggingClientIndex !== null && !zone?.clients.includes(draggingClientIndex)) {
        setIsDropTarget(true);
    }
  };

  const handleDragLeave = () => {
    setIsDropTarget(false);
  };
  
  const handleDrop = async (e: React.DragEvent) => {
    e.preventDefault();
    setIsDropTarget(false);
    const clientIndex = parseInt(e.dataTransfer.getData('text/plain'), 10);
    if (!isNaN(clientIndex)) {
      try {
        await api.clients.assignZone(clientIndex, zoneIndex);
      } catch (error) {
        console.error(`Failed to assign client ${clientIndex} to zone ${zoneIndex}:`, error);
      }
    }
  };
  
  if (!zone) {
    return (
      <div className="bg-white rounded-xl shadow p-6 animate-pulse">
        <div className="h-6 bg-gray-200 rounded mb-4 w-1/3"></div>
        <div className="h-20 bg-gray-200 rounded-lg mb-4"></div>
        <div className="h-10 bg-gray-200 rounded-lg mb-4"></div>
        <div className="h-8 bg-gray-200 rounded-lg"></div>
      </div>
    );
  }

  return (
    <div 
        className="bg-white rounded-xl shadow-lg p-5 border border-gray-200 flex flex-col space-y-4 transition-all duration-300"
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
    >
      <div className="flex items-center justify-between">
        <div>
            <h3 className="text-xl font-bold text-gray-800">{zone.name || `Zone ${zoneIndex}`}</h3>
            {zone.playlist && <p className="text-sm text-gray-500">{zone.playlist.name}</p>}
        </div>
        <div className="flex items-center space-x-2">
          <div className={`w-3 h-3 rounded-full transition-colors ${zone.playbackState === 'playing' ? 'bg-green-500' : 'bg-gray-400'}`} />
          <span className="text-sm font-medium text-gray-600 capitalize">{zone.playbackState}</span>
        </div>
      </div>

      {zone.track ? (
        <div className="p-4 bg-gray-50 rounded-lg">
          <div className="flex items-center space-x-4">
            <img
              src={zone.track.coverArtUrl || 'https://picsum.photos/64'}
              alt="Album cover"
              className="w-16 h-16 rounded-md object-cover bg-gray-200"
            />
            <div className="flex-1 min-w-0">
              <p className="font-semibold text-gray-900 truncate">{zone.track.title}</p>
              <p className="text-gray-600 truncate">{zone.track.artist}</p>
            </div>
          </div>
          {zone.progress && zone.track.durationMs && (
            <div className="mt-3">
              <div className="w-full bg-gray-200 rounded-full h-1.5">
                <div
                  className="bg-blue-600 h-1.5 rounded-full"
                  style={{ width: `${(zone.progress.progress || 0) * 100}%` }}
                />
              </div>
              <div className="flex justify-between text-xs text-gray-500 mt-1">
                <span>{formatTime(zone.progress.position)}</span>
                <span>{formatTime(zone.track.durationMs)}</span>
              </div>
            </div>
          )}
        </div>
      ) : (
        <div className="p-4 bg-gray-50 rounded-lg flex items-center justify-center h-[124px]">
          <p className="text-gray-500">No track playing</p>
        </div>
      )}

      <TransportControls zoneIndex={zoneIndex} />

      <VolumeSlider
        value={zone.volume}
        muted={zone.muted}
        onChange={handleVolumeChange}
        onMuteToggle={handleMuteToggle}
      />
      
      <div>
        <h4 className="text-sm font-medium text-gray-600 mb-1">
          Assigned Clients ({zone.clients.length})
        </h4>
        <ClientList 
            clientIndices={zone.clients} 
            isDropTarget={isDropTarget}
            onDragStart={onClientDragStart}
            onDragEnd={onClientDragEnd}
        />
      </div>
    </div>
  );
};