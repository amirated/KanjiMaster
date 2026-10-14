# Firebase authentication — architecture & phased plan (Phase B1)

Investigation + plan only. **No SDK is installed and no auth is implemented in this task.**
Guest stays 100% local (no Firebase, no anonymous auth). This complements
`docs/IDENTITY.md` (Phase A, already implemented).

## Current state (inspected)

- Unity **6** (URP 17.4, uGUI 2.0). `com.unity.androidjni` present (used by native Play
  Games calls). **No Firebase** packages, folders, or code anywhere. No other auth code.
- Project assemblies: `KanjiRush.Runtime` (all game code), `KanjiRush.Tests`,
  `LeanTween`. Persistence is local JSON files. Web app (`kanjimaster.loen.in`) is a
  **separate** application — not Unity, out of this repo.
- Phase A seam ready: `IdentityService.SetAuthenticated(userId, provider)` and the isolated
  `IdentityChoiceController.OnContinueWithAccount()` (currently a no-op). `PlayerIdentity`
  already has `state / guestId / userId / provider`.

## Canonical identity model

**Firebase UID is the canonical application player id.** Not the Play Games player id, not
the Game Center id, not an Apple id, not a Google email — those are provider identifiers
only (may be retained as metadata). On successful Firebase sign-in:
`IdentityService.SetAuthenticated(userId: firebaseUser.UserId, provider: "<providerName>")`.

Distinguish two very different things:
- **`FirebaseUser.UserId`** — the stable UID. Identity. Safe to persist in `PlayerIdentity`.
- **Firebase ID token** — a short-lived JWT (`user.TokenAsync(forceRefresh)`). This is the
  *credential* sent to the backend. **Never persist it; never treat the UID as a backend
  credential.** The backend verifies the ID token (Firebase Admin SDK) and extracts the UID.

## Key finding — Firebase never needs to run at Boot

Because Phase A persists auth **state locally** in `player_identity.json`, `BootLoader` can
route an authenticated returning user to Main Menu purely from local `PlayerIdentity`
(`IsAuthenticated`) — **no Firebase call at launch**. Firebase is only needed (a) during the
explicit sign-in action, and (b) when making backend/cloud calls (token refresh). Therefore:

- **Guest path stays entirely Firebase-free** (requirement satisfied structurally).
- **Firebase init is LAZY** — done on the first "Continue with Account", not in Boot. This
  avoids the Play-services dependency check and SDK cost for guests and at startup.
- `BootLoader` is **unchanged** by the whole auth effort.

## Shared architecture (the seam)

Keep Firebase out of the UI/identity domain; contain it in an auth layer + providers.

```
IdentityChoiceController        (UI — no Firebase types)
        ↓
AuthenticationService           (orchestration — app-level types; owns Firebase init + sign-in)
        ↓
IAuthenticationProvider         (per-platform credential acquisition; returns a Firebase Credential)
   ├── AndroidPlayGamesAuthProvider   (#if UNITY_ANDROID)
   ├── IosGameCenterAuthProvider      (#if UNITY_IOS)
   └── (Web providers live in the separate web app, not Unity)
        ↓
Firebase Authentication (SignInWithCredentialAsync → FirebaseUser)
        ↓
IdentityService.SetAuthenticated(firebaseUid, provider)   →  PlayerIdentity
```

- The UI calls `AuthenticationService.SignInWithPlatformAsync()` and gets back an app-level
  outcome (success / cancelled / failure / needs-linking) — **not** Firebase types.
- Providers acquire a platform credential (`PlayGamesAuthProvider.GetCredential(serverAuthCode)`,
  `GameCenterAuthProvider.GetCredential()`, etc.); `AuthenticationService` exchanges it via
  `FirebaseAuth.DefaultInstance.SignInWithCredentialAsync(credential)`.
- Namespaces consistent with the project: `KanjiMaster.Auth`. The current platform provider
  is selected at **compile time** (`#if UNITY_ANDROID / UNITY_IOS`), so exactly one provider
  is built per platform.

## Platform separation & build size (investigated)

Separate scenes do **not** by themselves reduce binary size (the task's caution is correct).
What actually reduces size / avoids cross-platform deps:

- **Native plugins are already platform-gated by Unity + EDM4U.** Firebase's External
  Dependency Manager resolves Android AARs (Gradle) and iOS pods (CocoaPods) per build
  target, and files under `Assets/Plugins/Android` / `Assets/Plugins/iOS` are auto-included
  only for that platform. So `firebase-auth` native libs and the **Google Play Games**
  Android plugin land only in Android builds; Game Center (GameKit) is iOS-only.
- **Managed code:** the Firebase C# assemblies (`Firebase.App`, `Firebase.Auth`) are
  cross-platform managed and compile into both builds; IL2CPP managed stripping trims unused
  code. To keep the *platform provider* managed code out of the wrong build, put each
  provider in a **platform-constrained asmdef** (Include Platforms = Android-only /
  iOS-only) and/or guard with `#if UNITY_ANDROID / UNITY_IOS`.
- **Scenes in Build Settings** are included regardless of platform unless you edit the scene
  list per target. So separate `AndroidAuthentication.unity` / `IOSAuthentication.unity`
  would each need a build preprocessor (`IPreprocessBuildWithReport`) to include only the
  relevant one — extra machinery for little gain, since Play Games / Game Center sign-in use
  **OS-native overlays** and need almost no custom Unity UI.

**Recommendation:** keep the **single `Identity` scene** (already built) and dispatch to the
compile-time platform provider from `AuthenticationService`. Do **not** add per-platform auth
scenes now — they don't reduce size and add build-list complexity. If a platform later needs
distinct in-Unity auth UI, add it then behind a platform asmdef. Real size control comes
from: platform asmdefs for providers, `Plugins/Android|iOS` for native libs, EDM4U, and
IL2CPP stripping.

## Firebase config artifacts & security

| Artifact | Where | Committable? |
|---|---|---|
| `google-services.json` (Android) | `Assets/` | Client config, **not a secret**. Safe in the build. Committable; many teams gitignore it and inject in CI for hygiene. |
| `GoogleService-Info.plist` (iOS) | `Assets/` | Same as above — client config, safe in build. |
| App **SHA-1 / SHA-256** signing fingerprints | Firebase Console (Android app) | Not in source. Required for Play Games / Google sign-in. |
| OAuth **client secrets**, Firebase **service-account JSON**, Apple **.p8** keys (Sign in with Apple / Game Center), Play Games service credentials, backend API keys | Console + backend secret manager | **SECRET — never commit, never in the Unity client.** |

Do not put secrets in source or create fake config files. The Android API key inside
`google-services.json` is a *restricted* key (bound by app signature + Firebase rules), not
an OAuth secret.

## Account linking (documented, not implemented)

Firebase links multiple provider credentials to one UID via
`user.LinkWithCredentialAsync(credential)`.

- **Cross-platform reality:** Play Games (Android) and Game Center (iOS) are platform-bound.
  The same human signing in with Play Games on Android and Game Center on iOS gets **two
  different Firebase UIDs** unless linked. A common **Google and/or Apple** provider
  (available on Android, iOS, and web) should be the cross-platform anchor; Play Games /
  Game Center are linked onto that account for convenience.
- **Edge case — credential already in use:** `LinkWithCredential` fails
  (`credential-already-in-use` / `email-already-in-use`) when the provider is attached to a
  *different* Firebase user. **Never silently merge two Firebase users.** Instead surface an
  explicit **"Sign in to existing account"** flow (sign in *with* that credential → switch to
  that UID), and later decide the progress reconciliation policy.
- **Combinations to support later:** {Google, Apple} as anchors + {Play Games, Game Center}
  linked per platform. Implement linking flows and the "existing account" branch in Phase B6.

## RevenueCat (placement only)

Use the **Firebase UID as the RevenueCat App User ID** (`Purchases.configure(appUserID: uid)`).
Architectural home: a future `PurchasesService` initialized **after** authentication with the
Firebase UID. Guests use RevenueCat's anonymous app-user-id; on authenticating, alias/login to
the Firebase UID. Not installed here (Phase B7).

## Backend / DynamoDB boundary (future, documented only)

```
Unity/Web → Firebase Auth → Firebase ID token → Backend API
   → verify ID token (Firebase Admin) → extract UID → DynamoDB (PlayerProgress)
```

The client never accesses DynamoDB with broad credentials. **Guest → authenticated:** when a
guest authenticates, the local `PlayerProgress` is uploaded/associated to the new Firebase UID
via the backend (the local `guestId` can correlate the pre-auth session); the conflict policy
(existing cloud progress vs local guest progress) is a later decision. Not implemented now.

## Phased implementation plan

Each phase lists: create/modify · Unity Editor · Firebase Console · Play Console · Apple ·
deps · tests · platform notes.

### B2 — Firebase Auth foundation (platform-agnostic seam)
- **Create:** `Auth/AuthenticationService.cs`, `Auth/IAuthenticationProvider.cs`,
  `Auth/AuthOutcome.cs` (success/cancelled/failed/needs-linking + firebase uid), `Auth/FirebaseInitializer.cs`
  (lazy `CheckAndFixDependenciesAsync`), an `Auth/KanjiMaster.Auth.asmdef` referencing the
  Firebase managed assemblies. Wire `IdentityChoiceController.OnContinueWithAccount` → `AuthenticationService`.
- **Modify:** `IdentityChoiceController` (call the service; still no fake success). Possibly a
  small `PlayerIdentity` note (keep `userId`=UID, `provider`); no domain change needed.
- **Unity Editor:** import Firebase App + Auth SDK (`.unitypackage` or UPM), let EDM4U resolve.
- **Firebase Console:** create the Firebase project + register Android & iOS apps; download
  `google-services.json` / `GoogleService-Info.plist`.
- **Deps:** Firebase Unity SDK (App, Auth), EDM4U.
- **Tests:** `AuthenticationService` state machine with a **fake `IAuthenticationProvider`**
  (no real Firebase) — success/cancel/failure → correct `IdentityService.SetAuthenticated`
  call / no-op; guest path never touches auth.
- **Platform:** none yet (providers are stubs returning "not available").

### B3 — Android: Google Play Games → Firebase
- **Create:** `Auth/AndroidPlayGamesAuthProvider.cs` (`#if UNITY_ANDROID`, own Android-only
  asmdef). **Editor:** import Google Play Games plugin for Unity into `Plugins/Android`.
- **Firebase Console:** enable **Play Games** sign-in provider; add the Play Games OAuth
  client. **Play Console:** configure Play Games Services, link the OAuth client, add the app
  SHA-1/256. **Apple:** none.
- **Tests:** provider seam tested via fake; native flow verified manually on device.
- **Platform:** Android only; native plugin gated to `Plugins/Android` + platform asmdef.

### B4 — iOS: Game Center → Firebase
- **Create:** `Auth/IosGameCenterAuthProvider.cs` (`#if UNITY_IOS`, iOS-only asmdef; uses
  `GameCenterAuthProvider.GetCredential`). **Firebase Console:** enable **Game Center** and/or
  **Apple** provider. **Apple:** enable Game Center capability, Sign in with Apple key (.p8) as
  needed; App Store Connect config. **Play Console:** none.
- **Tests:** fake-provider seam; device verification manual.
- **Platform:** iOS only.

### B5 — Web: Google / Apple → Firebase
- **In the separate web app** (`kanjimaster.loen.in`), not this repo: Firebase JS SDK with
  Google + Apple providers → same Firebase project → same UID. **Firebase Console:** enable
  Google + Apple web providers, authorized domains. **Apple:** Services ID + Sign in with
  Apple for web. No Unity changes.

### B6 — Account linking
- **Create:** linking methods in `AuthenticationService` (`LinkWithCredentialAsync`), the
  **"Sign in to existing account"** branch for `credential-already-in-use`. Document/implement
  Google/Apple as cross-platform anchor + Play Games/Game Center linked. **No silent merges.**
- **Tests:** linking state machine (already-linked, in-use-elsewhere, success) via fakes.

### B7 — RevenueCat identity integration
- **Create:** `PurchasesService` initialized post-auth with the Firebase UID as App User ID;
  guest→auth alias. Install RevenueCat then. (Separate task.)

### B8 — Backend authentication boundary
- Backend API verifies Firebase ID tokens (Firebase Admin), extracts UID. Client sends
  `user.TokenAsync()` in an Authorization header. No DynamoDB yet.

### B9 — DynamoDB cloud PlayerProgress + guest→cloud migration
- Backend persists `PlayerProgress` under the UID in DynamoDB; conflict/merge policy; guest
  progress association on first authentication. Client stays behind the backend boundary.

## Assumptions / risks

- **Firebase Unity SDK ↔ Unity 6 support:** verify the current Firebase Unity SDK release
  supports Unity 6 at B2 time and pin the version (assumption/risk).
- **Play Games ↔ Firebase in Unity:** confirm the current supported plugin/flow (server auth
  code → `PlayGamesAuthProvider`) against current Firebase docs at B3.
- **Cross-platform single account** requires a Google/Apple anchor; Play Games/Game Center
  alone won't unify Android+iOS — plan the linking UX (B6) accordingly.
- Keep `google-services.json`/`plist` as client config (safe); keep all true secrets in
  consoles/backend, never in the client.

## Recommended next task

**Phase B2 — Firebase Authentication foundation (platform-agnostic):** install the Firebase
App + Auth SDK, create the `KanjiMaster.Auth` layer (`AuthenticationService`,
`IAuthenticationProvider`, `AuthOutcome`, lazy `FirebaseInitializer`) with its own asmdef,
wire `IdentityChoiceController.OnContinueWithAccount` → `AuthenticationService`, and test the
orchestration with a **fake provider** (no real platform sign-in yet). This connects the Phase
A seam to Firebase without any platform SDK, keeps guest fully local, and leaves Boot
unchanged — the smallest safe step before the Android (B3) and iOS (B4) provider work.
