import UnityCanvas from './UnityCanvas';

function App() {
  return (
    <UnityCanvas
      onProgress={(p) => console.log(`Loading: ${Math.round(p * 100)}%`)}
      onLoaded={() => console.log('Unity ready')}
      onError={(e) => console.error('Unity error:', e)}
    />
  );
}

export default App;
