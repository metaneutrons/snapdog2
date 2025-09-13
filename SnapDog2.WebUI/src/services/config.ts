interface AppConfig {
  api: {
    baseUrl: string;
    key: string;
    timeout: number;
  };
  signalr: {
    hubUrl: string;
    reconnectDelay: number;
  };
  ui: {
    theme: 'light' | 'dark' | 'auto';
    refreshInterval: number;
  };
}

const validateConfig = (config: AppConfig): void => {
  if (!config.api.baseUrl) throw new Error('API base URL is required');
  if (!config.api.key) throw new Error('API key is required');
  if (config.api.timeout < 1000) throw new Error('API timeout must be at least 1000ms');
  if (!config.signalr.hubUrl) throw new Error('SignalR hub URL is required');
  if (!['light', 'dark', 'auto'].includes(config.ui.theme)) {
    throw new Error('UI theme must be light, dark, or auto');
  }
};

const config: AppConfig = {
  api: {
    baseUrl: import.meta.env.VITE_API_BASE_URL || '/api/v1',
    key: import.meta.env.VITE_API_KEY || 'dev-key',
    timeout: Number(import.meta.env.VITE_API_TIMEOUT) || 30000,
  },
  signalr: {
    hubUrl: import.meta.env.VITE_SIGNALR_HUB_URL || '/hubs/snapdog/v1',
    reconnectDelay: Number(import.meta.env.VITE_SIGNALR_RECONNECT_DELAY) || 5000,
  },
  ui: {
    theme: (import.meta.env.VITE_UI_THEME as 'light' | 'dark' | 'auto') || 'auto',
    refreshInterval: Number(import.meta.env.VITE_UI_REFRESH_INTERVAL) || 30000,
  },
};

// Validate configuration
try {
  validateConfig(config);
} catch (error) {
  console.error('❌ Configuration Error:', error.message);
  throw error;
}

// Debug configuration in development
if (import.meta.env.DEV) {
  console.log('🔧 App Configuration:', config);
}

export { config };
