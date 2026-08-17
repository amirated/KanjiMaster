# First-time onboarding

A short, friendly first-run flow that shows a new player what the game is and how to
play, then drops them at the Main Menu. It is game-first (no JLPT/exam framing, no
scoring/XP formulas) and completes in a few taps.

## Flow

```
Boot
 └─ load PlayerProgress → OnboardingRouter.StartDestination(IsComplete?)
      ├─ incomplete → Onboarding scene → (Next…Next → CTA) → Main Menu
      └─ complete   → Main Menu
```

The Boot scene decides BEFORE any UI is shown, so a returning player never sees a flash
of onboarding.

## Completion state (in PlayerProgress)

`PlayerProgress.Profile.hasCompletedOnboarding` (bool). New players default to `false`.
Owned by `OnboardingService`, persisted with the normal versioned save — no separate
PlayerPrefs key, no second persistence system.

- `OnboardingService.IsComplete(p)` — read.
- `OnboardingService.CompleteAsGuest()` — mark complete + save (the guest completion).
- `OnboardingService.GrandfatherIfExistingPlayer(p)` — load-time migration (below).

## Existing-player migration (important)

Save schema is bumped 5 → 6 (additive: the new profile flag). On LOAD only (never on
fresh-default creation), `GrandfatherIfExistingPlayer` sets `hasCompletedOnboarding =
true` when the player already has **meaningful progress** (any completed session, any XP,
or any kanji mastery). So players who were already playing before onboarding existed are
**not** forced through it, and none of their data (mastery, levels, sessions, streak, XP,
profile) is changed. A brand-new player has no save and no progress, so they keep the
default `false` and see onboarding.

Edge case: an existing save that exists but has zero engagement (installed, never played)
is treated like a new player and will see onboarding — acceptable, since they never
started. This never un-sets a completed flag.

## Screens (data-driven, 5 cards)

Content is `OnboardingPage` data (`OnboardingPages.Default()`), each card one idea:

1. **Learn Kanji by playing** — what the app is.
2. **How you play** — see a kanji, pick its meaning; a game is a short 15-question session.
3. **Three ways to practice** — English / Romaji / Kana, with or without a timer.
4. **Explore and progress** — the Learn section; start as **Rising Star** and rise toward
   Young Master, Adept, Expert, Master, Legend ("keep learning to unlock new levels").
5. **Build your streak** — practice daily; final CTA **"Start Learning"**.

Only Rising Star and Young Master are implemented; the copy frames the six levels as the
journey without implying access to all of them.

## Adding / reordering pages

Pages are plain serializable data. Either edit `OnboardingPages.Default()` (code), or
fill/reorder the `pages` list on `OnboardingController` in the Inspector (title, body,
optional image, optional CTA label on the last page). Leaving the list empty uses the
defaults. No controller changes needed to add, remove, reorder, or retitle a card.

## Navigation

`OnboardingFlow` (pure, unit-tested) handles paging: Next advances, Back retreats, the
first page has no Back, and Next on the last page triggers completion. The final CTA:
1) `CompleteAsGuest()` marks complete and **persists**, then 2) navigates to Main Menu —
in that order (never navigate before persistence). Duplicate taps are ignored (a
`_completing` guard plus `SceneLoader`'s single-transition guard).

## Guest / account boundary (future)

`CompleteAsGuest()` is the local/guest completion path. A future **Profile Setup** step
("Continue as Guest" vs "Sign In / Create Account") can be inserted as a final onboarding
card or between onboarding and Main Menu, calling `CompleteAsGuest()` for the guest
branch — without changing gameplay. No authentication, backend, or cloud is implemented
here.

## Manual Unity setup (author the scene)

The scripts are ready; build the scene like the other authored scenes (Learn/Results):

1. **Create** `Assets/Scenes/Onboarding.unity` (match the existing scenes' folder).
2. Add a **Canvas** (Canvas Scaler → Scale With Screen Size, reference 1080×1920, match
   0.5) + an **EventSystem**, matching the other scenes' responsive setup.
3. Build a card: a **Title** (TMP), a **Body** (TMP, word-wrap), an optional **Image**
   inside an "ImageContainer" GameObject, an optional **page indicator** TMP ("1 / 5").
4. Add **Next** and **Back** buttons (each with a TMP label); the Next label is the CTA on
   the last page.
5. Add an empty GameObject with the **`OnboardingController`** component and assign:
   `titleText`, `bodyText`, `image`, `imageContainer`, `pageIndicatorText`, `nextButton`,
   `nextButtonLabel`, `backButton`. Leave `pages` empty to use defaults (or fill it).
6. **Build Settings → Scenes In Build:** add `Onboarding` (any order after Boot). Scene
   file name must be exactly `Onboarding` (matches `SceneNames.Onboarding`).
7. (Images optional — use existing placeholder sprites; illustration generation is a
   separate task.)

## Testing

`Assets/Scripts/Tests/OnboardingTests.cs`: new-player incomplete; Boot routing both ways;
`CompleteAsGuest` marks + persists + survives reload; completed state round-trips; no-save
stays incomplete; existing-player-with-progress grandfathered with all data intact;
zero-progress save not grandfathered; fresh default not grandfathered; flow Next/Back,
first-has-no-Back, last-signals-completion, single-page; default content is 3–5 cards with
a final CTA.
