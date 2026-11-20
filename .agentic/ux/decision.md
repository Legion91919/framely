# Decision: home feed (slice 1 key screen)

Reviewed in headless Chrome at 1440px and 390px (`screens/home-feed-a|b|c.html`).

| Variant | Main job speed | Clarity | Accessibility | Tone | Total |
|---|---|---|---|---|---|
| A. Contact sheet (dense 4-col grid, keyboard, compose in top bar) | 3: fast to scan, but square crops hide the photographer's framing and captions are truncated | 3 | 4: arrow-key grid, but tiny meta text and crops lose alt context | 3: feels like a picker, not a gallery | 13 |
| B. Gallery wall (one column of full-ratio cards, follow rail, compose prompt + mobile button) | 5: newest first, one thumb-scroll, compose always one tap away | 5: author, place, caption, time in a fixed order | 4: semantic articles, real alt text, 44px targets | 5: quiet, photo is the loudest thing | 19 |
| C. Catch-up queue (one frame at a time, J/K, docked 3-step composer) | 3: great for "what's new", slow for browsing older work | 4 | 4: good live region, but hidden queue on mobile | 4: calm, a bit like a slideshow | 15 |

## Pick: B, with two borrowed ideas
- **From C:** a small "N new frames since this morning" strip at the top of the feed (sets the expectation that newest is first).
- **From A:** the `N` keyboard shortcut for compose on desktop.
- Compose moves into a modal dialog (native `<dialog>`) so the feed keeps its scroll position.
- Rejected: square crops (A) and one-at-a-time paging (C); both fight the "see the photographer's framing" job.

Final: `screens/home-feed.html` (state switcher for feed loading/empty/error/success and compose idle/uploading/rejected/posted).
