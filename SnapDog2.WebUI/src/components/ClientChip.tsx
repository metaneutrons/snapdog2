import React, { useEffect } from 'react';
import { useClient, useAppStore } from '../store';
import { api } from '../services/api';
import { VolumeSlider } from './VolumeSlider';

interface ClientChipProps {
  clientIndex: number;
  isDragging?: boolean;
  onDragStart: (clientIndex: number) => void;
  onDragEnd: () => void;
  className?: string;
}

// Fix for: Property 'key' does not exist on type 'ClientChipProps' when used in a list.
export const ClientChip: React.FC<ClientChipProps> = ({
  clientIndex,
  isDragging = false,
  onDragStart,
  onDragEnd,
  className = ''
}) => {
  const client = useClient(clientIndex);
  const { initializeClient } = useAppStore();

  useEffect(() => {
    initializeClient(clientIndex);
  }, [clientIndex, initializeClient]);

  const handleVolumeChange = async (volume: number) => {
    try {
      await api.clients.setVolume(clientIndex, volume);
    } catch (error) {
      console.error('Failed to set client volume:', error);
    }
  };

  const handleMuteToggle = async () => {
    try {
      await api.clients.toggleMute(clientIndex);
    } catch (error) {
      console.error('Failed to toggle client mute:', error);
    }
  };

  const handleDragStart = (e: React.DragEvent) => {
    e.dataTransfer.setData('text/plain', clientIndex.toString());
    e.dataTransfer.effectAllowed = 'move';
    onDragStart(clientIndex);
  };

  if (!client) {
    return (
      <div className={`bg-gray-200 rounded-lg p-3 animate-pulse ${className}`}>
        <div className="h-4 bg-gray-300 rounded mb-2"></div>
        <div className="h-2 bg-gray-300 rounded"></div>
      </div>
    );
  }

  return (
    <div
      className={`
        bg-white border rounded-lg p-3 cursor-move transition-all duration-200 space-y-2
        ${client.connected ? 'border-green-300' : 'border-gray-300'}
        ${isDragging ? 'opacity-50 scale-95 shadow-lg' : 'hover:shadow-md hover:border-blue-400'}
        ${className}
      `}
      draggable
      onDragStart={handleDragStart}
      onDragEnd={onDragEnd}
    >
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-2 min-w-0">
          <div className={`w-2.5 h-2.5 rounded-full flex-shrink-0 ${client.connected ? 'bg-green-500' : 'bg-red-500'}`} />
          <span className="text-sm font-semibold text-gray-800 truncate">{client.name || `Client ${clientIndex}`}</span>
        </div>
        {client.latency !== undefined && (
          <span className="text-xs text-gray-500 flex-shrink-0">{client.latency}ms</span>
        )}
      </div>

      <VolumeSlider
        value={client.volume}
        muted={client.muted}
        onChange={handleVolumeChange}
        onMuteToggle={handleMuteToggle}
        size="sm"
      />
    </div>
  );
};