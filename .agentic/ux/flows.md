# User flows

## Flow 1: post a photo and followers see it first (slice 1)
1. Photographer opens the home feed: `screens/home-feed.html` → `GET /api/feed` (25 per page, newest first).
2. Taps "Post a photo" (or presses `N`): `ComposeDialog` in `screens/home-feed.html`.
3. Chooses a JPEG, writes a caption and alt text, taps "Post to followers" → `POST /api/posts` (multipart/form-data: `photo`, `caption`, `altText`; header `Idempotency-Key`).
4. API checks size and type, strips EXIF (APP1) unless location sharing is on (`PUT /api/me/settings/location-sharing`), stores the file (served from `/media/...`), fans the post out to every follower's pre-built feed.
5. A follower opens the home feed: `screens/home-feed.html` → `GET /api/feed`; the new post is first. A non-follower never sees it.
6. Follower scrolls to the end and taps "Show older frames" → `GET /api/feed?cursor=<nextCursor>`.

## Flow 2: follow a photographer
1. Home feed `FollowRail` (`screens/home-feed.html`) → `GET /api/members` (with `isFollowing`).
2. Tap "Follow" → `PUT /api/me/following/{memberId}` (idempotent); their recent posts are back-filled into the viewer's feed; the followed member gets a notification (`GET /api/me/notifications`).
3. Tap "Following" to unfollow → `DELETE /api/me/following/{memberId}`; their posts leave the viewer's feed.
