#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Holedown.Tests
{
    // The foreground layer is a run asset, so the checks read the PNG bytes directly
    // instead of relying on a readable import setting. It must be the real decoration art
    // at its original place: no pasted band, no filled middle, no wall across the field.
    public sealed class ForegroundAssetTests
    {
        const string Path_ = "Assets/Holedown/Art/Theme/foreground_game.png";
        const string BackgroundPath = "Assets/Holedown/Art/Theme/background_game.png";
        const float StageWidth = 9.4f, StageHeight = 16.702232f, CenterX = 4f, CenterY = 4.8f;
        static Texture2D Load(string path = Path_)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path + " must decode");
            return texture;
        }
        static int Px(float worldX) => Mathf.RoundToInt((worldX + 0.7f) * 941f / StageWidth);
        static Vector2Int Pixel(float worldX, float worldY) => new Vector2Int(Px(worldX),
            // Texture coordinates start at the bottom-left, while the art rows are measured
            // from the top.
            1672 - 1 - Mathf.RoundToInt((CenterY + StageHeight / 2 - worldY) * 1672f / StageHeight));
        [Test] public void ForegroundKeepsTheStageCanvasAndRealTransparency()
        {
            var texture = Load();
            try
            {
                Assert.AreEqual(941, texture.width); Assert.AreEqual(1672, texture.height);
                var pixels = texture.GetPixels32();
                int opaque = 0, clear = 0;
                foreach (var pixel in pixels) { if (pixel.a == 255) opaque++; else if (pixel.a == 0) clear++; }
                Assert.AreEqual(pixels.Length, opaque + clear, "no partial alpha: rocks are solid or absent");
                Assert.Greater(opaque, 0); Assert.Greater(clear, opaque, "the field stays mostly transparent");
            }
            finally { Object.DestroyImmediate(texture); }
        }
        [Test] public void EveryForegroundPixelIsTheOriginalDecorationArt()
        {
            var foreground = Load();
            var background = Load(BackgroundPath);
            try
            {
                Assert.AreEqual(background.width, foreground.width);
                Assert.AreEqual(background.height, foreground.height);
                var front = foreground.GetPixels32();
                var back = background.GetPixels32();
                int checkedPixels = 0;
                for (int i = 0; i < front.Length; i++)
                {
                    if (front[i].a == 0) continue;
                    checkedPixels++;
                    // Same pixel, same colour: the layer copies the art instead of painting a band.
                    Assert.AreEqual(back[i].r, front[i].r, "red channel drifted at pixel " + i);
                    Assert.AreEqual(back[i].g, front[i].g, "green channel drifted at pixel " + i);
                    Assert.AreEqual(back[i].b, front[i].b, "blue channel drifted at pixel " + i);
                }
                Assert.Greater(checkedPixels, 1000, "rocks and crystals are part of the layer");
            }
            finally { Object.DestroyImmediate(foreground); Object.DestroyImmediate(background); }
        }
        [Test] public void PlayFieldAndDecorationGapsStayClear()
        {
            var texture = Load();
            try
            {
                // Row 0 numbers and the whole play field above the floor line stay readable.
                foreach (float column in new[] { 0.5f, 1.5f, 3.5f, 4.5f, 7.5f })
                {
                    var rowZero = Pixel(column, 0.5f);
                    Assert.Less(texture.GetPixel(rowZero.x, rowZero.y).a, 0.01f, "row 0 stays clear at column " + column);
                }
                foreach (float y in new[] { 0.07f, 2.5f, 6f, 10f })
                {
                    var point = Pixel(4f, y);
                    Assert.Less(texture.GetPixel(point.x, point.y).a, 0.01f, "the play field above the floor is clear");
                }
                // Below the floor line the decorations are separate shapes with gaps: a wall
                // or a pasted band would cover this line almost end to end.
                foreach (float y in new[] { -0.3f, -0.6f, -1f })
                {
                    var line = Pixel(0f, y).y;
                    int covered = 0, longestClearRun = 0, run = 0;
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (texture.GetPixel(x, line).a > 0.5f) { covered++; run = 0; }
                        else { run++; longestClearRun = Mathf.Max(longestClearRun, run); }
                    }
                    Assert.Less(covered, texture.width * 0.4f, "no opaque band across the stage at y=" + y);
                    Assert.Greater(longestClearRun, 400, "decorations keep real gaps between them at y=" + y);
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }
        [Test] public void StandingCrystalsRemainInFrontOfTheFloor()
        {
            var texture = Load();
            try
            {
                bool crystal = false;
                for (int y = 0; y < texture.height && !crystal; y++)
                    for (int x = 0; x < texture.width; x++)
                    {
                        var c = texture.GetPixel(x, y);
                        if (c.a > 0.99f && c.g > c.r * 1.3f && c.g > 0.3f) { crystal = true; break; }
                    }
                Assert.IsTrue(crystal, "teal crystals are part of the foreground");
            }
            finally { Object.DestroyImmediate(texture); }
        }
        [Test] public void ForegroundImportsAsSinglePointSprite()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Path_);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.AreEqual(FilterMode.Point, importer.filterMode);
            Assert.AreEqual(256f, importer.spritePixelsPerUnit);
            Assert.IsFalse(importer.mipmapEnabled);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path_);
            Assert.AreEqual(new Rect(0, 0, 941, 1672), sprite.rect);
        }
    }
}
#endif
