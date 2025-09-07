import React, { useState, useEffect } from 'react';
import { ChevronDownIcon, MusicIcon, ChevronLeftIcon, ChevronRightIcon } from './icons';
import { useAppStore } from '../store';
import { playlistApi } from '../services/playlistApi';
import type { PlaylistInfo } from '../types';

interface PlaylistSelectorProps {
  zoneIndex: number;
  currentPlaylistIndex?: number;
  currentPlaylistName?: string;
}

export const PlaylistSelector: React.FC<PlaylistSelectorProps> = ({
  zoneIndex,
  currentPlaylistIndex,
  currentPlaylistName
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const { playlists, setPlaylists } = useAppStore();

  useEffect(() => {
    const loadPlaylists = async () => {
      if (playlists.length === 0) {
        setLoading(true);
        try {
          const playlistData = await playlistApi.getPlaylists();
          setPlaylists(playlistData);
        } catch (error) {
          console.error('Failed to fetch playlists:', error);
        } finally {
          setLoading(false);
        }
      }
    };
    loadPlaylists();
  }, [playlists.length, setPlaylists]);

  const handlePlaylistSelect = async (playlist: PlaylistInfo) => {
    if (!playlist.index) return;
    try {
      await playlistApi.setZonePlaylist(zoneIndex, playlist.index);
      setIsOpen(false);
    } catch (error) {
      console.error('Failed to set playlist:', error);
    }
  };

  const handlePreviousPlaylist = async () => {
    try {
      const response = await fetch(`/api/v1/zones/${zoneIndex}/previous/playlist`, {
        method: 'POST',
        headers: { 'X-API-Key': 'test-api-key' }
      });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
    } catch (error) {
      console.error('Failed to go to previous playlist:', error);
    }
  };

  const handleNextPlaylist = async () => {
    try {
      const response = await fetch(`/api/v1/zones/${zoneIndex}/next/playlist`, {
        method: 'POST',
        headers: { 'X-API-Key': 'test-api-key' }
      });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
    } catch (error) {
      console.error('Failed to go to next playlist:', error);
    }
  };

  return (
    <div className="flex items-center space-x-1 mb-3">
      <button
        onClick={handlePreviousPlaylist}
        className="p-1 bg-theme-secondary border border-theme-primary rounded hover:bg-theme-accent transition-colors"
        title="Previous playlist"
      >
        <ChevronLeftIcon className="w-3 h-3 text-theme-primary" />
      </button>

      <div className="flex-1 relative">
        <button
          onClick={() => setIsOpen(!isOpen)}
          className="w-full flex items-center justify-between p-2 bg-theme-secondary border border-theme-primary rounded hover:bg-theme-accent transition-colors"
          disabled={loading}
        >
          <div className="flex items-center space-x-1">
            <MusicIcon className="w-3 h-3 text-theme-primary" />
            <span className="text-xs font-medium text-theme-primary truncate">
              {currentPlaylistName || 'Select Playlist'}
            </span>
          </div>
          <ChevronDownIcon 
            className={`w-3 h-3 text-theme-primary transition-transform ${isOpen ? 'rotate-180' : ''}`} 
          />
        </button>

        {isOpen && (
          <div className="absolute top-full left-0 right-0 mt-1 bg-theme-secondary border border-theme-primary rounded shadow-theme z-10 max-h-40 overflow-y-auto">
            {loading ? (
              <div className="p-2 text-center text-theme-secondary text-xs">Loading...</div>
            ) : playlists.length === 0 ? (
              <div className="p-2 text-center text-theme-secondary text-xs">No playlists</div>
            ) : (
              playlists.map((playlist) => (
                <button
                  key={playlist.index || playlist.name}
                  onClick={() => handlePlaylistSelect(playlist)}
                  className={`w-full text-left p-2 hover:bg-theme-accent transition-colors border-b border-theme-primary last:border-b-0 ${
                    playlist.index === currentPlaylistIndex ? 'bg-theme-accent' : ''
                  }`}
                >
                  <span className="text-xs font-medium text-theme-primary truncate block">{playlist.name}</span>
                </button>
              ))
            )}
          </div>
        )}
      </div>

      <button
        onClick={handleNextPlaylist}
        className="p-1 bg-theme-secondary border border-theme-primary rounded hover:bg-theme-accent transition-colors"
        title="Next playlist"
      >
        <ChevronRightIcon className="w-3 h-3 text-theme-primary" />
      </button>

      {isOpen && (
        <div 
          className="fixed inset-0 z-0" 
          onClick={() => setIsOpen(false)}
        />
      )}
    </div>
  );
};
