// ==========================================
// DirectFontFallbackTests
// The fallback chain is "ordered by you", so order
// and de-duplication are the whole contract: the
// per-character search order TMP ends up using is
// exactly what these tests assert, and a font is
// never listed as its own fallback.
// ==========================================
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityDirectTMP;

namespace UnityDirectTMP.Editor.Tests
{
    internal sealed class DirectFontFallbackTests
    {
        private readonly List<TMP_FontAsset> spawned = new List<TMP_FontAsset>();

        private TMP_FontAsset NewAsset(string name)
        {
            var asset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            asset.name = name;
            spawned.Add(asset);
            return asset;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (TMP_FontAsset a in spawned)
            {
                if (a != null) { Object.DestroyImmediate(a); }
            }
            spawned.Clear();
        }

        [Test]
        public void OrderAndDedupe_Drops_Nulls_And_Duplicates_Preserving_Order()
        {
            TMP_FontAsset a = NewAsset("A");
            TMP_FontAsset b = NewAsset("B");
            TMP_FontAsset c = NewAsset("C");

            var input = new List<TMP_FontAsset> { a, null, b, a, c, b };
            List<TMP_FontAsset> result = DirectFontFallback.OrderAndDedupe(input);

            Assert.AreEqual(3, result.Count);
            Assert.AreSame(a, result[0]);
            Assert.AreSame(b, result[1]);
            Assert.AreSame(c, result[2]);
        }

        [Test]
        public void OrderAndDedupe_Handles_Null_Input()
        {
            List<TMP_FontAsset> result = DirectFontFallback.OrderAndDedupe(null);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Apply_Installs_Table_Without_The_Primary_Or_Duplicates()
        {
            TMP_FontAsset primary = NewAsset("Primary");
            TMP_FontAsset a = NewAsset("A");
            TMP_FontAsset b = NewAsset("B");

            DirectFontFallback.Apply(primary, new List<TMP_FontAsset> { a, primary, b, a });

            Assert.IsNotNull(primary.fallbackFontAssetTable);
            Assert.AreEqual(2, primary.fallbackFontAssetTable.Count);
            Assert.AreSame(a, primary.fallbackFontAssetTable[0]);
            Assert.AreSame(b, primary.fallbackFontAssetTable[1]);
            CollectionAssert.DoesNotContain(primary.fallbackFontAssetTable, primary);
        }
    }
}
