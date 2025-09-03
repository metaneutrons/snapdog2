import React, { useState, useEffect, useCallback } from 'react';
import { useAppStore } from './store';
import { api } from './services/api';
import { ZoneCard } from './components/ZoneCard';
import { useSignalR } from './hooks/useSignalR';

function App() {
  const { initializeZone, initializeClient, setInitialZoneState, setInitialClientState } = useAppStore();
  const [zoneCount, setZoneCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [draggingClientIndex, setDraggingClientIndex] = useState<number | null>(null);

  const connection = useSignalR('');

  const fetchInitialData = useCallback(async () => {
    console.log('📡 fetchInitialData started');
    try {
      console.log('🔍 Fetching zone and client counts...');
      const [zonesCount, clientsCount] = await Promise.all([
        api.get.zoneCount(),
        api.get.clientCount(),
      ]);
      
      const fetchedZoneCount = Number(zonesCount);
      const fetchedClientCount = Number(clientsCount);
      console.log('📊 Counts:', { zones: fetchedZoneCount, clients: fetchedClientCount });

      setZoneCount(fetchedZoneCount);

      for (let i = 1; i <= fetchedZoneCount; i++) initializeZone(i);
      for (let i = 1; i <= fetchedClientCount; i++) initializeClient(i);

      console.log('🔄 Fetching zone and client states...');
      const zoneStatePromises = Array.from({ length: fetchedZoneCount }, (_, i) => api.get.zone(i + 1));
      const clientStatePromises = Array.from({ length: fetchedClientCount }, (_, i) => api.get.client(i + 1));

      const zoneStates = await Promise.all(zoneStatePromises);
      const clientStates = await Promise.all(clientStatePromises);
      console.log('✅ States fetched successfully');

      zoneStates.forEach((zoneState, i) => setInitialZoneState(i + 1, zoneState));
      clientStates.forEach((clientState, i) => setInitialClientState(i + 1, clientState));
      
      if (connection) {
        console.log('🔗 Joining SignalR zones...');
        for (let i = 1; i <= fetchedZoneCount; i++) {
          await connection.invoke('JoinZone', i);
        }
        console.log('✅ SignalR zones joined');
      }
      
      console.log('✅ fetchInitialData completed successfully');
    } catch (e) {
      console.error('❌ Failed to fetch initial data:', e);
      setError('Failed to load application data');
    } finally {
      console.log('🏁 Setting loading to false');
      setIsLoading(false);
    }
  }, [initializeZone, initializeClient, setInitialZoneState, setInitialClientState, connection]);
  
  useEffect(() => {
    console.log('🚀 Starting fetchInitialData...');
    fetchInitialData();
  }, [fetchInitialData]);

  const handleDragStart = useCallback((clientIndex: number) => {
    setDraggingClientIndex(clientIndex);
  }, []);

  const handleDragEnd = useCallback(() => {
    setDraggingClientIndex(null);
  }, []);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen text-gray-600">
        Loading audio control panel...
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen text-red-600 p-8 text-center">
        {error}
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 text-gray-800 font-sans">
      <header className="bg-white shadow-sm sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <h1 className="text-2xl font-bold text-gray-900">SnapDog Audio Control</h1>
        </div>
      </header>
      <main className="p-4 sm:p-6 lg:p-8">
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6 max-w-7xl mx-auto">
          {Array.from({ length: zoneCount }, (_, i) => i + 1).map((zoneIndex) => (
            <ZoneCard 
              key={zoneIndex} 
              zoneIndex={zoneIndex} 
              draggingClientIndex={draggingClientIndex}
              onClientDragStart={handleDragStart}
              onClientDragEnd={handleDragEnd}
            />
          ))}
        </div>
      </main>
    </div>
  );
}

export default App;
