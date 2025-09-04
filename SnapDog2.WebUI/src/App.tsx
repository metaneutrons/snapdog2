import React, { useState, useEffect } from 'react';
import { useAppStore } from './store';
import { api } from './services/api';
import { playlistApi } from './services/playlistApi';
import { ZoneCard } from './components/ZoneCard';
import { ThemeToggle } from './components/ThemeToggle';
import { useSignalR } from './hooks/useSignalR';

function App() {
  const { initializeZone, initializeClient, setInitialZoneState, setInitialClientState, setPlaylists, moveClientToZone } = useAppStore();
  const [zoneCount, setZoneCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [draggingClientIndex, setDraggingClientIndex] = useState<number | null>(null);

  const connection = useSignalR('');

  // SignalR event handlers
  useEffect(() => {
    if (!connection) return;

    const handleZoneUpdate = (zoneIndex: number, zoneState: any) => {
      setInitialZoneState(zoneIndex, zoneState);
    };

    const handleClientUpdate = (clientIndex: number, clientState: any) => {
      setInitialClientState(clientIndex, clientState);
    };

    const handleTrackProgress = (zoneIndex: number, positionMs: number, progressPercent: number) => {
      useAppStore.setState((state) => ({
        zones: {
          ...state.zones,
          [zoneIndex]: {
            ...state.zones[zoneIndex],
            progress: {
              position: positionMs,
              progress: progressPercent
            }
          }
        }
      }));
    };

    connection.on('ZoneUpdated', handleZoneUpdate);
    connection.on('ClientUpdated', handleClientUpdate);
    connection.on('TrackProgress', handleTrackProgress);

    return () => {
      connection.off('ZoneUpdated', handleZoneUpdate);
      connection.off('ClientUpdated', handleClientUpdate);
      connection.off('TrackProgress', handleTrackProgress);
    };
  }, [connection]);

  useEffect(() => {
    const init = async () => {
      try {
        const [zonesCount, clientsCount, playlists] = await Promise.all([
          api.get.zoneCount(),
          api.get.clientCount(),
          playlistApi.getPlaylists(),
        ]);
        
        const fetchedZoneCount = Number(zonesCount);
        const fetchedClientCount = Number(clientsCount);

        setZoneCount(fetchedZoneCount);
        setPlaylists(playlists);

        for (let i = 1; i <= fetchedZoneCount; i++) initializeZone(i);
        for (let i = 1; i <= fetchedClientCount; i++) initializeClient(i);

        const zoneStates = await Promise.all(
          Array.from({ length: fetchedZoneCount }, (_, i) => api.get.zone(i + 1))
        );
        const clientStates = await Promise.all(
          Array.from({ length: fetchedClientCount }, (_, i) => api.get.client(i + 1))
        );

        zoneStates.forEach((zoneState, i) => setInitialZoneState(i + 1, zoneState));
        clientStates.forEach((clientState, i) => setInitialClientState(i + 1, clientState));
        
      } catch (e) {
        console.error('Init failed:', e);
      } finally {
        setIsLoading(false);
      }
    };
    init();
  }, []);

  const handleClientDragStart = (clientIndex: number) => {
    setDraggingClientIndex(clientIndex);
  };

  const handleClientDragEnd = () => {
    setDraggingClientIndex(null);
  };

  const handleZoneDrop = async (targetZoneIndex: number) => {
    if (draggingClientIndex) {
      try {
        await moveClientToZone(draggingClientIndex, targetZoneIndex);
      } catch (e) {
        console.error('Failed to move client:', e);
      }
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-theme-primary">
        <div className="text-theme-primary text-lg">Loading...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-theme-primary">
      <header className="bg-theme-secondary shadow-theme sticky top-0 z-10 border-b border-theme-primary">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <h1 className="text-2xl font-bold text-theme-primary">SnapDog Audio Control</h1>
            <ThemeToggle />
          </div>
        </div>
      </header>
      <main className="p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6 max-w-7xl mx-auto">
          {Array.from({ length: zoneCount }, (_, i) => i + 1).map((zoneIndex) => (
            <div
              key={zoneIndex}
              onDrop={(e) => {
                e.preventDefault();
                handleZoneDrop(zoneIndex);
              }}
              onDragOver={(e) => e.preventDefault()}
            >
              <ZoneCard 
                zoneIndex={zoneIndex} 
                draggingClientIndex={draggingClientIndex}
                onClientDragStart={handleClientDragStart}
                onClientDragEnd={handleClientDragEnd}
              />
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}

export default App;
