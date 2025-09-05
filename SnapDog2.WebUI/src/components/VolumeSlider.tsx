
import React, { useCallback, useState, useEffect } from 'react';
import { VolumeXIcon, Volume2Icon } from './icons';

interface VolumeSliderProps {
  value: number;
  muted: boolean;
  onChange: (value: number) => void;
  onMuteToggle: () => void;
  size?: 'sm' | 'md';
  className?: string;
}

export function VolumeSlider({
  value,
  muted,
  onChange,
  onMuteToggle,
  size = 'md',
  className = ''
}: VolumeSliderProps) {
  
  const [localValue, setLocalValue] = useState(value);

  // Sync local value with prop value
  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  // Debounce API calls
  useEffect(() => {
    const timer = setTimeout(() => {
      if (localValue !== value) {
        onChange(localValue);
      }
    }, 150); // 150ms debounce

    return () => clearTimeout(timer);
  }, [localValue, onChange, value]);
  
  const handleSliderChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setLocalValue(parseInt(e.target.value, 10));
  }, []);

  return (
    <div className={`flex items-center space-x-2 ${className}`}>
      <button
        onClick={onMuteToggle}
        className="p-1 rounded-full text-gray-500 hover:bg-gray-100 hover:text-gray-800 transition-colors"
        aria-label={muted ? 'Unmute' : 'Mute'}
      >
        {muted ? (
          <VolumeXIcon size={size === 'sm' ? 18 : 22} className="text-red-500" />
        ) : (
          <Volume2Icon size={size === 'sm' ? 18 : 22} />
        )}
      </button>

      <input
        type="range"
        min="0"
        max="100"
        value={muted ? 0 : localValue}
        onChange={handleSliderChange}
        disabled={muted}
        className={`w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-blue-600 dark:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed`}
        aria-label="Volume"
      />
    </div>
  );
}
