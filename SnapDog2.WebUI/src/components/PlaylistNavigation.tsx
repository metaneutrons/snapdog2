import React, { useState, useEffect } from 'react';
import { ChevronLeftIcon, ChevronRightIcon, ListIcon, Loader2Icon } from './icons';
import { useAppStore } from '../store';
import { playlistApi } from '../services/playlistApi';
import type { PlaylistInfo } from '../types';

interface PlaylistNavigationProps {
  zoneIndex: number;
  currentPlaylist?: PlaylistInfo | null;
  onPlaylistChange?: (playlist: PlaylistInfo) => void;
  onShowTrackList?: () => void;
  isChangingPlaylist?: boolean;
}

export const PlaylistNavigation: React.FC<PlaylistNavigationProps> = ({
  zoneIndex,
  currentPlaylist,
  onPlaylistChange,
  onShowTrackList,
  isChangingPlaylist = false
}) => {
  const { playlists, setPlaylists } = useAppStore();
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const loadPlaylists = async () => {
      if (playlists.length === 0) {
        setIsLoading(true);
        try {
          const playlistData = await playlistApi.getPlaylists();
          setPlaylists(playlistData);
        } catch (error) {
          console.error('Failed to load playlists:', error);
        } finally {
          setIsLoading(false);
        }
      }
    };

    loadPlaylists();
  }, [playlists.length, setPlaylists]);

  const currentIndex = currentPlaylist ? playlists.findIndex(p => p.index === currentPlaylist.index) : -1;
  const canGoPrevious = currentIndex > 0;
  const canGoNext = currentIndex < playlists.length - 1;

  const handlePreviousPlaylist = async () => {
    if (!canGoPrevious || isChangingPlaylist) return;
    
    const previousPlaylist = playlists[currentIndex - 1];
    if (previousPlaylist && onPlaylistChange) {
      onPlaylistChange(previousPlaylist);
    }
  };

  const handleNextPlaylist = async () => {
    if (!canGoNext || isChangingPlaylist) return;
    
    const nextPlaylist = playlists[currentIndex + 1];
    if (nextPlaylist && onPlaylistChange) {
      onPlaylistChange(nextPlaylist);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Loader2Icon className="w-4 h-4 animate-spin" />
        Loading playlists...
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2">
      {/* Playlist Navigation */}
      <button
        onClick={handlePreviousPlaylist}
        disabled={!canGoPrevious || isChangingPlaylist}
        className="p-1 rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
        title="Previous Playlist"
      >
        <ChevronLeftIcon className="w-4 h-4" />
      </button>

      {/* Current Playlist Display */}
      <div className="flex items-center gap-1 min-w-0">
        <span className="text-sm text-gray-600">📻</span>
        <span className="text-sm font-medium truncate">
          {currentPlaylist?.name || 'No Playlist'}
        </span>
        {isChangingPlaylist && (
          <Loader2Icon className="w-3 h-3 animate-spin text-blue-500" />
        )}
      </div>

      <button
        onClick={handleNextPlaylist}
        disabled={!canGoNext || isChangingPlaylist}
        className="p-1 rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
        title="Next Playlist"
      >
        <ChevronRightIcon className="w-4 h-4" />
      </button>

      {/* Track List Button */}
      <button
        onClick={onShowTrackList}
        disabled={!currentPlaylist || isChangingPlaylist}
        className="p-1 rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed ml-2"
        title="Show Track List"
      >
        <ListIcon className="w-4 h-4" />
      </button>
    </div>
  );
};
