import type { FeedItem } from './api/types';

/** Appends an older page, dropping anything already shown (a post can appear twice if the feed moved between pages). */
export function mergePages(current: FeedItem[], older: FeedItem[]): FeedItem[] {
  const seen = new Set(current.map((p) => p.id));
  return [...current, ...older.filter((p) => !seen.has(p.id))];
}

/** "4 min ago", "3 h ago", "Yesterday", "2 d ago", then a date. */
export function relativeTime(iso: string, now: Date = new Date()): string {
  const minutes = Math.floor((now.getTime() - new Date(iso).getTime()) / 60000);
  if (minutes < 1) return 'Just now';
  if (minutes < 60) return `${minutes} min ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} h ago`;
  const days = Math.floor(hours / 24);
  if (days === 1) return 'Yesterday';
  if (days < 7) return `${days} d ago`;
  return new Date(iso).toLocaleDateString('en-ZA', { day: 'numeric', month: 'short' });
}

/** The "N new frames from M photographers" strip: posts from others in the last 12 hours. */
export function freshSummary(items: FeedItem[], viewerId: string, now: Date = new Date()): { count: number; authors: number } {
  const cutoff = now.getTime() - 12 * 60 * 60 * 1000;
  const fresh = items.filter((p) => p.author.id !== viewerId && new Date(p.publishedAt).getTime() >= cutoff);
  return { count: fresh.length, authors: new Set(fresh.map((p) => p.author.id)).size };
}

export function initials(name: string): string {
  return name
    .split(' ')
    .filter((w) => /^\p{Lu}/u.test(w))
    .map((w) => w[0])
    .join('')
    .slice(0, 2);
}

export const MAX_PHOTO_BYTES = 10 * 1024 * 1024;
export const MAX_CAPTION = 500;

/** Mirrors the API's checks so people get instant feedback; the API stays the source of truth. */
export function validatePhoto(file: Pick<File, 'type' | 'size' | 'name'> | null): string | null {
  if (!file) return 'Choose a photo first.';
  if (file.type !== 'image/jpeg') return `That file is ${file.type || 'not an image'}. Framely accepts JPEG photos up to 10 MB.`;
  if (file.size === 0) return 'That file is empty.';
  if (file.size > MAX_PHOTO_BYTES) return `That photo is ${(file.size / 1048576).toFixed(1)} MB. The limit is 10 MB.`;
  return null;
}
