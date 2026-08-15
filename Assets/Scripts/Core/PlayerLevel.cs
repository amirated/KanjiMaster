// PlayerLevel now lives in KanjiMaster.Progression (Assets/Scripts/Progression/PlayerLevel.cs),
// alongside the progression model that uses it. This Core copy was a duplicate and
// has been removed to resolve the ambiguous-reference error. (The unused Jlpt /
// TryGetKanjiLevel mapping helper that lived here was dropped; the JLPT mapping is
// documented on the enum itself. File deletion isn't available in this environment —
// this file is safe to delete manually in the Unity Editor.)
namespace KanjiMaster.Core
{
}
