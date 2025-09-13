// Global error handling for unhandled promise rejections and errors
export function setupGlobalErrorHandlers() {
  // Handle unhandled promise rejections
  window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
    
    // Prevent the default browser behavior (logging to console)
    event.preventDefault();
    
    // You could send to error reporting service here
    // errorReportingService.report(event.reason);
  });

  // Handle uncaught errors
  window.addEventListener('error', (event) => {
    console.error('Uncaught error:', event.error);
    
    // You could send to error reporting service here
    // errorReportingService.report(event.error);
  });

  console.log('🛡️ Global error handlers initialized');
}
