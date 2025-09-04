import React from 'react';
import { useTheme } from '../hooks/useTheme';

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  const themes = [
    { 
      value: 'light' as const, 
      icon: (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="12" cy="12" r="5"/>
          <path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>
        </svg>
      )
    },
    { 
      value: 'dark' as const, 
      icon: (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
        </svg>
      )
    },
    { 
      value: 'system' as const, 
      icon: (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
          <line x1="8" y1="21" x2="16" y2="21"/>
          <line x1="12" y1="17" x2="12" y2="21"/>
        </svg>
      )
    },
  ];

  return (
    <div className="flex bg-theme-tertiary rounded-full p-1 shadow-theme">
      {themes.map((themeOption) => (
        <button
          key={themeOption.value}
          onClick={() => setTheme(themeOption.value)}
          className={`
            w-10 h-10 rounded-full transition-all duration-300 ease-out
            flex items-center justify-center
            ${theme === themeOption.value
              ? 'bg-theme-secondary text-theme-primary shadow-theme transform scale-105'
              : 'text-theme-secondary hover:text-theme-primary hover:bg-theme-secondary/50'
            }
          `}
          title={`${themeOption.value} mode`}
        >
          {themeOption.icon}
        </button>
      ))}
    </div>
  );
}
