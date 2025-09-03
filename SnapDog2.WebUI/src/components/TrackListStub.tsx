import React from 'react';
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
  playlist,
  currentTrack,
  onClose,
  isChangingTrack = false
}) => {
  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 max-h-96">
        <div className="flex items-center justify-between p-4 border-b">
          <h3 className="text-lg font-semibold">
            🎵 {playlist.name}
          </h3>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
          >
            ✕
          </button>
        </div>
        
        <div className="p-4 overflow-y-auto max-h-64">
          {isChangingTrack && (
            <div className="text-center text-blue-600 mb-4">
              🔄 Changing track...
            </div>
          )}
          
          <div className="space-y-2">
            <div className="text-sm text-gray-500 mb-2">
              {playlist.trackCount || 0} tracks
            </div>
            
            {/* Mock tracks for now */}
            {[1, 2, 3, 4, 5].map((i) => (
              <div
                key={i}
                className={`p-2 rounded cursor-pointer hover:bg-gray-100 ${
                  i === 1 ? 'bg-blue-50 border-l-4 border-blue-500' : ''
                }`}
              >
                <div className="flex items-center space-x-2">
                  <span className="text-xs text-gray-400 w-6">{i}</span>
                  <div className="flex-1">
                    <div className="text-sm font-medium">
                      Track {i} {i === 1 ? '● Playing' : ''}
                    </div>
                    <div className="text-xs text-gray-500">
                      Artist {i} • Album {i}
                    </div>
                  </div>
                  <span className="text-xs text-gray-400">3:45</span>
                </div>
              </div>
            ))}
          </div>
        </div>
        
        <div className="p-4 border-t bg-gray-50 text-center text-xs text-gray-500">
          Track List - Coming Soon
        </div>
      </div>
    </div>
  );
};
