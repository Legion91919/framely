import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, createApi, type FramelyApi } from '../api/client';
import { ApiContext } from '../api/context';
import type { FeedItem, FeedPage } from '../api/types';
import { ComposeDialog } from './ComposeDialog';
import { HomeFeed } from './HomeFeed';

afterEach(cleanup);

const item = (n: number): FeedItem => ({
  id: `p${n}`,
  author: { id: 'naledi', handle: 'naledi.frames', displayName: 'Naledi Dube', city: 'Cape Town' },
  imageUrl: `data:image/svg+xml,<svg/>`,
  width: 800,
  height: 1000,
  altText: `Frame ${n}`,
  caption: `Caption ${n}`,
  publishedAt: new Date().toISOString(),
});

function fakeApi(overrides: Partial<FramelyApi>): FramelyApi {
  const fail = () => Promise.reject(new Error('not stubbed'));
  return { feed: fail, members: fail, me: fail, follow: fail, unfollow: fail, post: fail, demoViewers: fail, ...overrides };
}

const renderWith = (api: FramelyApi, ui: React.ReactElement) => render(<ApiContext.Provider value={api}>{ui}</ApiContext.Provider>);

describe('HomeFeed', () => {
  it('shows a loading state, then the newest post first, then pages with the cursor', async () => {
    const feed = vi.fn((cursor?: string | null): Promise<FeedPage> =>
      Promise.resolve(cursor === 'c1' ? { items: [item(26)], nextCursor: null } : { items: Array.from({ length: 25 }, (_, i) => item(i + 1)), nextCursor: 'c1' }),
    );
    renderWith(fakeApi({ feed }), <HomeFeed viewerId="thandi" refreshKey={0} />);

    expect(screen.getByText('Loading your feed…')).toBeTruthy();
    const articles = await screen.findAllByRole('article');
    expect(articles).toHaveLength(25);
    expect(articles[0].textContent).toContain('Caption 1');

    fireEvent.click(screen.getByRole('button', { name: 'Show older frames' }));
    await screen.findByText('Caption 26');
    expect(feed).toHaveBeenLastCalledWith('c1');
    expect(screen.getByText("You're all caught up.")).toBeTruthy();
  });

  it('shows the empty state when nobody is followed yet', async () => {
    renderWith(fakeApi({ feed: () => Promise.resolve({ items: [], nextCursor: null }) }), <HomeFeed viewerId="thandi" refreshKey={0} onFindPhotographers={() => {}} />);
    expect(await screen.findByText('Your wall is empty')).toBeTruthy();
  });

  it('shows an error with a working retry', async () => {
    const feed = vi.fn().mockRejectedValueOnce(new Error('Network down')).mockResolvedValueOnce({ items: [item(1)], nextCursor: null });
    renderWith(fakeApi({ feed }), <HomeFeed viewerId="thandi" refreshKey={0} />);

    expect((await screen.findByRole('alert')).textContent).toContain('Network down');
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));
    expect(await screen.findByText('Caption 1')).toBeTruthy();
  });
});

describe('ComposeDialog', () => {
  it('rejects a non-JPEG before uploading anything', async () => {
    const post = vi.fn();
    renderWith(fakeApi({ post }), <ComposeDialog onClose={() => {}} onPosted={() => {}} />);

    fireEvent.change(screen.getByLabelText('Photo (JPEG)'), { target: { files: [new File(['x'], 'shot.png', { type: 'image/png' })] } });

    expect(screen.getByRole('status').textContent).toContain('Framely accepts JPEG photos');
    fireEvent.click(screen.getByRole('button', { name: 'Post to followers' }));
    expect(post).not.toHaveBeenCalled();
  });

  it('posts photo, caption and alt text with one idempotency key, reusing it on retry', async () => {
    const created = item(99);
    const post = vi.fn().mockRejectedValueOnce(new ApiError(503, { detail: 'Try again shortly.' })).mockResolvedValueOnce(created);
    const onPosted = vi.fn();
    renderWith(fakeApi({ post }), <ComposeDialog onClose={() => {}} onPosted={onPosted} newKey={() => 'key-1'} />);

    const photo = new File([new Uint8Array([0xff, 0xd8, 0xff])], 'dawn.jpg', { type: 'image/jpeg' });
    fireEvent.change(screen.getByLabelText('Photo (JPEG)'), { target: { files: [photo] } });
    fireEvent.change(screen.getByLabelText('Caption'), { target: { value: 'Harbour at dawn' } });
    fireEvent.change(screen.getByLabelText('Describe the photo (alt text)'), { target: { value: 'Boats in a calm harbour' } });
    fireEvent.click(screen.getByRole('button', { name: 'Post to followers' }));
    expect(await screen.findByText('Try again shortly.')).toBeTruthy();

    fireEvent.click(screen.getByRole('button', { name: 'Post to followers' }));
    await waitFor(() => expect(onPosted).toHaveBeenCalledWith(created));
    expect(post).toHaveBeenNthCalledWith(1, photo, 'Harbour at dawn', 'Boats in a calm harbour', 'key-1');
    expect(post).toHaveBeenNthCalledWith(2, photo, 'Harbour at dawn', 'Boats in a calm harbour', 'key-1');
  });
});

describe('createApi', () => {
  it('sends the viewer header and turns ProblemDetails into ApiError', async () => {
    const fetchImpl = vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Invalid cursor', detail: 'Start again.' }), { status: 400 }));
    const api = createApi('thandi', fetchImpl as unknown as typeof fetch);

    await expect(api.feed('bad')).rejects.toThrow('Start again.');
    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toBe('/api/feed?cursor=bad');
    expect(new Headers(init.headers).get('X-Member-Id')).toBe('thandi');
  });
});
