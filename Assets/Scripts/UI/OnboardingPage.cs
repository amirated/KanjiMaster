using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanjiMaster.UI
{
    /// <summary>
    /// One onboarding card — plain serializable data so pages can be reordered, added,
    /// removed, retitled, or given an image entirely in the Inspector (or overridden by
    /// the built-in defaults) without touching the controller.
    /// </summary>
    [Serializable]
    public class OnboardingPage
    {
        [TextArea] public string title;
        [TextArea(2, 6)] public string body;
        public Sprite image;        // optional; the card hides its image slot when null
        public string ctaOverride;  // optional; used as the button label on the LAST page
    }

    /// <summary>
    /// The default onboarding content (5 short cards). Kept in one place so copy is easy
    /// to edit; the controller uses these only when no pages are assigned in the
    /// Inspector. Intentionally friendly and game-first — no JLPT/exam framing, no
    /// scoring/XP formulas. Only Rising Star and Young Master exist today, so the
    /// progression copy frames the six levels as the journey without implying access.
    /// </summary>
    public static class OnboardingPages
    {
        public static List<OnboardingPage> Default() => new List<OnboardingPage>
        {
            new OnboardingPage
            {
                title = "Learn Kanji by playing",
                body = "Build your kanji knowledge a little every day — by playing quick games, not cramming.",
            },
            new OnboardingPage
            {
                title = "How you play",
                body = "See a kanji, pick its meaning. Each game is a short 15-question session. Answer, learn, and improve as you go.",
            },
            new OnboardingPage
            {
                title = "Three ways to practice",
                body = "English — match a kanji to its meaning.\n" +
                       "Romaji — connect a kanji with its pronunciation.\n" +
                       "Kana — read kanji through Japanese script.\n\n" +
                       "Play with a timer for a challenge, or without to take your time.",
            },
            new OnboardingPage
            {
                title = "Explore and progress",
                body = "Browse kanji any time in the Learn section. Start as a Rising Star and keep practicing to rise toward Young Master, Adept, Expert, Master, and Legend.\n\n" +
                       "Keep learning to unlock new levels.",
            },
            new OnboardingPage
            {
                title = "Build your streak",
                body = "Practice every day to grow your daily streak and make learning a habit. Ready to begin?",
                ctaOverride = "Start Learning",
            },
        };
    }
}
