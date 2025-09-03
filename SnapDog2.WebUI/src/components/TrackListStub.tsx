import React, { useState, useEffect } from 'react';
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
  playlist,
  currentTrack,
  onClose,
  onTrackSelect,
  isChangingTrack = false
}) => {
  const [tracks, setTracks] = useState<TrackInfo[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadTracks = async () => {
      try {
        setLoading(true);
        console.log('🔍 Loading tracks for playlist:', playlist.index);
        const tracks = await playlistApi.getPlaylistTracks(playlist.index);
        console.log('✅ Tracks loaded:', tracks.length);
        setTracks(tracks);
      } catch (error) {
        console.error('❌ Failed to load tracks:', error);
      } finally {
        setLoading(false);
      }
    };
    loadTracks();
  }, [playlist.index]);

  const handleTrackClick = (track: TrackInfo) => {
    console.log('🎵 Selecting track:', track.title);
    onTrackSelect(track);
  };

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
          
          {loading ? (
            <div className="text-center text-gray-500 py-8">
              Loading tracks...
            </div>
          ) : (
            <div className="space-y-2">
              <div className="text-sm text-gray-500 mb-2">
                {tracks.length} tracks
              </div>
              
              {tracks.map((track, index) => {
                const isCurrentTrack = currentTrack && track.index === currentTrack.index;
                return (
                  <div
                    key={track.index}
                    onClick={() => handleTrackClick(track)}
                    className={`p-2 rounded cursor-pointer hover:bg-gray-100 ${
                      isCurrentTrack ? 'bg-blue-50 border-l-4 border-blue-500' : ''
                    }`}
                  >
                    <div className="flex items-center space-x-2">
                      <span className="text-xs text-gray-400 w-6">{index + 1}</span>
                      <div className="flex-1">
                        <div className="text-sm font-medium">
                          {track.title} {isCurrentTrack ? '● Playing' : ''}
                        </div>
                        <div className="text-xs text-gray-500">
                          {track.artist} • {track.album}
                        </div>
                      </div>
                      <span className="text-xs text-gray-400">
                        {track.durationMs ? Math.floor(track.durationMs / 60000) + ':' + 
                         String(Math.floor((track.durationMs % 60000) / 1000)).padStart(2, '0') : '0:00'}
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
