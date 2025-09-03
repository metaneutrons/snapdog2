import React from 'react';
import type { PlaylistInfo } from '../types';

interface PlaylistNavigationProps {
  zoneIndex: number;
  currentPlaylist?: PlaylistInfo | null;
  onPlaylistChange?: (playlist: PlaylistInfo) => void;
  onShowTrackList?: () => void;
  isChangingPlaylist?: boolean;
}

export const PlaylistNavigation: React.FC<PlaylistNavigationProps> = ({
  currentPlaylist,
  onShowTrackList,
  isChangingPlaylist = false
}) => {
  return (
    <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border">
      <div className="flex items-center space-x-2">
        <button 
          className="px-2 py-1 text-gray-400 hover:text-gray-600 disabled:opacity-50 text-sm"
          disabled={true}
          title="Previous Playlist"
        >
          ◀
        </button>
        
        <div className="text-sm">
          <div className="font-medium text-gray-800">
            {currentPlaylist?.name || 'No Playlist'}
          </div>
          {currentPlaylist && (
            <div className="text-xs text-gray-500">
              {currentPlaylist.trackCount || 0} tracks
            </div>
          )}
        </div>
        
        <button 
          className="px-2 py-1 text-gray-400 hover:text-gray-600 disabled:opacity-50 text-sm"
          disabled={true}
          title="Next Playlist"
        >
          ▶
        </button>
      </div>
      
      <div className="flex items-center space-x-2">
        {isChangingPlaylist && (
          <div className="text-xs text-blue-600">Changing...</div>
        )}
        
        <button
          onClick={onShowTrackList}
          className="px-2 py-1 text-xs bg-blue-100 text-blue-700 rounded hover:bg-blue-200 disabled:opacity-50"
          disabled={!currentPlaylist}
          title="Show Track List"
        >
          📋 Tracks
        </button>
      </div>
    </div>
  );
};
