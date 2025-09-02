
export interface TrackInfo {
  index?: number;
  title: string;
  artist: string;
  album?: string;
  durationMs?: number;
  positionMs?: number;
  progress?: number;
  coverArtUrl?: string;
  isPlaying: boolean;
  source: string;
  url: string;
}

export interface PlaylistInfo {
  index?: number;
  name: string;
  trackCount: number;
  source: string;
  coverArtUrl?: string;
}

export type PlaybackState = 'playing' | 'paused' | 'stopped';

export interface ZoneState {
  name: string;
  volume: number;
  muted: boolean;
  playlistShuffle: boolean;
  trackRepeat: boolean;
  playlistRepeat: boolean;
  playbackState: PlaybackState;
  track?: TrackInfo;
  progress?: { position: number; progress: number };
  playlist?: PlaylistInfo;
  clients: number[];
}

export interface ClientState {
  name: string;
  connected: boolean;
  zoneIndex?: number;
  volume: number;
  muted: boolean;
  latency?: number;
}