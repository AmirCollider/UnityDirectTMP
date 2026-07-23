// ==========================================
// DirectTMPPathTests
// The pure string / hashing helpers that the loader
// and the cache lean on. Keeping these correct is
// what lets "the same font, requested two ways" map
// to one cached atlas, and what keeps a spooled font
// file's name stable across runs.
// ==========================================
using NUnit.Framework;
using System.Text;
using UnityDirectTMP;
using UnityEngine.TextCore.LowLevel;

namespace UnityDirectTMP.Editor.Tests
{
    internal sealed class DirectTMPPathTests
    {
        [Test]
        public void CombineUnder_Joins_With_Forward_Slashes_And_Trims_Leading_Slash()
        {
            Assert.AreEqual("/root/dir/sub/font.ttf", DirectFontLoader.CombineUnder("/root/dir", "sub/font.ttf"));
            Assert.AreEqual("/root/dir/font.ttf", DirectFontLoader.CombineUnder("/root/dir/", "/font.ttf"));
            Assert.AreEqual("/root/dir/a/b.ttf", DirectFontLoader.CombineUnder("/root/dir", "a\\b.ttf"));
        }

        [Test]
        public void NormalizePath_Converts_Backslashes()
        {
            Assert.AreEqual("C:/Fonts/Vazirmatn.ttf", DirectFontKey.NormalizePath("C:\\Fonts\\Vazirmatn.ttf"));
            Assert.AreEqual(string.Empty, DirectFontKey.NormalizePath(null));
        }

        [Test]
        public void HashBytes_Is_Deterministic_For_The_Same_Content()
        {
            byte[] one = Encoding.UTF8.GetBytes("the quick brown fox");
            byte[] two = Encoding.UTF8.GetBytes("the quick brown fox");

            Assert.AreEqual(DirectFontFactory.HashBytes(one), DirectFontFactory.HashBytes(two));
        }

        [Test]
        public void HashBytes_Differs_For_Different_Content()
        {
            byte[] a = Encoding.UTF8.GetBytes("font-a");
            byte[] b = Encoding.UTF8.GetBytes("font-b");

            Assert.AreNotEqual(DirectFontFactory.HashBytes(a), DirectFontFactory.HashBytes(b));
        }

        [Test]
        public void ForBytes_Same_Content_And_Settings_Produce_Equal_Keys()
        {
            var settings = new DirectFontSettings(90, 9, 1024, 1024, GlyphRenderMode.SDFAA);
            byte[] bytes = Encoding.UTF8.GetBytes("a font buffer");

            DirectFontKey k1 = DirectFontKey.ForBytes(bytes, settings);
            DirectFontKey k2 = DirectFontKey.ForBytes((byte[])bytes.Clone(), settings);

            Assert.IsTrue(k1.Equals(k2));
            Assert.AreEqual(k1.GetHashCode(), k2.GetHashCode());
        }
    }
}
