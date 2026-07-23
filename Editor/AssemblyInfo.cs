// ==========================================
// AssemblyInfo (Editor)
// Lets the EditMode test assembly reach the
// editor-side helpers (the converter's pure logic,
// the settings keys) without exposing them as public
// API. Test-only; nothing here affects a player build.
// ==========================================
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AmirCollider.UnityDirectTMP.Editor.Tests")]
