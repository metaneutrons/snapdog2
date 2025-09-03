import React from 'react';
import { useZone, useAppStore } from '../store';
import { PlayIcon, PauseIcon, SkipBackIcon, SkipForwardIcon, ShuffleIcon, RepeatIcon, Repeat1Icon } from './icons';

interface TransportControlsProps {
  zoneIndex: number;
}

const ControlButton: React.FC<{
    onClick: () => void;
    'aria-label': string;
    isActive?: boolean;
    children: React.ReactNode;
    className?: string;
}> = ({ onClick, children, isActive, className = '', ...props }) => (
  <button
    onClick={onClick}
    className={`p-2 rounded-full transition-colors duration-200 ${isActive ? 'bg-blue-100 text-blue-700' : 'text-gray-500 hover:bg-gray-200 hover:text-gray-800'} ${className}`}
    {...props}
  >
    {children}
  </button>
);

export function TransportControls({ zoneIndex }: TransportControlsProps) {
  const zone = useZone(zoneIndex);
  const { playZone, pauseZone, nextTrack, prevTrack, toggleShuffle, toggleRepeat } = useAppStore();

  if (!zone) return null;

  const { playbackState, playlistShuffle, trackRepeat, playlistRepeat } = zone;
  const isPlaying = playbackState === 'playing';

  const handlePlayPause = () => {
    if (isPlaying) {
      pauseZone(zoneIndex).catch(console.error);
    } else {
      playZone(zoneIndex).catch(console.error);
    }
  };

  const handleNext = () => nextTrack(zoneIndex).catch(console.error);
  const handlePrevious = () => prevTrack(zoneIndex).catch(console.error);
  const handleToggleShuffle = () => toggleShuffle(zoneIndex).catch(console.error);
  const handleToggleRepeat = () => toggleRepeat(zoneIndex).catch(console.error);
  const handleToggleTrackRepeat = () => toggleRepeat(zoneIndex).catch(console.error);

  return (
    <div className="flex items-center justify-between">
      <div className="flex items-center space-x-2">
        <ControlButton onClick={handleToggleShuffle} aria-label={playlistShuffle ? 'Disable Shuffle' : 'Enable Shuffle'} isActive={playlistShuffle}>
          <ShuffleIcon size={18} />
        </ControlButton>
        <ControlButton onClick={trackRepeat ? handleToggleRepeat : handleToggleTrackRepeat} aria-label="Toggle Repeat" isActive={trackRepeat || playlistRepeat}>
          {trackRepeat ? <Repeat1Icon size={18} /> : <RepeatIcon size={18} />}
        </ControlButton>
      </div>

      <div className="flex items-center space-x-2">
        <ControlButton onClick={handlePrevious} aria-label="Previous Track">
          <SkipBackIcon size={20} />
        </ControlButton>
        <button
            onClick={handlePlayPause}
            className="p-3 bg-blue-600 text-white rounded-full hover:bg-blue-700 transition-colors shadow-md"
            aria-label={isPlaying ? 'Pause' : 'Play'}
        >
            {isPlaying ? <PauseIcon size={24} /> : <PlayIcon size={24} />}
        </button>
        <ControlButton onClick={handleNext} aria-label="Next Track">
          <SkipForwardIcon size={20} />
        </ControlButton>
      </div>
      
      <div className="w-16"></div>
    </div>
  );
}