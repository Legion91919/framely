# Components (home feed)

Build in this order. Every component uses token CSS variables only.

| Component | Props | Loading | Empty | Error | Success |
|---|---|---|---|---|---|
| `AppBar` | `viewer: Member \| null`, `members: Member[]`, `onViewerChange(id)`, `onCompose()` | logo only, no avatar | n/a | logo + "offline" text | logo, compose button (`N`), viewer switcher |
| `HomeFeed` | `viewerId`, `refreshKey` | two `PhotoCardSkeleton`s, `aria-busy=true` | "Your wall is empty" + find photographers | `role=alert` box with retry | `FreshStrip` + `PhotoCard` list + "Show older frames" (cursor) |
| `PhotoCard` | `post: FeedItem` (author, imageUrl, width, height, altText, caption, publishedAt) | `PhotoCardSkeleton` | n/a | image `onError` shows a neutral frame with alt text | photo at its own ratio, author row, caption, relative time |
| `FreshStrip` | `count`, `authors` | hidden | hidden | hidden | "N new frames from M photographers" |
| `FollowRail` | `viewerId`, `onChanged()` | three skeleton rows | "You follow everyone here" | inline error + retry | member rows with `FollowButton` |
| `FollowButton` | `member`, `following: boolean`, `onToggle()` | disabled "…" while saving | n/a | reverts and shows error text | "Follow" / "Following" (`aria-pressed`) |
| `ComposeDialog` | `open`, `onClose()`, `onPosted(post)` | "Posting…" (button disabled) | idle: choose a JPEG | rejected file / API ProblemDetails message | "Posted" then closes and refreshes feed |

Client-side checks in `ComposeDialog` mirror the API (JPEG only, max 10 MB, caption <= 500); the API stays the source of truth.
