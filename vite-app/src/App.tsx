import { useState, useCallback, useRef, useEffect } from 'react';
import UnityCanvas from './UnityCanvas';

type SendFn = (obj: string, method: string, param: string) => void;
type Expression = 'angry' | 'sad' | 'fear' | 'happy' | 'disgust' | 'contempt' | 'surprised' | 'blank';

const KEYWORD_WEIGHTS: Record<Expression, Array<[string, number]>> = {
  angry: [
    // 강한 욕설
    ['씨발', 4], ['씨바', 4], ['시발', 4], ['시바', 4], ['썅', 4],
    ['개새', 4], ['병신', 4], ['느금', 4], ['보지', 4], ['자지', 4],
    ['니애미', 4], ['니에미', 4],
    // 중간 욕설/공격
    ['닥쳐', 3], ['꺼져', 3], ['죽어', 3], ['존나', 3],
    ['미친놈', 3], ['미친년', 3], ['개같', 3], ['개소리', 3],
    ['헛소리', 3], ['집어쳐', 3], ['꺼지라', 3],
    // 분노 표현
    ['빡', 2], ['열받', 2], ['짜증', 2], ['화나', 2], ['분노', 2],
    ['억울', 2], ['어이없', 2], ['황당', 2], ['더럽', 2], ['미치겠', 2],
    // 가벼운 불만
    ['최악', 1], ['싫어', 1], ['별로', 1], ['구려', 1], ['망했', 1],
    ['진짜로', 1], ['왜이래', 1], ['졌나', 1],
  ],
  sad: [
    // 강한 울음 표현
    ['흑흑', 3], ['훌쩍', 3], ['엉엉', 3], ['으앙', 3], ['으엉', 3],
    ['ㅠㅠ', 3], ['ㅜㅜ', 3], ['T_T', 3], ['TT', 3],
    // 슬픔 감정
    ['슬퍼', 2], ['눈물', 2], ['우울', 2], ['외로워', 2], ['무기력', 2],
    ['힘들', 2], ['힘들어', 2], ['보고싶', 2], ['그리워', 2], ['서러', 2], ['괴로', 2],
    // 가벼운 슬픔
    ['울고', 1], ['지쳐', 1], ['상처', 1], ['아파', 1], ['지침', 1],
    ['ㅠ-ㅜ', 1], ['ㅜ-ㅠ', 1], ['ㅠ.ㅠ', 1], ['ㅜ.ㅜ', 1], ['ㅠ', 1], ['ㅜ', 1],
  ],
  fear: [
    ['공황', 4], ['공포', 3], ['두려워', 3],
    ['무서워', 2], ['무섭다', 2], ['오싹', 2],
    ['겁나', 1], ['불안해', 1], ['떨려', 1], ['긴장돼', 1],
  ],
  disgust: [
    ['혐오', 4], ['역겨워', 3], ['구역질', 3], ['토나와', 3],
    ['징그러워', 2], ['더러워', 2],
    ['역해', 1], ['구리다', 1], ['냄새나', 1],
  ],
  contempt: [
    ['경멸', 4], ['하찮아', 3], ['무시해', 3], ['코웃음', 3],
    ['웃기지마', 2], ['찌질', 2],
    ['우습네', 1], ['웃기네', 1],
  ],
  surprised: [
    ['말도안돼', 3], ['믿을수없', 3], ['이게뭐', 3],
    ['충격', 2], ['대박', 2], ['설마', 2], ['당황', 2],
    ['헐', 1], ['뭐야', 1], ['놀랐', 1], ['진짜', 1], ['미쳤', 1], ['갑자기', 1],
  ],
  happy: [
    ['행복', 3], ['사랑', 3], ['기뻐', 3], ['즐거워', 3],
    ['감사', 2], ['고마워', 2], ['신남', 2], ['설레', 2], ['다행', 2],
    ['좋아', 1], ['신나', 1], ['최고', 1], ['기대', 1],
  ],
  blank: [],
};

function detectExpression(text: string): Expression {
  const t = text.toLowerCase();
  let best: Expression = 'blank';
  let bestScore = 0;
  for (const [expr, pairs] of Object.entries(KEYWORD_WEIGHTS) as [Expression, Array<[string, number]>][]) {
    if (expr === 'blank') continue;
    const score = pairs.reduce((sum, [kw, w]) => sum + (t.includes(kw) ? w : 0), 0);
    if (score > bestScore) { bestScore = score; best = expr; }
  }
  return best;
}

function App() {
  const [text, setText] = useState('');
  const [sendToUnity, setSendToUnity] = useState<SendFn | null>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  // Unity가 키보드 이벤트를 전역 캡처하는 것을 막음
  // textarea/input에 포커스가 있을 때만 이벤트를 가로챔
  useEffect(() => {
    const intercept = (e: KeyboardEvent) => {
      const tag = (document.activeElement as HTMLElement)?.tagName;
      if (tag === 'TEXTAREA' || tag === 'INPUT') {
        e.stopImmediatePropagation();
      }
    };
    window.addEventListener('keydown',  intercept, true);
    window.addEventListener('keyup',    intercept, true);
    window.addEventListener('keypress', intercept, true);
    return () => {
      window.removeEventListener('keydown',  intercept, true);
      window.removeEventListener('keyup',    intercept, true);
      window.removeEventListener('keypress', intercept, true);
    };
  }, []);

  const handleReady = useCallback((send: SendFn) => {
    setSendToUnity(() => send);
  }, []);

  const handleRequestInput = useCallback((_placeholder: string) => {
    inputRef.current?.focus();
  }, []);

  const handleSubmit = useCallback(() => {
    const trimmed = text.trim();
    if (!trimmed) return;

    const expression = detectExpression(trimmed);
    const payload = JSON.stringify({
      id: `slime-${Date.now()}`,
      text: trimmed,
      expression,
      stage: 1,
    });

    if (sendToUnity) {
      sendToUnity('SlimeManager', 'CreateSlimeFromWeb', payload);
    } else {
      console.log('[Chat] Unity 미연결 — payload:', payload);
    }

    setText('');
    inputRef.current?.focus();
  }, [text, sendToUnity]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  }, [handleSubmit]);

  const handleInputFocus = useCallback(() => {
    (document.getElementById('unity-canvas') as HTMLCanvasElement | null)?.blur();
  }, []);

  const handleInputBlur = useCallback(() => {
    (document.getElementById('unity-canvas') as HTMLCanvasElement | null)?.focus();
  }, []);

  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative' }}>
      <UnityCanvas
        onProgress={(p) => console.log(`Loading: ${Math.round(p * 100)}%`)}
        onLoaded={() => console.log('Unity ready')}
        onError={(e) => console.error('Unity error:', e)}
        onReady={handleReady}
        onRequestInput={handleRequestInput}
      />

      <div style={{
        position: 'fixed', bottom: 0, left: 0, right: 0,
        padding: '12px 16px',
        paddingBottom: 'max(20px, env(safe-area-inset-bottom, 20px))',
        background: 'linear-gradient(to top, rgba(6,4,14,0.98) 60%, transparent)',
        display: 'flex', gap: 8, alignItems: 'flex-end',
      }}>
        <textarea
          ref={inputRef}
          value={text}
          onChange={e => setText(e.target.value)}
          onKeyDown={handleKeyDown}
          onFocus={handleInputFocus}
          onBlur={handleInputBlur}
          placeholder="지금 기분이 어때요?"
          maxLength={200}
          rows={1}
          style={{
            flex: 1,
            background: 'rgba(255,255,255,0.09)',
            border: '1.5px solid rgba(255,255,255,0.15)',
            borderRadius: 20,
            padding: '14px 18px',
            color: '#e8e8f0',
            fontSize: 16,
            lineHeight: '22px',
            resize: 'none',
            outline: 'none',
            fontFamily: 'inherit',
            maxHeight: 100,
            overflowY: 'auto',
            WebkitAppearance: 'none',
          } as React.CSSProperties}
          onInput={e => {
            const el = e.currentTarget;
            el.style.height = 'auto';
            el.style.height = Math.min(el.scrollHeight, 100) + 'px';
          }}
        />
        <button
          onClick={handleSubmit}
          disabled={!text.trim()}
          style={{
            background: text.trim() ? '#7C3AED' : 'rgba(124,58,237,0.25)',
            border: 'none',
            borderRadius: 20,
            padding: '14px 22px',
            color: text.trim() ? '#fff' : 'rgba(255,255,255,0.3)',
            fontSize: 16,
            fontWeight: 700,
            cursor: text.trim() ? 'pointer' : 'default',
            whiteSpace: 'nowrap',
            transition: 'background 0.2s, color 0.2s',
            minWidth: 80,
            WebkitTapHighlightColor: 'transparent',
          } as React.CSSProperties}
        >
          털어내기
        </button>
      </div>
    </div>
  );
}

export default App;
