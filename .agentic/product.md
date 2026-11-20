# Product brief

> **Showcase project.** Lumen Collective (fictional), a network of 4,000 South African photographers is a fictional client and every name, user and number here is invented.
> Framely is an original photo-sharing social app, built to show how the Baton model handles a consumer-scale product.

## Client
Lumen Collective (fictional), a network of 4,000 South African photographers

## Problem
Local photographers have no photo-first community of their own: big platforms bury their work under ads and video, and they can't see who actually follows their craft.

## Users
Photographers (daily, mobile-first, posting and browsing); followers (daily, browsing only).

## Outcomes
Home feed opens in under 1 second on 4G; new posts appear in followers' feeds within 5 seconds; image bandwidth served from a CDN, not the API.

## Constraints
POPIA: location stripped from EXIF unless the user opts in; delete-my-account removes photos within 30 days.

## Out of scope (first release)
Anything not needed for the outcomes above in the first three slices.

## Traits (for the architecture advisor)
- MediaDelivery: "every post is one or more full-resolution photos"
- ReadHeavy: "members scroll feeds ~50x more often than they post"
- PersonalData: "profiles, follower lists and location tags"
- SeveralBusinessAreas: "media, social graph, feed, notifications"
- SmallTeam: "three engineers for the launch"

Global-scale profile used for the evolution plan: MediaDelivery, ReadHeavy, PersonalData, SeveralBusinessAreas, GlobalScale.
