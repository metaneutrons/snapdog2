import React from 'react';
import { useZone } from '../store';
import { ClientChip } from './ClientChip';

interface ClientListProps {
  zoneIndex: number;
  draggingClientIndex: number | null;
  onClientDragStart: (clientIndex: number) => void;
  onClientDragEnd: () => void;
}

export function ClientList({ zoneIndex, draggingClientIndex, onClientDragStart, onClientDragEnd }: ClientListProps) {
  const zone = useZone(zoneIndex);
  
  if (!zone) return null;

  const clientIndices = zone.clients || [];
  const isDropTarget = draggingClientIndex !== null;

  return (
    <div className={`space-y-2 p-2 rounded-lg transition-colors duration-200 min-h-[6rem] border ${isDropTarget ? 'bg-gray-400 border-2 border-dashed border-blue-400' : 'bg-theme-tertiary border-theme-secondary'}`}>
      {clientIndices.length > 0 ? (
        clientIndices.map((clientIndex) => (
          <ClientChip
            key={clientIndex}
            clientIndex={clientIndex}
            isDragging={draggingClientIndex === clientIndex}
            onDragStart={onClientDragStart}
            onDragEnd={onClientDragEnd}
          />
        ))
      ) : (
        <div className="text-theme-tertiary text-sm text-center py-4">
          No clients assigned
        </div>
      )}
    </div>
  );
}
