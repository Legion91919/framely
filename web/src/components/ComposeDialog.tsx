import { useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '../api/client';
import { useApi } from '../api/context';
import type { FeedItem } from '../api/types';
import { MAX_CAPTION, validatePhoto } from '../feedLogic';

interface Props {
  onClose(): void;
  onPosted(post: FeedItem): void;
  newKey?: () => string;
}

type Phase = { kind: 'idle' } | { kind: 'uploading' } | { kind: 'rejected'; message: string } | { kind: 'posted' };

/** Compose a post: JPEG + caption + alt text. One Idempotency-Key per dialog, so a retry never double-posts. */
export function ComposeDialog({ onClose, onPosted, newKey = () => crypto.randomUUID() }: Props) {
  const api = useApi();
  const [file, setFile] = useState<File | null>(null);
  const [caption, setCaption] = useState('');
  const [altText, setAltText] = useState('');
  const [phase, setPhase] = useState<Phase>({ kind: 'idle' });
  const [idempotencyKey] = useState(newKey);
  const fileInput = useRef<HTMLInputElement>(null);
  const preview = useMemo(() => (file && typeof URL.createObjectURL === 'function' ? URL.createObjectURL(file) : null), [file]);

  useEffect(() => {
    fileInput.current?.focus();
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);

  function choose(f: File | null) {
    setFile(f);
    const problem = f ? validatePhoto(f) : null;
    setPhase(problem ? { kind: 'rejected', message: problem } : { kind: 'idle' });
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    const problem = validatePhoto(file) ?? (caption.length > MAX_CAPTION ? `Captions can be at most ${MAX_CAPTION} characters.` : null);
    if (problem || !file) {
      setPhase({ kind: 'rejected', message: problem ?? 'Choose a photo first.' });
      return;
    }
    setPhase({ kind: 'uploading' });
    try {
      const post = await api.post(file, caption, altText, idempotencyKey);
      setPhase({ kind: 'posted' });
      onPosted(post);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Network problem. Your photo was not posted; try again.';
      setPhase({ kind: 'rejected', message });
    }
  }

  const status =
    phase.kind === 'uploading'
      ? { cls: 'hint', text: 'Uploading and removing location data…' }
      : phase.kind === 'rejected'
        ? { cls: 'err', text: phase.message }
        : phase.kind === 'posted'
          ? { cls: 'ok', text: 'Posted. Your followers will see it at the top of their feed.' }
          : { cls: 'hint', text: '' };

  return (
    <div className="overlay" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <div className="dialog" role="dialog" aria-modal="true" aria-labelledby="compose-title">
        <form onSubmit={submit} noValidate>
          <h2 id="compose-title">Post a photo</h2>
          <div className="pick">
            {preview && <img src={preview} alt="Selected photo preview" />}
            <label>
              Photo (JPEG)
              <input ref={fileInput} type="file" accept="image/jpeg" onChange={(e) => choose(e.target.files?.[0] ?? null)} />
            </label>
            <p className="hint">JPEG up to 10 MB. Location (GPS) data is removed unless location sharing is on in your settings.</p>
          </div>
          <label>
            Caption
            <textarea rows={3} value={caption} maxLength={MAX_CAPTION} onChange={(e) => setCaption(e.target.value)} aria-describedby="caption-count" />
          </label>
          <p className="hint" id="caption-count">
            {caption.length} / {MAX_CAPTION}
          </p>
          <label>
            Describe the photo (alt text)
            <input type="text" value={altText} maxLength={300} onChange={(e) => setAltText(e.target.value)} />
          </label>
          <p className={status.cls} role="status" aria-live="polite">
            {status.text}
          </p>
          <div className="actions">
            <button type="button" className="btn" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={phase.kind === 'uploading' || phase.kind === 'posted'}>
              {phase.kind === 'uploading' ? 'Posting…' : 'Post to followers'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
