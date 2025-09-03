import React, { useState, useEffect } from 'react';
import { XIcon, PlayIcon, Loader2Icon, RefreshCwIcon } from './icons';
import { useAppStore } from '../store';
import { playlistApi } from '../services/playlistApi';
import type { PlaylistInfo, TrackInfo } from '../types';

interface TrackListProps {
  zoneIndex: number;
  playlist: PlaylistInfo;
  currentTrack?: TrackInfo | null;
  onClose: () => void;
  onTrackSelect: (track: TrackInfo) => void;
  isChangingTrack?: boolean;
}

export const TrackList: React.FC<TrackListProps> = ({
  zoneIndex,
  playlist,
  currentTrack,
  onClose,
  onTrackSelect,
  isChangingTrack = false
}) => {
  const { playlistTracks, setPlaylistTracks } = useAppStore();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const tracks = playlistTracks[playlist.index] || [];

  const loadTracks = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const trackData = await playlistApi.getPlaylistTracks(playlist.index);
      setPlaylistTracks(playlist.index, trackData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load tracks');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (tracks.length === 0) {
      loadTracks();
    }
  }, [playlist.index]);

  const formatDuration = (durationMs?: number | null) => {
    if (!durationMs) return '';
    const minutes = Math.floor(durationMs / 60000);
    const seconds = Math.floor((durationMs % 60000) / 1000);
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[80vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b">
          <div className="flex items-center gap-2">
            <span className="text-lg font-semibold">🎵 Track List - {playlist.name}</span>
            {isLoading && <Loader2Icon className="w-4 h-4 animate-spin" />}
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={loadTracks}
              disabled={isLoading}
              className="p-1 rounded hover:bg-gray-100 disabled:opacity-50"
              title="Refresh"
            >
              <RefreshCwIcon className="w-4 h-4" />
            </button>
            <button
              onClick={onClose}
              className="p-1 rounded hover:bg-gray-100"
              title="Close"
            >
              <XIcon className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-hidden">
          {error ? (
            <div className="p-4 text-center">
              <div className="text-red-600 mb-2">⚠️ {error}</div>
              <button
                onClick={loadTracks}
                className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
              >
                Retry
              </button>
            </div>
          ) : isLoading ? (
            <div className="p-8 text-center">
              <Loader2Icon className="w-8 h-8 animate-spin mx-auto mb-2" />
              <div className="text-gray-600">Loading tracks...</div>
            </div>
          ) : (
            <div className="overflow-y-auto max-h-full">
              {tracks.map((track) => {
                const isCurrentTrack = currentTrack?.index === track.index;
                const isChangingThis = isChangingTrack && isCurrentTrack;
                
                return (
                  <div
                    key={track.index}
                    className={`flex items-center gap-3 p-3 border-b hover:bg-gray-50 cursor-pointer ${
                      isCurrentTrack ? 'bg-blue-50 border-blue-200' : ''
                    }`}
                    onClick={() => !isChangingThis && onTrackSelect(track)}
                  >
                    {/* Track Number / Status */}
                    <div className="w-8 text-center">
                      {isCurrentTrack ? (
                        isChangingThis ? (
                          <Loader2Icon className="w-4 h-4 animate-spin text-blue-500" />
                        ) : (
                          <span className="text-blue-600 font-bold">●</span>
                        )
                      ) : (
                        <span className="text-gray-500">{track.index}</span>
                      )}
                    </div>

                    {/* Track Info */}
                    <div className="flex-1 min-w-0">
                      <div className="font-medium truncate">{track.title}</div>
                      {track.artist && (
                        <div className="text-sm text-gray-600 truncate">{track.artist}</div>
                      )}
                    </div>

                    {/* Duration */}
                    <div className="text-sm text-gray-500 w-16 text-right">
                      {formatDuration(track.durationMs)}
                    </div>

                    {/* Cover Art */}
                    {track.coverArtUrl && (
                      <div className="w-8 h-8 rounded overflow-hidden bg-gray-200">
                        <img
                          src={track.coverArtUrl}
                          alt="Cover"
                          className="w-full h-full object-cover"
                          onError={(e) => {
                            e.currentTarget.style.display = 'none';
                          }}
                        />
                      </div>
                    )}

                    {/* Play Button */}
                    {!isCurrentTrack && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          onTrackSelect(track);
                        }}
                        className="p-1 rounded hover:bg-gray-200 opacity-0 group-hover:opacity-100 transition-opacity"
                        title="Play Track"
                      >
                        <PlayIcon className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-3 border-t bg-gray-50 text-sm text-gray-600 text-center">
          {tracks.length} tracks • {playlist.source}
        </div>
      </div>
    </div>
  );
};
