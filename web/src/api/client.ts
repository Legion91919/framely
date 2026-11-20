import type { DemoViewer, FeedItem, FeedPage, Me, Member, Problem } from './types';

export class ApiError extends Error {
  constructor(public readonly status: number, public readonly problem: Problem) {
    super(problem.detail ?? problem.title ?? `Request failed (${status})`);
  }
}

export interface FramelyApi {
  feed(cursor?: string | null): Promise<FeedPage>;
  members(): Promise<Member[]>;
  me(): Promise<Me>;
  follow(memberId: string): Promise<void>;
  unfollow(memberId: string): Promise<void>;
  post(photo: File, caption: string, altText: string, idempotencyKey: string): Promise<FeedItem>;
  demoViewers(): Promise<DemoViewer[]>;
}

type Fetch = typeof fetch;

/** Typed client for the Framely API. Identity is the demo X-Member-Id header until the auth slice. */
export function createApi(viewerId: string | null, fetchImpl: Fetch = (...a) => fetch(...a)): FramelyApi {
  async function call<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    if (viewerId) headers.set('X-Member-Id', viewerId);
    const res = await fetchImpl(path, { ...init, headers });
    if (!res.ok) {
      let problem: Problem = { status: res.status };
      try {
        problem = (await res.json()) as Problem;
      } catch {
        /* non-JSON error body */
      }
      throw new ApiError(res.status, problem);
    }
    return res.status === 204 ? (undefined as T) : ((await res.json()) as T);
  }

  return {
    feed: (cursor) => call<FeedPage>(cursor ? `/api/feed?cursor=${encodeURIComponent(cursor)}` : '/api/feed'),
    members: () => call<Member[]>('/api/members'),
    me: () => call<Me>('/api/me'),
    follow: (id) => call<void>(`/api/me/following/${id}`, { method: 'PUT' }),
    unfollow: (id) => call<void>(`/api/me/following/${id}`, { method: 'DELETE' }),
    post: (photo, caption, altText, idempotencyKey) => {
      const form = new FormData();
      form.append('photo', photo);
      form.append('caption', caption);
      form.append('altText', altText);
      return call<FeedItem>('/api/posts', { method: 'POST', body: form, headers: { 'Idempotency-Key': idempotencyKey } });
    },
    demoViewers: () => call<DemoViewer[]>('/api/demo/viewers'),
  };
}
