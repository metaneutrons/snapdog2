import React, { useState, useEffect } from 'react';
import { useAppStore } from './store';
import { api } from './services/api';
import { ZoneCard } from './components/ZoneCard';
import { ThemeToggle } from './components/ThemeToggle';

function App() {
  console.log('App component rendering...');
  
  const { initializeZone, initializeClient, setInitialZoneState, setInitialClientState, moveClientToZone } = useAppStore();
  console.log('Store loaded successfully');
  
  const [zoneCount, setZoneCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [loadingProgress, setLoadingProgress] = useState('Initializing...');
  const [draggingClientIndex, setDraggingClientIndex] = useState<number | null>(null);

  useEffect(() => {
    const init = async () => {
      try {
        console.log('Starting optimized initialization...');
        setLoadingProgress('Loading clients...');
        
        // Load clients first (fast - 11ms)
        const clients = await api.get.clients();
        console.log('Clients loaded:', clients.length);
        
        clients.forEach((clientData, index) => {
          const clientIndex = index + 1;
          initializeClient(clientIndex);
          setInitialClientState(clientIndex, clientData);
        });
        
        setLoadingProgress('Loading zones...');
        
        // Load zones second (slow - 5000ms)
        const zones = await api.get.zones();
        console.log('Zones loaded:', zones.length);
        
        setZoneCount(zones.length);

        zones.forEach((zoneData, index) => {
          const zoneIndex = index + 1;
          initializeZone(zoneIndex);
          setInitialZoneState(zoneIndex, zoneData);
        });

        setIsLoading(false);
        console.log('Optimized initialization complete');
      } catch (error) {
        console.error('Initialization failed:', error);
        setLoadingProgress('Failed to load. Please refresh.');
        setIsLoading(false);
      }
    };

    init();
  }, []);

  const handleClientMove = async (clientIndex: number, targetZoneIndex: number) => {
    try {
      console.log(`Moving client ${clientIndex} to zone ${targetZoneIndex}`);
      
      // Call API to move client
      await api.post.moveClientToZone(clientIndex, targetZoneIndex);
      
      // Update store
      await moveClientToZone(clientIndex, targetZoneIndex);
      
      console.log('Client move successful');
    } catch (error) {
      console.error('Failed to move client:', error);
    }
  };

  const handleDragStart = (clientIndex: number) => {
    console.log(`Drag started for client ${clientIndex}`);
    setDraggingClientIndex(clientIndex);
  };

  const handleDragEnd = () => {
    console.log('Drag ended');
    setDraggingClientIndex(null);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-100 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="text-xl text-gray-600 dark:text-gray-400 mb-4">{loadingProgress}</div>
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-100 dark:bg-gray-900">
      <header className="bg-white dark:bg-gray-800 shadow">
        <div className="max-w-7xl mx-auto py-6 px-4 sm:px-6 lg:px-8 flex justify-between items-center">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
            SnapDog Audio Control
          </h1>
          <ThemeToggle />
        </div>
      </header>
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {Array.from({ length: zoneCount }, (_, index) => (
              <ZoneCard
                key={index + 1}
                zoneIndex={index + 1}
                onClientMove={handleClientMove}
                onClientDragStart={handleDragStart}
                onClientDragEnd={handleDragEnd}
                draggingClientIndex={draggingClientIndex}
              />
            ))}
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;
