import { describe, expect, it } from 'vitest';
import type { FeedItem } from './api/types';
import { freshSummary, initials, mergePages, relativeTime, validatePhoto } from './feedLogic';

const now = new Date('2026-11-19T12:00:00Z');
const post = (id: string, authorId: string, minutesAgo: number): FeedItem => ({
  id,
  author: { id: authorId, handle: 'h', displayName: 'Naledi Dube', city: 'Cape Town' },
  imageUrl: '/media/x.jpg',
  width: 800,
  height: 1000,
  altText: '',
  caption: '',
  publishedAt: new Date(now.getTime() - minutesAgo * 60000).toISOString(),
});

describe('feed logic', () => {
  it('mergePages appends older posts and drops duplicates', () => {
    const merged = mergePages([post('1', 'a', 1), post('2', 'a', 2)], [post('2', 'a', 2), post('3', 'a', 3)]);
    expect(merged.map((p) => p.id)).toEqual(['1', '2', '3']);
  });

  it('relativeTime reads like a person wrote it', () => {
    expect(relativeTime(post('1', 'a', 4).publishedAt, now)).toBe('4 min ago');
    expect(relativeTime(post('1', 'a', 180).publishedAt, now)).toBe('3 h ago');
    expect(relativeTime(post('1', 'a', 60 * 30).publishedAt, now)).toBe('Yesterday');
  });

  it('freshSummary counts only other people’s posts from the last 12 hours', () => {
    const items = [post('1', 'a', 10), post('2', 'b', 60), post('3', 'me', 5), post('4', 'a', 60 * 13)];
    expect(freshSummary(items, 'me', now)).toEqual({ count: 2, authors: 2 });
  });

  it('initials skip lowercase particles', () => {
    expect(initials('Pieter van Wyk')).toBe('PW');
  });

  it('validatePhoto mirrors the API: JPEG only, not empty, max 10 MB', () => {
    expect(validatePhoto({ type: 'image/jpeg', size: 2_000_000, name: 'a.jpg' })).toBeNull();
    expect(validatePhoto({ type: 'image/png', size: 2_000, name: 'a.png' })).toContain('JPEG');
    expect(validatePhoto({ type: 'image/jpeg', size: 11 * 1024 * 1024, name: 'a.jpg' })).toContain('10 MB');
    expect(validatePhoto(null)).toBe('Choose a photo first.');
  });
});
