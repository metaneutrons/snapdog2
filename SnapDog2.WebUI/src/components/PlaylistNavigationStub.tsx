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
  currentPlaylist
}) => {
  return (
    <div className="flex items-center justify-between p-2 bg-gray-100 rounded">
      <div className="text-sm text-gray-600">
        Playlist: {currentPlaylist?.name || 'None'}
      </div>
      <div className="text-xs text-gray-400">
        [Playlist Navigation - Coming Soon]
      </div>
    </div>
  );
};
