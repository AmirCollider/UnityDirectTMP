// ==========================================
// DirectFontSettingsTests
// DirectFontSettings doubles as half of the font
// cache key, so its equality, hashing and clamping
// have to be exactly right - two "equal" settings
// must share an atlas, and a hand-edited value must
// never reach the FontEngine out of range. These are
// the tests that pin that down.
// ==========================================
using NUnit.Framework;
using UnityDirectTMP;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP.Editor.Tests
{
    internal sealed class DirectFontSettingsTests
    {
        [Test]
        public void Equal_Settings_Are_Equal_And_Share_A_HashCode()
        {
            var a = new DirectFontSettings(90, 9, 1024, 1024, GlyphRenderMode.SDFAA);
            var b = new DirectFontSettings(90, 9, 1024, 1024, GlyphRenderMode.SDFAA);

            Assert.IsTrue(a == b);
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Differing_Field_Makes_Settings_Unequal()
        {
            var a = new DirectFontSettings(90, 9, 1024, 1024, GlyphRenderMode.SDFAA);
            var bigger = new DirectFontSettings(120, 9, 1024, 1024, GlyphRenderMode.SDFAA);
            var padded = new DirectFontSettings(90, 12, 1024, 1024, GlyphRenderMode.SDFAA);

            Assert.IsTrue(a != bigger);
            Assert.IsTrue(a != padded);
        }

        [Test]
        public void Clamped_Forces_Values_Into_The_Safe_Range()
        {
            var wild = new DirectFontSettings(5, 999, 100, 100000, GlyphRenderMode.SDFAA);
            DirectFontSettings safe = wild.Clamped();

            Assert.AreEqual(DirectTMPConstants.MinSamplingPointSize, safe.SamplingPointSize);
            Assert.AreEqual(32, safe.AtlasPadding);
            Assert.AreEqual(DirectTMPConstants.MinAtlasDimension, safe.AtlasWidth);   // 100 -> 128 -> clamped up to 256
            Assert.AreEqual(DirectTMPConstants.MaxAtlasDimension, safe.AtlasHeight);  // 100000 -> clamped down to 8192
        }

        [Test]
        public void Clamped_Snaps_Atlas_Size_To_A_Power_Of_Two()
        {
            var odd = new DirectFontSettings(90, 9, 1000, 2000, GlyphRenderMode.SDFAA);
            DirectFontSettings safe = odd.Clamped();

            Assert.AreEqual(1024, safe.AtlasWidth);
            Assert.AreEqual(2048, safe.AtlasHeight);
        }

        [Test]
        public void Default_Matches_The_Package_Constants()
        {
            DirectFontSettings d = DirectFontSettings.Default;
            Assert.AreEqual(DirectTMPConstants.DefaultSamplingPointSize, d.SamplingPointSize);
            Assert.AreEqual(DirectTMPConstants.DefaultAtlasWidth, d.AtlasWidth);
            Assert.AreEqual(DirectTMPConstants.DefaultAtlasHeight, d.AtlasHeight);
            Assert.AreEqual(DirectTMPConstants.DefaultRenderMode, d.RenderMode);
        }
    }
}
