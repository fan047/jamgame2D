#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Holedown.Tests
{
    public sealed class BackgroundEdgeTests
    {
        const string Path = "Assets/Holedown/Art/Theme/background_game.png";
        Texture2D image;
        Color32[] pixels;

        [SetUp] public void Load()
        {
            image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(ImageConversion.LoadImage(image, File.ReadAllBytes(Path)));
            pixels = image.GetPixels32();
        }
        [TearDown] public void Cleanup() { if (image) Object.DestroyImmediate(image); }
        Color32 At(int x, int yFromTop) => pixels[(image.height - 1 - yFromTop) * image.width + x];
        static int RGB(Color32 p) => p.r << 16 | p.g << 8 | p.b;

        [Test] public void NativeDimensionsOpaqueAlphaAndPointImportArePreserved()
        {
            Assert.AreEqual(941, image.width); Assert.AreEqual(1672, image.height);
            Assert.IsTrue(System.Array.TrueForAll(pixels, p => p.a == 255), "all source pixels remain opaque");
            var importer = (TextureImporter)AssetImporter.GetAtPath(Path);
            Assert.AreEqual(FilterMode.Point, importer.filterMode);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        }

        [Test] public void QuietMiddleMatchesThePreRepairGradient()
        {
            Assert.AreEqual(new Color32(25,33,66,255), At(470,200));
            Assert.AreEqual(new Color32(24,33,65,255), At(470,835));
            Assert.AreEqual(new Color32(25,33,66,255), At(200,500));
            Assert.AreEqual(new Color32(24,32,64,255), At(700,1200));
        }

        [Test] public void PlaqueIsGoneAndOuterPixelsAreNotAUniformFeather()
        {
            for (int y = 0; y < 100; y++)
                for (int x = 350; x < 595; x++)
                {
                    var p = At(x,y);
                    Assert.Less(p.r, 40); Assert.Less(p.g, 50); Assert.Less(p.b, 100);
                }
            var colors = new HashSet<int>();
            for (int y = 0; y < image.height; y++) { colors.Add(RGB(At(0,y))); colors.Add(RGB(At(940,y))); }
            for (int x = 0; x < image.width; x++) { colors.Add(RGB(At(x,0))); colors.Add(RGB(At(x,1671))); }
            Assert.Greater(colors.Count, 100, "the old 34px uniform-color feather must not return");
            // These are unchanged source-derived hard edge colors, not blended additions.
            Assert.AreEqual(new Color32(25,33,68,255), At(0,500));
            Assert.AreEqual(new Color32(31,40,84,255), At(940,500));
            Assert.AreEqual(new Color32(24,31,63,255), At(470,0));
        }
    }
}
#endif
