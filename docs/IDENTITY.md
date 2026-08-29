# Identity & guest flow

**PlayerIdentity ≠ PlayerProgress.** Identity ("who / signed in?") is a separate concept
from progress ("what have they learned?"), persisted in its own file with its own version.

**Routing rule:** `authenticated → Main Menu`; `guest / unknown → Identity scene`.

## Status: Phase A implemented (Guest flow)

The Guest flow and identity architecture are implemented. Real authentication is **not**
integrated: "Continue with Account" is an isolated future entry point that does nothing
yet (it never authenticates, navigates, or falls back to guest). See "Phased plan" below.

Implemented: `PlayerIdentity` + `AuthState`, `IdentitySaveData` envelope,
`IPlayerIdentityStore` + `LocalPlayerIdentityStore` (`player_identity.json`),
`IdentityService` (`Default`, `ContinueAsGuest`, `SetAuthenticated`), `IdentityRouter`,
`IdentityChoiceController`, Boot routing via the identity gate, and `Identity` in
`SceneNames`/`SceneLoader`. Manual step remaining: author the `Identity.unity` scene (below).

### Manual Unity steps (Identity scene)

1. Create `Assets/Scenes/Identity.unity` (Canvas → Scale With Screen Size, 1080×1920,
   match 0.5, + EventSystem, + a `SafeAreaFitter` parent — like the other UI scenes).
2. Add two buttons ("Continue with Account", "Continue as Guest"), each with a TMP label
   and a TMP supporting-text line.
3. Add the **`IdentityChoiceController`** component and assign `accountButton`,
   `guestButton`, and (optional) the four TMP text slots. Copy is filled from the
   controller if the text slots are assigned.
4. **Build Settings → Scenes In Build:** add `Identity` (after Boot). File name must be
   exactly `Identity` (matches `SceneNames.Identity`).

## Phase B2 — Firebase Authentication foundation (code-side; SDK install pending)

The application authentication layer (`KanjiMaster.Auth`) is implemented and wired to
`IdentityChoiceController.OnContinueWithAccount`. It **compiles and runs without the
Firebase SDK** (Firebase calls are behind the `FIREBASE_AUTH` scripting define); until the
SDK is installed, a real provider exists, and the define is set, "Continue with Account"
reports **ProviderUnavailable** (never a fake success). Guest is unchanged and fully local.

Rules (for reference):
- **Firebase UID is the canonical player id** — stored in `PlayerIdentity.userId` on success
  (`provider` = one of `AuthProviders`). Provider-specific ids are not the app id.
- **Guest is completely local** — no Firebase init, no Firebase user, **no Firebase
  Anonymous Authentication** (intentionally never used).
- **Firebase init is lazy** — `FirebaseInitializer.EnsureReadyAsync` runs only when a provider
  exists and sign-in is requested; **never in Boot, never for guests**.
- **Platform providers are platform-specific** — Android Play Games (B3) and iOS Game Center
  (B4) are added behind `#if UNITY_ANDROID/UNITY_IOS` in platform-constrained assemblies.
- **Android ↔ iOS linking is out of scope** now. **Android ↔ Web** and **iOS ↔ Web** linking
  (via Google/Apple) are future requirements (B6).
- **Firebase ID tokens** are short-lived credentials for future backend APIs — **not
  persisted**, never stored in `PlayerIdentity`, never treated as the UID.
- **RevenueCat** will later use the Firebase UID as its App User ID (B7).
- **DynamoDB** will later be reached through a backend that verifies the Firebase ID token and
  extracts the UID (B8/B9). The client never accesses DynamoDB directly.

Architecture: `IdentityChoiceController → AuthenticationService → IAuthenticationProvider →
IFirebaseAuthenticator (FIREBASE_AUTH-gated) → Firebase → IdentityService.SetAuthenticated`.
The auth layer lives inside `KanjiRush.Runtime` in the `KanjiMaster.Auth` namespace (a
separate asmdef would create a circular dependency with `IdentityService`); Firebase
references are isolated to that namespace and gated by the define.

### Firebase setup — MANUAL steps still required (I cannot do these)

1. **Firebase Console:** create one Firebase project for KanjiMaster. Register an **Android
   app** and an **iOS app** using the project's **package name / bundle id** (see values
   needed below). Enable the intended sign-in providers later (Play Games in B3, Game
   Center/Apple in B4).
2. **Config files (client config, not secrets — safe in the build):** download
   `google-services.json` (Android) → `Assets/`, and `GoogleService-Info.plist` (iOS) →
   `Assets/`. Do **not** commit any OAuth client secrets, service-account JSON, or Apple `.p8`
   keys — those stay in the consoles / backend.
3. **Install the Firebase Unity SDK** — only **Firebase App** + **Firebase Auth** (via
   `.unitypackage` from the Firebase Unity SDK, or the Firebase UPM packages). Let EDM4U
   resolve native deps. Do not add Firestore/RTDB/Analytics/etc.
4. **Reference + enable:** ensure the Firebase assemblies are referenceable from the auth code
   (if they land as auto-referenced DLLs in `Assets/`, `KanjiRush.Runtime` picks them up
   automatically; if they ship as UPM asmdefs, add them to `KanjiRush.Runtime.asmdef`
   references). Then add **`FIREBASE_AUTH`** to Player → Scripting Define Symbols (Android +
   iOS). After that the gated Firebase code activates.
5. Confirm the Firebase Unity SDK version supports **Unity 6** and pin it.

### Values I need from you (do not invent)

- The final **Android package name** and **iOS bundle identifier** (currently only Standalone
  is set to `com.DefaultCompany.2D-URP`; Android/iOS default to `com.DefaultCompany.KanjiMaster`
  — set real ids before registering with Firebase, and set them consistently in Unity Player
  Settings).
- The **Firebase project** and its downloaded `google-services.json` / `GoogleService-Info.plist`.

---

Original plan (retained for reference):

## Current flow (inspected)

```
App launch → Boot scene → BootLoader.Start()
  progress = PlayerProgressService.Default.Current        (loads once; cached across scenes)
  destination = OnboardingRouter.StartDestination(OnboardingService.IsComplete(progress))
  SceneLoader.Load(destination)                            → Onboarding | MainMenu
```

- **Onboarding completion** lives in `PlayerProgress.Profile.hasCompletedOnboarding`
  (`OnboardingService`). New player → false → Onboarding; returning/grandfathered → MainMenu.
- **First launch** is detected implicitly: no save file → default progress
  (`hasCompletedOnboarding = false`). Existing pre-onboarding saves are grandfathered to
  completed on load.
- **Routing** is scene-per-screen via `SceneNames` / `SceneLoader.Load` (single choke
  point, with a re-entrancy guard). Scenes: Boot, Onboarding, MainMenu, Game, Results, Learn.
- **Persistence** (correction to the brief): progress is a **JSON file**
  (`Application.persistentDataPath/player_progress.json`), wrapped in a versioned `SaveData`
  envelope and written atomically by `LocalPlayerProgressStore` behind
  `IPlayerProgressStore` / `PlayerProgressService.Default`. It is **not** PlayerPrefs.
- **Profile model** `ProfileData` currently holds only `hasCompletedOnboarding`.
- **No identity/auth/account code exists** anywhere in the project.

Relevant files: `Core/BootLoader.cs`, `Services/OnboardingRouter.cs`, `SceneNames.cs`,
`SceneLoader.cs`, `Progression/OnboardingService.cs`, `Progression/PlayerProgress.cs`
(`ProfileData`), `Progression/IPlayerProgressStore.cs`, `LocalPlayerProgressStore.cs`,
`PlayerProgressService.cs`, `SaveData.cs` / `SaveDataMigration.cs`, `UI/OnboardingController.cs`.

## Recommended placement of the identity choice

A **dedicated `Identity` scene** (scene-per-screen, consistent with the rest of the app),
authored manually like Onboarding, with a small `IdentityChoiceController`. Not merged
into Onboarding (different lifecycle: onboarding is one-time; the identity choice recurs
every launch for non-authenticated players). Boot stays UI-less; MainMenu is not the host.
Alternative if fewer scenes are preferred: a panel/state inside Boot — not recommended,
as Boot currently has no UI.

## Minimal PlayerIdentity model (separate from PlayerProgress)

```csharp
public enum AuthState { Unknown = 0, Guest = 1, Authenticated = 2 }

[Serializable]
public class PlayerIdentity
{
    public int schemaVersion;   // own version, for future migration (start at 1)
    public AuthState state;     // Unknown (never chosen) / Guest / Authenticated
    public string guestId;      // stable local GUID, set on first guest continue (future linking)
    public string userId;       // set when Authenticated (future)
    public string provider;     // set when Authenticated (future): e.g. "google" / "apple"
    public bool IsAuthenticated => state == AuthState.Authenticated;
}
```

Supports Unknown/Guest/Authenticated, a future `userId` + single `provider`, and a
`guestId` to correlate pre-auth progress later. Deliberately no multi-provider or backend
structures yet.

## Persistence approach

A **separate identity store**, mirroring the progress store but in its own file:
`IPlayerIdentityStore` + `LocalPlayerIdentityStore` →
`Application.persistentDataPath/player_identity.json`, exposed via `IdentityService.Default`
(cached, settable — parallels `PlayerProgressService.Default`). Keeps identity fully
separate from `PlayerProgress` (different file, different lifecycle, own version) and is
backend-friendly (a future `RemoteIdentityStore` implements the same interface). Use the
JSON-file pattern, not a raw PlayerPrefs key, for consistency + versioning + swap-ability.

## Proposed routing

```
Boot:
  if IdentityService.Default.Current.IsAuthenticated → MainMenu
  else (Unknown or Guest)                            → Identity scene

Identity scene:
  Continue as Guest:
      IdentityService.ContinueAsGuest()   // state = Guest, ensure guestId, save (does NOT touch progress)
      → if !OnboardingService.IsComplete(progress)  → Onboarding   else → MainMenu
  Continue with Account:
      (Phase B) launch auth → on success IdentityService.SetAuthenticated(userId, provider) → save
      → onboarding-if-needed → MainMenu
      (Phase A: present as a clearly-marked "coming soon" / disabled action — never a dead end)
```

- **New player:** Identity choice → Guest → Onboarding (first time) → MainMenu.
- **Returning guest:** Identity choice → Guest → MainMenu (onboarding already complete).
- **Returning authenticated:** MainMenu directly (no identity choice, no onboarding).

The identity choice is the consistent post-Boot gate for non-authenticated players;
`OnboardingRouter`/`OnboardingService` become the "after identity" step (unchanged).
(Product alternative: run Onboarding before the identity choice for brand-new players —
easy to swap; identity-first is recommended for a single consistent gate.)

## Guest progress handling

Choosing Guest sets identity only — `PlayerProgress` is **never** reset, duplicated, or
migrated. Existing local progress persists and is used as-is; a returning guest re-picks
Guest and continues on the same progress.

## Future guest → authenticated transition (must remain possible)

On authentication, keep the existing local `PlayerProgress`, set `state = Authenticated` +
`userId` (+ `provider`). A later backend sync uploads/associates that local progress under
the account (`guestId` can correlate the pre-auth session). Because identity and progress
are separate objects in separate stores, this association is additive — nothing here
prevents it.

## Phased implementation plan

- **Phase A (next task):** `PlayerIdentity` + `IdentityService` + `LocalPlayerIdentityStore`
  + `IPlayerIdentityStore` + `IdentityRouter` (pure decision) + `Identity.unity` +
  `IdentityChoiceController` + Boot routing change + `SceneNames`/`SceneLoader` +
  Build Settings. Guest fully works; "Continue with Account" is a visible, non-dead-end
  stub. Tests for routing + identity persistence.
- **Phase B:** Integrate a real auth provider behind `IdentityService` (Account button
  launches it; `SetAuthenticated` on success). Provider chosen then — none now.
- **Phase C:** Backend/cloud sync + guest→account progress association.

## Files to create / modify (Phase A)

Create: `Progression/PlayerIdentity.cs`, `Progression/IdentityService.cs`,
`Progression/IPlayerIdentityStore.cs`, `Progression/LocalPlayerIdentityStore.cs`,
`Services/IdentityRouter.cs`, `UI/IdentityChoiceController.cs`, `Scenes/Identity.unity`
(manual authored), `Tests/IdentityTests.cs`.
Modify: `Core/BootLoader.cs` (identity gate → then onboarding step),
`Services/SceneNames.cs` (+`Identity`), `Services/SceneLoader.cs` (+`GoToIdentity`),
Build Settings (add Identity scene). `OnboardingController` stays as-is (still → MainMenu).

## Assumptions / risks

- The brief says PlayerPrefs; the project actually persists progress as a versioned JSON
  file — identity should follow the same JSON-file pattern (assumption/correction).
- Existing installs have no `player_identity.json` → identity defaults to `Unknown` → the
  identity choice appears once on next launch; picking Guest keeps all progress. Expected,
  not a bug.
- Showing the choice every launch to non-authenticated users can feel naggy; the specified
  gentle messaging mitigates it. A future "remember guest for N days" is supported by the
  model (add a timestamp) but is out of scope.
- "Continue with Account" has no provider in Phase A — it must be a clearly-marked
  "coming soon"/disabled action, never a dead end.
- Ordering (identity-first vs onboarding-first for brand-new players) is a product
  decision; flagged above.
```
