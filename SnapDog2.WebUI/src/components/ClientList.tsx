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
    <div className={`space-y-2 p-2 rounded-lg transition-colors duration-200 min-h-[6rem] ${isDropTarget ? 'bg-blue-100 border-2 border-dashed border-blue-400' : 'bg-gray-100'}`}>
      {clientIndices.length > 0 ? (
        clientIndices.map((clientIndex) => (
          <ClientChip
            key={clientIndex}
            clientIndex={clientIndex}
            onDragStart={onClientDragStart}
            onDragEnd={onClientDragEnd}
          />
        ))
      ) : (
        <div className="text-gray-500 text-sm text-center py-4">
          No clients assigned
        </div>
      )}
    </div>
  );
}
