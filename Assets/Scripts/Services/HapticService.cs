using KanjiMaster.Settings;
using UnityEngine;

namespace KanjiMaster.Services
{
    /// <summary>
    /// Minimal, reusable haptic feedback, gated by the Haptics setting. Call
    /// <see cref="Light"/> from clearly-defined interactions (currently the button press).
    /// It never fires when the setting is off, and is a safe no-op on platforms without a
    /// supported path (Editor, iOS for now, Standalone/WebGL).
    ///
    /// Android uses a short one-shot vibration via the system Vibrator; API 26+ uses a light
    /// VibrationEffect, older devices fall back to a brief vibrate(ms).
    /// </summary>
    public static class HapticService
    {
        public static bool Enabled => SettingsService.Default.HapticsEnabled;

        /// <summary>A short, light tap — e.g. on a button press.</summary>
        public static void Light() => Vibrate(20);

        /// <summary>A slightly firmer tap for a positive event — e.g. a correct answer.</summary>
        public static void Success() => Vibrate(35);

        /// <summary>A longer buzz for a negative event — e.g. a wrong answer / timeout.</summary>
        public static void Failure() => Vibrate(70);

        private static void Vibrate(long milliseconds)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidVibrate(milliseconds);
#endif
            // iOS Taptic needs a native plugin (none installed) → intentionally no-op for v1.
            // Editor / Standalone / WebGL → no-op.
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void AndroidVibrate(long milliseconds)
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;

                    int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                    if (sdk >= 26)
                    {
                        using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        {
                            // DEFAULT_AMPLITUDE = -1 → let the device pick a light amplitude.
                            var effect = effectClass.CallStatic<AndroidJavaObject>(
                                "createOneShot", milliseconds, -1);
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        vibrator.Call("vibrate", milliseconds); // deprecated path for < API 26
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Haptics] Vibrate failed (non-fatal): {e.Message}");
            }
        }
#endif
    }
}
