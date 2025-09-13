import React, { useState, useEffect } from 'react';
import { useAppStore } from './store';
import { useSignalR } from './hooks/useSignalR';
import { useEventBus } from './hooks/useEventBus';
import { api } from './services/api';
import { ZoneCard } from './components/ZoneCard';
import { ZoneErrorBoundary } from './components/ZoneErrorBoundary';
import { ThemeToggle } from './components/ThemeToggle';

function App() {
  console.log('App component rendering...');
  
  const { initializeZone, initializeClient, setInitialZoneState, setInitialClientState, moveClientToZone } = useAppStore();
  const { isConnected } = useSignalR();
  const { on, emit } = useEventBus();
  console.log('Store loaded successfully, SignalR connected:', isConnected);
  
  const [zoneCount, setZoneCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [loadingProgress, setLoadingProgress] = useState('Initializing...');
  const [draggingClientIndex, setDraggingClientIndex] = useState<number | null>(null);

  // Event handlers
  on('client.move', async ({ clientIndex, targetZoneIndex }) => {
    try {
      console.log(`Event: Moving client ${clientIndex} to zone ${targetZoneIndex}`);
      await api.post.moveClientToZone(clientIndex, targetZoneIndex);
      await moveClientToZone(clientIndex, targetZoneIndex);
      console.log('Event: Client move successful');
    } catch (error) {
      console.error('Event: Failed to move client:', error);
    }
  });

  on('zone.volume.change', async ({ zoneIndex, volume }) => {
    try {
      console.log(`Event: Changing zone ${zoneIndex} volume to ${volume}`);
      await api.post.setZoneVolume(zoneIndex, volume);
      console.log('Event: Volume change successful');
    } catch (error) {
      console.error('Event: Failed to change volume:', error);
    }
  });

  on('zone.mute.toggle', async ({ zoneIndex }) => {
    try {
      console.log(`Event: Toggling zone ${zoneIndex} mute`);
      await api.post.toggleZoneMute(zoneIndex);
      console.log('Event: Mute toggle successful');
    } catch (error) {
      console.error('Event: Failed to toggle mute:', error);
    }
  });

  on('client.drag.start', ({ clientIndex }) => {
    console.log(`Event: Drag started for client ${clientIndex}`);
    setDraggingClientIndex(clientIndex);
  });

  on('client.drag.end', () => {
    console.log('Event: Drag ended');
    setDraggingClientIndex(null);
  });

  useEffect(() => {
    const init = async () => {
      try {
        console.log('Starting optimized initialization...');
        setLoadingProgress('Loading clients...');
        
        const clients = await api.get.clients();
        console.log('Clients loaded:', clients.length);
        
        clients.forEach((clientData, index) => {
          const clientIndex = index + 1;
          initializeClient(clientIndex);
          setInitialClientState(clientIndex, clientData);
        });
        
        setLoadingProgress('Loading zones...');
        
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
          <div className="flex items-center space-x-4">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
              SnapDog Audio Control
            </h1>
            <div className="flex items-center space-x-2">
              <div className={`w-2 h-2 rounded-full ${isConnected ? 'bg-green-500' : 'bg-red-500'}`}></div>
              <span className="text-sm text-gray-600 dark:text-gray-400">
                {isConnected ? 'Connected' : 'Disconnected'}
              </span>
            </div>
          </div>
          <ThemeToggle />
        </div>
      </header>
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {Array.from({ length: zoneCount }, (_, index) => (
              <ZoneErrorBoundary key={index + 1} zoneIndex={index + 1}>
                <ZoneCard
                  zoneIndex={index + 1}
                  draggingClientIndex={draggingClientIndex}
                />
              </ZoneErrorBoundary>
            ))}
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;
