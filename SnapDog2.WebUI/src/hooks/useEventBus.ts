import { useEffect } from 'react';
import { eventBus } from '../services/eventBus';

type EventCallback<T = any> = (data: T) => void;

interface AppEvents {
  'client.move': { clientIndex: number; targetZoneIndex: number };
  'zone.volume.change': { zoneIndex: number; volume: number };
  'zone.mute.toggle': { zoneIndex: number };
  'client.drag.start': { clientIndex: number };
  'client.drag.end': {};
}

export function useEventBus() {
  const on = <K extends keyof AppEvents>(
    event: K, 
    callback: EventCallback<AppEvents[K]>
  ) => {
    useEffect(() => {
      eventBus.on(event, callback);
      return () => eventBus.off(event, callback);
    }, [event, callback]);
  };

  const emit = <K extends keyof AppEvents>(event: K, data: AppEvents[K]) => {
    eventBus.emit(event, data);
  };

  return { on, emit };
}
