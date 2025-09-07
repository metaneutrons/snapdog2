import React, { useState, useEffect } from 'react';
import { ChevronDownIcon, MusicIcon } from './icons';
import { useAppStore } from '../store';
import { playlistApi } from '../services/playlistApi';
import type { PlaylistInfo } from '../types';

interface PlaylistSelectorProps {
  zoneIndex: number;
  currentPlaylistIndex?: number;
  currentPlaylistName?: string;
  className?: string;
}

export const PlaylistSelector: React.FC<PlaylistSelectorProps> = ({
  zoneIndex,
  currentPlaylistIndex,
  currentPlaylistName,
  className = ''
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

  return (
    <div className={`relative ${className}`}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="w-full flex items-center justify-between p-3 bg-theme-secondary border border-theme-primary rounded-lg hover:bg-theme-accent transition-colors"
        disabled={loading}
      >
        <div className="flex items-center space-x-2">
          <MusicIcon className="w-4 h-4 text-theme-primary" />
          <span className="text-sm font-medium text-theme-primary">
            {currentPlaylistName || 'Select Playlist'}
          </span>
        </div>
        <ChevronDownIcon 
          className={`w-4 h-4 text-theme-primary transition-transform ${isOpen ? 'rotate-180' : ''}`} 
        />
      </button>

      {isOpen && (
        <div className="absolute top-full left-0 right-0 mt-1 bg-theme-secondary border border-theme-primary rounded-lg shadow-theme z-10 max-h-60 overflow-y-auto">
          {loading ? (
            <div className="p-3 text-center text-theme-secondary">Loading...</div>
          ) : playlists.length === 0 ? (
            <div className="p-3 text-center text-theme-secondary">No playlists available</div>
          ) : (
            playlists.map((playlist) => (
              <button
                key={playlist.index || playlist.name}
                onClick={() => handlePlaylistSelect(playlist)}
                className={`w-full text-left p-3 hover:bg-theme-accent transition-colors border-b border-theme-primary last:border-b-0 ${
                  playlist.index === currentPlaylistIndex ? 'bg-theme-accent text-theme-primary' : 'text-theme-primary'
                }`}
              >
                <div className="flex justify-between items-center">
                  <span className="font-medium">{playlist.name}</span>
                  {playlist.trackCount && (
                    <span className="text-xs text-theme-secondary">{playlist.trackCount} tracks</span>
                  )}
                </div>
              </button>
            ))
          )}
        </div>
      )}

      {isOpen && (
        <div 
          className="fixed inset-0 z-0" 
          onClick={() => setIsOpen(false)}
        />
      )}
    </div>
  );
};
