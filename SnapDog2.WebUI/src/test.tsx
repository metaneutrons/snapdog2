import React from 'react';
import ReactDOM from 'react-dom/client';

function TestApp() {
  return <div>TEST REACT WORKING</div>;
}

const rootElement = document.getElementById('root');
if (rootElement) {
  const root = ReactDOM.createRoot(rootElement);
  root.render(<TestApp />);
  console.log('Test React mounted successfully');
} else {
  console.error('Root element not found');
}
