
import React from 'react';
import { ClientChip } from './ClientChip';

interface ClientListProps {
  clientIndices: number[];
  isDropTarget: boolean;
  onDragStart: (clientIndex: number) => void;
  onDragEnd: () => void;
}

export function ClientList({ clientIndices, isDropTarget, onDragStart, onDragEnd }: ClientListProps) {
  return (
    <div className={`space-y-2 p-2 rounded-lg transition-colors duration-200 min-h-[6rem] ${isDropTarget ? 'bg-blue-100 border-2 border-dashed border-blue-400' : 'bg-gray-100'}`}>
      {clientIndices.length > 0 ? (
        clientIndices.map(clientIndex => (
          <ClientChip
            key={clientIndex}
            clientIndex={clientIndex}
            onDragStart={onDragStart}
            onDragEnd={onDragEnd}
          />
        ))
      ) : (
        <div className="flex items-center justify-center h-full text-sm text-gray-500 p-4">
          {isDropTarget ? 'Drop client here' : 'No clients assigned'}
        </div>
      )}
    </div>
  );
}
