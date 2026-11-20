import { useState } from 'react';
import type { FeedItem } from '../api/types';
import { initials, relativeTime } from '../feedLogic';

export function PhotoCard({ post, now }: { post: FeedItem; now?: Date }) {
  const [broken, setBroken] = useState(false);
  const titleId = `post-${post.id}`;
  const ratio = post.width > 0 && post.height > 0 ? `${post.width} / ${post.height}` : '4 / 5';
  return (
    <article className="card" aria-labelledby={titleId}>
      <header>
        <span className="avatar" aria-hidden="true">
          {initials(post.author.displayName)}
        </span>
        <span className="who">
          <b id={titleId}>{post.author.displayName}</b>
          <span className="muted">
            @{post.author.handle} · {post.author.city}
          </span>
        </span>
      </header>
      {broken ? (
        <div className="img-fallback" style={{ aspectRatio: ratio }} role="img" aria-label={post.altText || 'Photo unavailable'}>
          Photo unavailable{post.altText ? `: ${post.altText}` : ''}
        </div>
      ) : (
        <img
          src={post.imageUrl}
          alt={post.altText || `Photo by ${post.author.displayName}`}
          width={post.width || undefined}
          height={post.height || undefined}
          style={{ aspectRatio: ratio }}
          loading="lazy"
          onError={() => setBroken(true)}
        />
      )}
      {post.caption && <p>{post.caption}</p>}
      <time dateTime={post.publishedAt}>{relativeTime(post.publishedAt, now)}</time>
    </article>
  );
}

export function PhotoCardSkeleton() {
  return (
    <article className="card" aria-hidden="true">
      <header>
        <span className="avatar skel" />
        <span className="who">
          <span className="skel" style={{ display: 'block', height: 14, width: '40%' }} />
          <span className="skel" style={{ display: 'block', height: 12, width: '60%', marginTop: 6 }} />
        </span>
      </header>
      <div className="skel" style={{ aspectRatio: '4 / 5', borderRadius: 0 }} />
      <p>
        <span className="skel" style={{ display: 'block', height: 14, width: '80%' }} />
      </p>
      <time>&nbsp;</time>
    </article>
  );
}
