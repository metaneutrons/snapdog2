
import React, { useState } from 'react';
import { useUnassignedClients } from '../store';
import { ClientList } from './ClientList';
import { api } from '../services/api';

interface UnassignedClientsProps {
    draggingClientIndex: number | null;
    onClientDragStart: (clientIndex: number) => void;
    onClientDragEnd: () => void;
}

export function UnassignedClients({ draggingClientIndex, onClientDragStart, onClientDragEnd }: UnassignedClientsProps) {
  const unassignedClientIndices = useUnassignedClients();
  const [isDropTarget, setIsDropTarget] = useState(false);

  const isClientAlreadyUnassigned = draggingClientIndex !== null && unassignedClientIndices.includes(draggingClientIndex);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    if(draggingClientIndex !== null && !isClientAlreadyUnassigned) {
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
        await api.clients.assignZone(clientIndex, null);
      } catch (error) {
        console.error(`Failed to unassign client ${clientIndex}:`, error);
      }
    }
  };

  return (
    <div 
        className="bg-white rounded-xl shadow-lg p-5 border border-gray-200 h-full"
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
    >
      <h3 className="text-xl font-bold text-gray-800 mb-4">Unassigned Clients</h3>
      <ClientList 
        clientIndices={unassignedClientIndices} 
        isDropTarget={isDropTarget}
        onDragStart={onClientDragStart}
        onDragEnd={onClientDragEnd}
      />
    </div>
  );
}
