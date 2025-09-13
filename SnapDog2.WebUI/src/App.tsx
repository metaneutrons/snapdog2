import React, { useState, useEffect } from 'react';
import { useAppStore } from './store';
import { api } from './services/api';
import { ZoneCard } from './components/ZoneCard';
import { ThemeToggle } from './components/ThemeToggle';

function App() {
  console.log('App component rendering...');
  
  const { initializeZone, initializeClient, setInitialZoneState, setInitialClientState } = useAppStore();
  console.log('Store loaded successfully');
  
  const [zoneCount, setZoneCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const init = async () => {
      try {
        console.log('Starting API initialization...');
        
        // Get zone count from API
        const zonesCount = await api.get.zoneCount();
        console.log('Zone count from API:', zonesCount);
        
        const fetchedZoneCount = Number(zonesCount);
        setZoneCount(fetchedZoneCount);

        // Initialize zones in store
        for (let i = 1; i <= fetchedZoneCount; i++) {
          initializeZone(i);
        }

        setIsLoading(false);
        console.log('Initialization complete');
      } catch (error) {
        console.error('API initialization failed:', error);
        // Fallback to hardcoded
        setZoneCount(2);
        initializeZone(1);
        initializeZone(2);
        setIsLoading(false);
      }
    };

    init();
  }, []);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-100 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-xl text-gray-600 dark:text-gray-400">Loading zones...</div>
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
                onClientMove={() => {}}
                onDragStart={() => {}}
                onDragEnd={() => {}}
                draggingClientIndex={null}
              />
            ))}
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;
