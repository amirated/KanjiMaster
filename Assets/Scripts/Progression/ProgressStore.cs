// RETIRED. ProgressStore's local JSON persistence has moved to
// LocalPlayerProgressStore (which implements IPlayerProgressStore), accessed via
// PlayerProgressService. That is now the single owner of the save file path and
// JSON details, and it adds schema versioning, migration, atomic writes, and
// corrupt-save recovery.
//
// The type has been removed to keep persistence details in exactly one place.
// File deletion was not available in this environment — this file is safe to
// delete manually in the Unity Editor.
namespace KanjiMaster.Progression
{
}
