// ==========================================
// AssemblyInfo
// Exposes this runtime assembly's internal types
// to the EditMode test assembly only. The public
// surface of Unity DirectTMP is deliberately small
// - DirectFont, DirectTMP and a couple of enums -
// while the moving parts (the cache key, the
// fallback chain, the path helpers) stay `internal`.
// The tests need to reach those internals, so this
// single grant lets them, without widening the API
// that ships to users. The test assembly is
// Editor-only and gated behind UNITY_INCLUDE_TESTS,
// so this never affects a player build.
// ==========================================
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AmirCollider.UnityDirectTMP.Editor")]
[assembly: InternalsVisibleTo("AmirCollider.UnityDirectTMP.Editor.Tests")]
