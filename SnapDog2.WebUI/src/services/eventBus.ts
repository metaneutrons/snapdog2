type EventCallback<T = any> = (data: T) => void;

interface AppEvents {
  'client.move': { clientIndex: number; targetZoneIndex: number };
  'zone.volume.change': { zoneIndex: number; volume: number };
  'zone.mute.toggle': { zoneIndex: number };
  'client.drag.start': { clientIndex: number };
  'client.drag.end': {};
}

class EventBus {
  private listeners: Map<string, EventCallback[]> = new Map();

  on<K extends keyof AppEvents>(event: K, callback: EventCallback<AppEvents[K]>): void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, []);
    }
    this.listeners.get(event)!.push(callback);
  }

  off<K extends keyof AppEvents>(event: K, callback: EventCallback<AppEvents[K]>): void {
    const callbacks = this.listeners.get(event);
    if (callbacks) {
      const index = callbacks.indexOf(callback);
      if (index > -1) {
        callbacks.splice(index, 1);
      }
    }
  }

  emit<K extends keyof AppEvents>(event: K, data: AppEvents[K]): void {
    const callbacks = this.listeners.get(event);
    if (callbacks) {
      callbacks.forEach(callback => callback(data));
    }
  }
}

export const eventBus = new EventBus();
