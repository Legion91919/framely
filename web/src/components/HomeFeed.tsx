import { useCallback, useEffect, useState } from 'react';
import { useApi } from '../api/context';
import type { FeedItem } from '../api/types';
import { freshSummary, mergePages } from '../feedLogic';
import { Logo } from './Logo';
import { PhotoCard, PhotoCardSkeleton } from './PhotoCard';

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; items: FeedItem[]; nextCursor: string | null; loadingMore: boolean; moreError: string | null };

interface Props {
  viewerId: string;
  refreshKey: number;
  onFindPhotographers?(): void;
}

/** The pre-built home feed: newest first, 25 per page, "Show older frames" follows the cursor. */
export function HomeFeed({ viewerId, refreshKey, onFindPhotographers }: Props) {
  const api = useApi();
  const [state, setState] = useState<State>({ status: 'loading' });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setState({ status: 'loading' });
    api
      .feed()
      .then((page) => !cancelled && setState({ status: 'ready', items: page.items, nextCursor: page.nextCursor, loadingMore: false, moreError: null }))
      .catch((e: Error) => !cancelled && setState({ status: 'error', message: e.message }));
    return () => {
      cancelled = true;
    };
  }, [api, viewerId, refreshKey, attempt]);

  const loadMore = useCallback(async () => {
    if (state.status !== 'ready' || !state.nextCursor) return;
    setState({ ...state, loadingMore: true, moreError: null });
    try {
      const page = await api.feed(state.nextCursor);
      setState({ status: 'ready', items: mergePages(state.items, page.items), nextCursor: page.nextCursor, loadingMore: false, moreError: null });
    } catch (e) {
      setState({ ...state, loadingMore: false, moreError: (e as Error).message });
    }
  }, [api, state]);

  if (state.status === 'loading') {
    return (
      <main aria-label="Home feed, newest first" aria-busy="true">
        <p className="sr-only" role="status">
          Loading your feed…
        </p>
        <PhotoCardSkeleton />
        <PhotoCardSkeleton />
      </main>
    );
  }

  if (state.status === 'error') {
    return (
      <main aria-label="Home feed, newest first">
        <div className="state-box error" role="alert">
          <h2>We couldn't load your feed</h2>
          <p>{state.message}</p>
          <p>
            <button className="btn btn-primary" onClick={() => setAttempt((n) => n + 1)}>
              Try again
            </button>
          </p>
        </div>
      </main>
    );
  }

  if (state.items.length === 0) {
    return (
      <main aria-label="Home feed, newest first">
        <div className="state-box">
          <Logo size={56} />
          <h2>Your wall is empty</h2>
          <p className="muted">Follow a few photographers and their newest frames land here first.</p>
          {onFindPhotographers && (
            <p>
              <button className="btn btn-primary" onClick={onFindPhotographers}>
                Find photographers
              </button>
            </p>
          )}
        </div>
      </main>
    );
  }

  const fresh = freshSummary(state.items, viewerId);
  return (
    <main aria-label="Home feed, newest first" aria-busy="false">
      {fresh.count > 0 && (
        <div className="fresh" role="status">
          {fresh.count} new {fresh.count === 1 ? 'frame' : 'frames'} from {fresh.authors} {fresh.authors === 1 ? 'photographer' : 'photographers'} in the last 12 hours
        </div>
      )}
      {state.items.map((post) => (
        <PhotoCard key={post.id} post={post} />
      ))}
      {state.moreError && (
        <p className="err" role="alert">
          {state.moreError}
        </p>
      )}
      {state.nextCursor ? (
        <button className="btn more" onClick={loadMore} disabled={state.loadingMore}>
          {state.loadingMore ? 'Loading…' : 'Show older frames'}
        </button>
      ) : (
        <p className="hint" style={{ textAlign: 'center', marginBottom: 'var(--space-xl)' }}>
          You're all caught up.
        </p>
      )}
    </main>
  );
}
