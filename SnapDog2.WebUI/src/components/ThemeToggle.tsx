import React from 'react';
import { useTheme } from '../hooks/useTheme';

export function ThemeToggle() {
  const { theme, setTheme, resolvedTheme } = useTheme();

  const themes = [
    { value: 'light' as const, label: '☀️ Light', icon: '☀️' },
    { value: 'dark' as const, label: '🌙 Dark', icon: '🌙' },
    { value: 'system' as const, label: '💻 System', icon: '💻' },
  ];

  return (
    <div className="flex items-center gap-2">
      <span className="text-theme-secondary text-sm font-medium">Theme:</span>
      <div className="flex rounded-lg border-theme-primary border overflow-hidden">
        {themes.map((themeOption) => (
          <button
            key={themeOption.value}
            onClick={() => setTheme(themeOption.value)}
            className={`
              px-3 py-1.5 text-sm font-medium transition-all duration-200
              ${theme === themeOption.value
                ? 'bg-blue-500 text-white'
                : 'theme-toggle hover:bg-theme-tertiary'
              }
            `}
            title={themeOption.label}
          >
            <span className="flex items-center gap-1">
              <span>{themeOption.icon}</span>
              <span className="hidden sm:inline">{themeOption.value}</span>
            </span>
          </button>
        ))}
      </div>
      <span className="text-theme-tertiary text-xs">
        ({resolvedTheme === 'dark' ? '🌙' : '☀️'})
      </span>
    </div>
  );
}
