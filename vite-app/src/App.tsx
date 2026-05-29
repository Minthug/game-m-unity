import { useState, useCallback, useRef, useEffect } from 'react';
import UnityCanvas from './UnityCanvas';

type SendFn = (obj: string, method: string, param: string) => void;
type Expression = 'angry' | 'sad' | 'fear' | 'happy' | 'disgust' | 'contempt' | 'surprised' | 'blank';

const KEYWORD_WEIGHTS: Record<Expression, Array<[string, number]>> = {
  angry: [
    // 강한 욕설
    ['씨발', 4], ['씨바', 4], ['시발', 4], ['시바', 4], ['썅', 4],
    ['개새', 4], ['병신', 4], ['느금', 4], ['니애미', 4], ['니에미', 4],
    // 강한 분노 표현
    ['빡쳐', 3], ['빡침', 3], ['열불', 3], ['개짜증', 3],
    ['닥쳐', 3], ['꺼져', 3], ['존나', 3], ['개같', 3], ['개소리', 3],
    ['헛소리', 3], ['집어쳐', 3], ['미친놈', 3], ['미친년', 3],
    // 중간 분노
    ['빡', 2], ['열받', 2], ['짜증', 2], ['화나', 2], ['화났', 2], ['화딱지', 2],
    ['분노', 2], ['억울', 2], ['어이없', 2], ['황당', 2], ['더럽', 2],
    ['미치겠', 2], ['환장', 2], ['뚜껑', 2], ['속터', 2], ['분해', 2],
    ['답답', 2], ['열나', 2], ['버럭', 2], ['꼴받', 2], ['욱해', 2],
    ['참을수없', 2], ['못참', 2], ['뒤집', 2], ['울화', 2],
    // 가벼운 불만
    ['최악', 1], ['싫어', 1], ['별로', 1], ['망했', 1], ['왜이래', 1],
    ['짜증나', 1], ['귀찮', 1], ['피곤해', 1], ['성가', 1], ['불쾌', 1],
  ],
  sad: [
    // 강한 울음
    ['흑흑', 3], ['훌쩍', 3], ['엉엉', 3], ['으앙', 3], ['으엉', 3],
    ['ㅠㅠ', 3], ['ㅜㅜ', 3], ['T_T', 3], ['TT', 3], ['펑펑', 3],
    ['절망', 3], ['비참', 3],
    // 슬픔 감정 (어근 기반)
    ['슬프', 2], ['슬퍼', 2], ['눈물', 2], ['우울', 2], ['무기력', 2],
    ['외롭', 2], ['외로워', 2], ['힘들', 2], ['보고싶', 2], ['그리', 2],
    ['서러', 2], ['괴로', 2], ['속상', 2], ['먹먹', 2], ['허전', 2],
    ['울컥', 2], ['쓸쓸', 2], ['서글', 2], ['공허', 2], ['낙담', 2],
    ['실망', 2], ['상처', 2], ['가슴아', 2], ['마음아', 2], ['찢어', 2],
    ['눈물나', 2], ['울고싶', 2], ['처량', 2], ['외면', 2], ['고독', 2],
    // 가벼운 슬픔
    ['울고', 1], ['지쳐', 1], ['아파', 1], ['지침', 1], ['포기', 1],
    ['찡해', 1], ['뭉클', 1], ['시무룩', 1], ['풀이죽', 1], ['기운없', 1],
    ['ㅠ-ㅜ', 1], ['ㅜ-ㅠ', 1], ['ㅠ.ㅠ', 1], ['ㅜ.ㅜ', 1], ['ㅠ', 1], ['ㅜ', 1],
  ],
  fear: [
    // 강한 공포
    ['공황', 4], ['공포', 3], ['패닉', 3],
    ['두려', 3], ['두렵', 3],
    // 중간 공포
    ['무서', 2], ['무섭', 2], ['오싹', 2], ['소름', 2],
    ['떨려', 2], ['덜덜', 2], ['부들부들', 2], ['진땀', 2], ['식은땀', 2],
    ['겁나', 2], ['겁먹', 2], ['겁이', 2], ['긴장', 2],
    // 가벼운 불안
    ['불안', 1], ['걱정', 1], ['초조', 1], ['조마조마', 1],
    ['꺼림', 1], ['위험', 1], ['심장이', 1], ['악몽', 1], ['으스스', 1],
  ],
  disgust: [
    // 강한 혐오
    ['혐오', 4], ['구역질', 3], ['토나', 3], ['역겨', 3],
    // 중간 혐오
    ['징그러', 2], ['더러', 2], ['질려', 2], ['지겨', 2], ['지겹', 2],
    ['싫증', 2], ['오글', 2], ['창피', 2], ['쪽팔', 2], ['한심', 2],
    ['꼴보기싫', 2], ['꼴불견', 2], ['진절머리', 2],
    // 가벼운 불쾌
    ['역해', 1], ['구리', 1], ['냄새나', 1], ['민망', 1], ['어색', 1],
    ['불편', 1], ['찝찝', 1], ['께름직', 1],
  ],
  contempt: [
    // 강한 경멸
    ['경멸', 4], ['무시', 3], ['코웃음', 3], ['비웃', 3],
    // 중간 경멸
    ['하찮', 2], ['저급', 2], ['저질', 2], ['건방', 2],
    ['웃기지마', 2], ['찌질', 2], ['나댄다', 2], ['설친다', 2],
    ['기가차', 2], ['기가막혀', 2], ['수준이하', 2], ['무능', 2],
    // 가벼운 경멸
    ['우습', 1], ['촌스럽', 1], ['유치', 1], ['치졸', 1], ['꼴사납', 1],
  ],
  surprised: [
    // 강한 놀람
    ['말도안돼', 3], ['믿을수없', 3], ['이게뭐', 3], ['말이안돼', 3],
    ['어안이벙', 3], ['벙쪄', 3], ['벙찐', 3],
    // 중간 놀람
    ['충격', 2], ['대박', 2], ['설마', 2], ['당황', 2],
    ['깜짝', 2], ['헉', 2], ['반전', 2], ['뜬금없', 2],
    ['어머', 2], ['어머나', 2], ['갑툭튀', 2],
    // 가벼운 놀람
    ['헐', 1], ['뭐야', 1], ['놀랐', 1], ['미쳤', 1], ['갑자기', 1],
    ['진짜로', 1], ['실화', 1], ['레알', 1], ['진심', 1],
  ],
  happy: [
    // 강한 기쁨
    ['행복', 3], ['사랑', 3], ['기뻐', 3], ['기쁘', 3], ['즐거', 3],
    ['최고야', 3], ['너무좋아', 3], ['감동', 3],
    // 중간 기쁨
    ['감사', 2], ['고마워', 2], ['신남', 2], ['설레', 2], ['다행', 2],
    ['뿌듯', 2], ['기분좋', 2], ['만족', 2], ['두근', 2], ['해냈', 2],
    ['성공', 2], ['힐링', 2], ['치유', 2], ['따뜻', 2], ['포근', 2],
    ['흐뭇', 2], ['웃음나', 2], ['신나', 2], ['기대돼', 2],
    // 가벼운 기쁨
    ['좋아', 1], ['최고', 1], ['기대', 1], ['재밌', 1], ['즐거운', 1],
    ['풀렸', 1], ['해결됐', 1], ['다행이', 1], ['럭키', 1], ['ㅋㅋ', 1], ['ㅎㅎ', 1],
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
  const [isInputOpen, setIsInputOpen] = useState(false);
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
    setIsInputOpen(true);
    setTimeout(() => inputRef.current?.focus(), 50);
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
    setIsInputOpen(false);
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

  const openInput = () => {
    setIsInputOpen(true);
    setTimeout(() => inputRef.current?.focus(), 80);
  };

  const closeInput = () => {
    setIsInputOpen(false);
    setText('');
    (document.getElementById('unity-canvas') as HTMLCanvasElement | null)?.focus();
  };

  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative' }}>
      <UnityCanvas
        onProgress={(p) => console.log(`Loading: ${Math.round(p * 100)}%`)}
        onLoaded={() => console.log('Unity ready')}
        onError={(e) => console.error('Unity error:', e)}
        onReady={handleReady}
        onRequestInput={handleRequestInput}
      />

      {/* 플로팅 버튼 — 하단 중앙 */}
      {!isInputOpen && (
        <button
          onClick={openInput}
          style={{
            position: 'fixed',
            bottom: 'max(36px, env(safe-area-inset-bottom, 36px))',
            left: '50%',
            transform: 'translateX(-50%)',
            width: 64,
            height: 64,
            borderRadius: 32,
            background: 'rgba(90, 40, 200, 0.82)',
            border: '2px solid rgba(180, 150, 255, 0.35)',
            boxShadow: '0 4px 20px rgba(110, 50, 220, 0.55), 0 0 0 6px rgba(110,50,220,0.12)',
            fontSize: 30,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            WebkitTapHighlightColor: 'transparent',
            backdropFilter: 'blur(4px)',
          } as React.CSSProperties}
        >
          🫠
        </button>
      )}

      {/* 입력 패널 오버레이 */}
      {isInputOpen && (
        <>
          {/* 반투명 배경 — 탭하면 닫힘 */}
          <div
            onClick={closeInput}
            style={{
              position: 'fixed', inset: 0,
              background: 'rgba(4, 2, 12, 0.55)',
              backdropFilter: 'blur(2px)',
            }}
          />

          {/* 입력 패널 */}
          <div style={{
            position: 'fixed',
            bottom: 0, left: 0, right: 0,
            background: 'linear-gradient(180deg, rgba(18,8,40,0.98) 0%, rgba(8,4,20,1) 100%)',
            borderRadius: '28px 28px 0 0',
            border: '1px solid rgba(130, 80, 255, 0.25)',
            borderBottom: 'none',
            boxShadow: '0 -8px 40px rgba(100, 40, 220, 0.3)',
            padding: '20px 20px',
            paddingBottom: 'max(24px, env(safe-area-inset-bottom, 24px))',
            display: 'flex',
            flexDirection: 'column',
            gap: 14,
          }}>
            {/* 핸들 + 헤더 */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12 }}>
              <div style={{
                width: 40, height: 4, borderRadius: 2,
                background: 'rgba(160, 120, 255, 0.3)',
              }} />
              <div style={{ display: 'flex', alignItems: 'center', width: '100%', justifyContent: 'space-between' }}>
                <span style={{ color: 'rgba(180, 150, 255, 0.9)', fontSize: 14, fontWeight: 600, letterSpacing: 0.3 }}>
                  지금 기분이 어때요?
                </span>
                <button
                  onClick={closeInput}
                  style={{
                    background: 'rgba(255,255,255,0.07)',
                    border: 'none',
                    borderRadius: 12,
                    color: 'rgba(255,255,255,0.45)',
                    fontSize: 14,
                    fontWeight: 600,
                    padding: '4px 10px',
                    cursor: 'pointer',
                    WebkitTapHighlightColor: 'transparent',
                  } as React.CSSProperties}
                >
                  닫기
                </button>
              </div>
            </div>

            {/* 입력창 */}
            <textarea
              ref={inputRef}
              value={text}
              onChange={e => setText(e.target.value)}
              onKeyDown={handleKeyDown}
              onFocus={handleInputFocus}
              placeholder="지금 느끼는 감정을 뭐든 써봐요"
              maxLength={200}
              rows={3}
              style={{
                width: '100%',
                boxSizing: 'border-box',
                background: 'rgba(255,255,255,0.04)',
                border: '1px solid rgba(140, 90, 255, 0.25)',
                borderRadius: 16,
                padding: '16px',
                color: '#ddd8ff',
                fontSize: 17,
                lineHeight: '26px',
                resize: 'none',
                outline: 'none',
                fontFamily: 'inherit',
                maxHeight: 130,
                overflowY: 'auto',
                WebkitAppearance: 'none',
              } as React.CSSProperties}
              onInput={e => {
                const el = e.currentTarget;
                el.style.height = 'auto';
                el.style.height = Math.min(el.scrollHeight, 130) + 'px';
              }}
            />

            {/* 전송 버튼 — 풀 너비 */}
            <button
              onClick={handleSubmit}
              disabled={!text.trim()}
              style={{
                width: '100%',
                background: text.trim()
                  ? 'linear-gradient(135deg, #6D28D9, #8B5CF6)'
                  : 'rgba(100,60,200,0.15)',
                border: 'none',
                borderRadius: 16,
                padding: '16px',
                color: text.trim() ? '#fff' : 'rgba(255,255,255,0.2)',
                fontSize: 17,
                fontWeight: 700,
                cursor: text.trim() ? 'pointer' : 'default',
                letterSpacing: 0.5,
                transition: 'background 0.2s, box-shadow 0.2s',
                boxShadow: text.trim() ? '0 6px 24px rgba(109,40,217,0.5)' : 'none',
                WebkitTapHighlightColor: 'transparent',
              } as React.CSSProperties}
            >
              {text.trim() ? '🫠 털어내기' : '털어내기'}
            </button>
          </div>
        </>
      )}
    </div>
  );
}

export default App;
