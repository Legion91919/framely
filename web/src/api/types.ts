export interface Author {
  id: string;
  handle: string;
  displayName: string;
  city: string;
}

export interface FeedItem {
  id: string;
  author: Author;
  imageUrl: string;
  width: number;
  height: number;
  altText: string;
  caption: string;
  publishedAt: string;
}

export interface FeedPage {
  items: FeedItem[];
  nextCursor: string | null;
}

export interface Member {
  id: string;
  handle: string;
  displayName: string;
  city: string;
  followerCount: number;
  isFollowing: boolean;
}

export interface Me {
  id: string;
  handle: string;
  displayName: string;
  city: string;
  locationSharing: boolean;
  followerCount: number;
  followingCount: number;
}

export interface DemoViewer {
  id: string;
  handle: string;
  displayName: string;
}

/** RFC 9457 problem details as returned by the API. */
export interface Problem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
