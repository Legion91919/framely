import type { DemoViewer } from '../api/types';
import { Logo } from './Logo';

interface Props {
  viewers: DemoViewer[];
  viewerId: string | null;
  onViewerChange(id: string): void;
  onCompose(): void;
}

export function AppBar({ viewers, viewerId, onViewerChange, onCompose }: Props) {
  return (
    <header className="top">
      <a className="mark" href="/" aria-label="Framely home">
        <Logo />
        framely
      </a>
      <span className="spacer" />
      <button className="btn btn-primary compose-top" onClick={onCompose} aria-keyshortcuts="n" disabled={!viewerId}>
        Post a photo <kbd>N</kbd>
      </button>
      {viewers.length > 0 && (
        <div className="viewer">
          <label>
            <span>Viewing as </span>
            <select aria-label="Viewing as (demo)" value={viewerId ?? ''} onChange={(e) => onViewerChange(e.target.value)}>
              {viewers.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.displayName}
                </option>
              ))}
            </select>
          </label>
        </div>
      )}
    </header>
  );
}
