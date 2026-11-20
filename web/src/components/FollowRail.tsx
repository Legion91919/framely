import { useEffect, useState } from 'react';
import { useApi } from '../api/context';
import type { Member } from '../api/types';
import { initials } from '../feedLogic';

interface ButtonProps {
  member: Member;
  onToggled(following: boolean): void;
}

export function FollowButton({ member, onToggled }: ButtonProps) {
  const api = useApi();
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const following = member.isFollowing;

  async function toggle() {
    setSaving(true);
    setError(null);
    try {
      await (following ? api.unfollow(member.id) : api.follow(member.id));
      onToggled(!following);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <span style={{ display: 'grid', justifyItems: 'end' }}>
      <button
        className={`btn ${following ? 'btn-following' : ''}`}
        aria-pressed={following}
        aria-label={`${following ? 'Following' : 'Follow'} ${member.displayName}`}
        onClick={toggle}
        disabled={saving}
      >
        {saving ? '…' : following ? 'Following' : 'Follow'}
      </button>
      {error && <span className="err">{error}</span>}
    </span>
  );
}

interface RailProps {
  viewerId: string;
  onChanged(): void;
}

export function FollowRail({ viewerId, onChanged }: RailProps) {
  const api = useApi();
  const [members, setMembers] = useState<Member[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setMembers(null);
    setError(null);
    api
      .members()
      .then((m) => !cancelled && setMembers(m))
      .catch((e: Error) => !cancelled && setError(e.message));
    return () => {
      cancelled = true;
    };
  }, [api, viewerId, attempt]);

  return (
    <aside aria-label="Photographers to follow">
      <div className="panel" id="photographers">
        <h2>Photographers</h2>
        {error ? (
          <div role="alert">
            <p className="err">{error}</p>
            <button className="btn" onClick={() => setAttempt((n) => n + 1)}>
              Try again
            </button>
          </div>
        ) : members === null ? (
          <ul aria-busy="true">
            {[0, 1, 2].map((i) => (
              <li key={i}>
                <span className="avatar skel" />
                <span className="skel" style={{ height: 14, flex: 1 }} />
              </li>
            ))}
          </ul>
        ) : members.length === 0 ? (
          <p className="hint">No other photographers yet.</p>
        ) : (
          <ul>
            {members.map((m) => (
              <li key={m.id}>
                <span className="avatar" aria-hidden="true">
                  {initials(m.displayName)}
                </span>
                <span className="who">
                  <b>{m.displayName}</b>
                  <span className="muted">
                    @{m.handle} · {m.city}
                  </span>
                </span>
                <FollowButton
                  member={m}
                  onToggled={(following) => {
                    setMembers((all) => all?.map((x) => (x.id === m.id ? { ...x, isFollowing: following } : x)) ?? null);
                    onChanged();
                  }}
                />
              </li>
            ))}
          </ul>
        )}
      </div>
    </aside>
  );
}
