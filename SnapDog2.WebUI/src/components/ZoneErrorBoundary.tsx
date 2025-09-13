import React from 'react';
import { ErrorBoundary } from './ErrorBoundary';

interface ZoneErrorBoundaryProps {
  zoneIndex: number;
  children: React.ReactNode;
}

export function ZoneErrorBoundary({ zoneIndex, children }: ZoneErrorBoundaryProps) {
  const fallback = (
    <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-6">
      <div className="flex items-center space-x-3">
        <div className="text-red-500 text-2xl">⚠️</div>
        <div>
          <h3 className="text-lg font-medium text-red-800 dark:text-red-200">
            Zone {zoneIndex} Error
          </h3>
          <p className="text-red-600 dark:text-red-300 text-sm">
            This zone encountered an error and couldn't load properly.
          </p>
        </div>
      </div>
    </div>
  );

  return (
    <ErrorBoundary fallback={fallback}>
      {children}
    </ErrorBoundary>
  );
}
