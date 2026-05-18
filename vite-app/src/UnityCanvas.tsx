import { useEffect, useRef, useState } from 'react';

interface Props {
  basePath?: string;
  loaderFile?: string;
  fileBasename?: string;
  onProgress?: (p: number) => void;
  onLoaded?: () => void;
  onError?: (e: Error) => void;
}

export default function UnityCanvas({
  basePath = '/unity',
  loaderFile = 'game-m-unity.loader.js',
  fileBasename = 'game-m-unity',
  onProgress,
  onLoaded,
  onError,
}: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const unityInstanceRef = useRef<any>(null);
  const [progress, setProgress] = useState(0);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let mounted = true;
    const script = document.createElement('script');
    script.src = `${basePath}/${loaderFile}`;
    script.async = true;

    script.onload = () => {
      if (!mounted || !containerRef.current) return;

      const canvas = document.createElement('canvas');
      canvas.id = 'unity-canvas';
      canvas.style.cssText = 'width:100%;height:100%;display:block;';
      containerRef.current.appendChild(canvas);

      if (typeof (window as any).createUnityInstance !== 'function') {
        const err = new Error('createUnityInstance not found');
        onError?.(err);
        return;
      }

      (window as any)
        .createUnityInstance(canvas, {
          dataUrl: `${basePath}/${fileBasename}.data.br`,
          frameworkUrl: `${basePath}/${fileBasename}.framework.js.br`,
          codeUrl: `${basePath}/${fileBasename}.wasm.br`,
          streamingAssetsUrl: 'StreamingAssets',
          companyName: 'Minthug',
          productName: 'game-m',
          productVersion: '1.0',
        }, (p: number) => {
          if (!mounted) return;
          setProgress(Math.round(p * 100));
          onProgress?.(p);
        })
        .then((instance: any) => {
          if (!mounted) { instance?.Quit?.(); return; }
          unityInstanceRef.current = instance;
          setLoaded(true);
          onLoaded?.();
        })
        .catch((e: any) => {
          onError?.(e instanceof Error ? e : new Error(String(e)));
        });
    };

    script.onerror = () => onError?.(new Error('Failed to load Unity loader'));
    document.body.appendChild(script);

    return () => {
      mounted = false;
      unityInstanceRef.current?.Quit?.().catch(() => {});
      try { document.body.removeChild(script); } catch {}
    };
  }, []);

  // Unity → React 메시지 수신 진입점
  useEffect(() => {
    (window as any).onUnityMessage = (json: string) => {
      try {
        const msg = JSON.parse(json);
        console.log('[Unity→Web]', msg);
        // TODO: msg.type 에 따라 Toss SDK 호출 (Storage, haptic 등)
      } catch {}
    };
  }, []);

  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative', background: '#161618' }}>
      {!loaded && (
        <div style={{
          position: 'absolute', inset: 0,
          display: 'flex', flexDirection: 'column',
          alignItems: 'center', justifyContent: 'center',
          color: 'rgba(220,220,240,0.7)', gap: 16,
        }}>
          <div style={{ fontSize: 18, fontWeight: 600 }}>들어줄게</div>
          <div style={{
            width: 200, height: 4, borderRadius: 2,
            background: 'rgba(255,255,255,0.1)',
          }}>
            <div style={{
              width: `${progress}%`, height: '100%', borderRadius: 2,
              background: 'rgba(155,135,255,0.8)',
              transition: 'width 0.3s ease',
            }} />
          </div>
          <div style={{ fontSize: 13, opacity: 0.5 }}>{progress}%</div>
        </div>
      )}
      <div ref={containerRef} style={{ width: '100%', height: '100%' }} />
    </div>
  );
}
