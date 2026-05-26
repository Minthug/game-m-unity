import { useEffect, useRef, useState } from 'react';

type SendFn = (obj: string, method: string, param: string) => void;

interface Props {
  basePath?: string;
  loaderFile?: string;
  fileBasename?: string;
  onProgress?: (p: number) => void;
  onLoaded?: () => void;
  onError?: (e: Error) => void;
  onReady?: (send: SendFn) => void;
  onRequestInput?: (placeholder: string) => void; // Unity가 채팅창 열기 요청
}

export default function UnityCanvas({
  basePath = '/unity/Build',
  loaderFile = 'unity.loader.js',
  fileBasename = 'unity',
  onProgress,
  onLoaded,
  onError,
  onReady,
  onRequestInput,
}: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const unityInstanceRef = useRef<any>(null);
  const [progress, setProgress] = useState(0);
  const [loaded, setLoaded] = useState(false);

  // Unity → React 메시지 수신
  useEffect(() => {
    (window as any).onUnityMessage = (json: string) => {
      let msg: any;
      try { msg = JSON.parse(json); } catch { return; }

      switch (msg.type) {
        case 'storage_set':
          try { localStorage.setItem(msg.key, msg.value); } catch {}
          break;

        case 'storage_get': {
          const value = localStorage.getItem(msg.key) ?? '';
          // Unity가 콜백을 기다리는 경우 SendMessage로 돌려줌
          unityInstanceRef.current?.SendMessage(
            'StorageCallback', 'OnStorageResult',
            JSON.stringify({ key: msg.key, value })
          );
          break;
        }

        case 'haptic':
          if ('vibrate' in navigator) navigator.vibrate(msg.payload === 'heavy' ? 30 : 10);
          break;

        case 'request_text_input':
          onRequestInput?.(msg.placeholder ?? '');
          break;

        default:
          console.log('[Unity→Web]', msg);
      }
    };

    return () => { (window as any).onUnityMessage = null; };
  }, [onRequestInput]);

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
        onError?.(new Error('createUnityInstance not found'));
        return;
      }

      (window as any)
        .createUnityInstance(canvas, {
          dataUrl:            `${basePath}/${fileBasename}.data.br`,
          frameworkUrl:       `${basePath}/${fileBasename}.framework.js.br`,
          codeUrl:            `${basePath}/${fileBasename}.wasm.br`,
          streamingAssetsUrl: 'StreamingAssets',
          companyName:        'Minthug',
          productName:        'game-m',
          productVersion:     '1.0',
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
          onReady?.((obj, method, param) => instance.SendMessage(obj, method, param));
        })
        .catch((e: any) => {
          if (mounted) setLoaded(true);
          onError?.(e instanceof Error ? e : new Error(String(e)));
        });
    };

    script.onerror = () => {
      if (mounted) setLoaded(true);
      onError?.(new Error('Unity 빌드 파일 없음 — UI 테스트 모드'));
    };

    document.body.appendChild(script);

    return () => {
      mounted = false;
      unityInstanceRef.current?.Quit?.().catch(() => {});
      try { document.body.removeChild(script); } catch {}
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
          <div style={{ width: 200, height: 4, borderRadius: 2, background: 'rgba(255,255,255,0.1)' }}>
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
