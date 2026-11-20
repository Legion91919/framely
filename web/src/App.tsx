import { useEffect, useMemo, useState } from 'react';
import { createApi } from './api/client';
import { ApiContext } from './api/context';
import type { DemoViewer } from './api/types';
import { AppBar } from './components/AppBar';
import { ComposeDialog } from './components/ComposeDialog';
import { FollowRail } from './components/FollowRail';
import { HomeFeed } from './components/HomeFeed';

const DEFAULT_HANDLE = 'thandi.m';

export function App() {
  const [viewers, setViewers] = useState<DemoViewer[]>([]);
  const [viewerId, setViewerId] = useState<string | null>(null);
  const [bootError, setBootError] = useState<string | null>(null);
  const [composing, setComposing] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const api = useMemo(() => createApi(viewerId), [viewerId]);

  useEffect(() => {
    createApi(null)
      .demoViewers()
      .then((v) => {
        setViewers(v);
        setViewerId((v.find((x) => x.handle === DEFAULT_HANDLE) ?? v[0])?.id ?? null);
      })
      .catch((e: Error) => setBootError(e.message));
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const typing = e.target instanceof HTMLElement && /INPUT|TEXTAREA|SELECT/.test(e.target.tagName);
      if (e.key === 'n' && !typing && !composing && viewerId) {
        e.preventDefault();
        setComposing(true);
      }
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [composing, viewerId]);

  return (
    <ApiContext.Provider value={api}>
      <AppBar viewers={viewers} viewerId={viewerId} onViewerChange={setViewerId} onCompose={() => setComposing(true)} />
      {viewerId ? (
        <div className="wrap">
          <HomeFeed viewerId={viewerId} refreshKey={refreshKey} onFindPhotographers={() => document.getElementById('photographers')?.scrollIntoView()} />
          <FollowRail viewerId={viewerId} onChanged={() => setRefreshKey((k) => k + 1)} />
        </div>
      ) : (
        <div className="wrap">
          <main>
            {bootError ? (
              <div className="state-box error" role="alert">
                <h2>Framely is not reachable</h2>
                <p>{bootError}. Start the API on port 5101 (see the runbook) and reload.</p>
              </div>
            ) : (
              <p role="status" className="hint">
                Loading…
              </p>
            )}
          </main>
        </div>
      )}
      {viewerId && (
        <button className="fab" aria-label="Post a photo" onClick={() => setComposing(true)}>
          +
        </button>
      )}
      {composing && (
        <ComposeDialog
          onClose={() => setComposing(false)}
          onPosted={() => {
            setRefreshKey((k) => k + 1);
            setTimeout(() => setComposing(false), 900);
          }}
        />
      )}
    </ApiContext.Provider>
  );
}
