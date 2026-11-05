using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class StagePresentationTests
    {
        [UnityTest]
        public IEnumerator CompleteFrameFitsPortraitLandscapeAndNarrowViews()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            var camera=Camera.main; var rig=camera.GetComponent<BoardCamera>();
            var background=GameObject.Find("Game/Background").GetComponent<SpriteRenderer>();
            var stage=new Vector2(9.4f,9.4f*1672/941);
            var oldTarget=camera.targetTexture;
            var target=new RenderTexture(540,960,0);
            camera.targetTexture=target;
            try
            {
                foreach(float aspect in new[]{540f/960,1280f/720,0.3f})
                {
                    camera.aspect=aspect; rig.Synchronize(); yield return null;
                    Assert.AreEqual(Mathf.Max(stage.y/2,stage.x/(2*aspect)),camera.orthographicSize,0.0001f);
                    Assert.AreEqual(stage.x,background.bounds.size.x,0.0001f);
                    Assert.AreEqual(stage.y,background.bounds.size.y,0.0001f);
                    foreach(var corner in new[]{background.bounds.min,background.bounds.max})
                    {
                        var point=camera.WorldToViewportPoint(corner);
                        Assert.That(point.x,Is.InRange(-0.0001f,1.0001f)); Assert.That(point.y,Is.InRange(-0.0001f,1.0001f));
                    }
                    Assert.Less(background.bounds.min.x,0); Assert.Greater(background.bounds.max.x,8);
                    Assert.Less(background.bounds.min.y,0); Assert.Greater(background.bounds.max.y,9);
                    var aim=Object.FindFirstObjectByType<AimInput>();
                    Assert.IsFalse(aim.TryPointerDirection(camera.WorldToScreenPoint(new Vector3(-1,4,0)),out _),"Decorative margins cannot fire");
                }
                foreach(string name in new[]{"Left","Right","Bottom"}) Assert.IsTrue(GameObject.Find("Game/"+name).GetComponent<BoxCollider2D>().enabled);
                Assert.AreEqual(new Vector3(4,9,0),GameObject.Find("Game/Launcher").transform.position);
            }
            finally { camera.targetTexture=oldTarget;Object.Destroy(target);camera.ResetAspect(); rig.Synchronize(); }
        }
        [UnityTest]
        public IEnumerator PointerSamplingSynchronizesProjectionInResizeFrame()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            var camera=Camera.main;var rig=camera.GetComponent<BoardCamera>();
            // Fractional Editor GameView pixels (DPI scaling) round ScreenToWorldPoint differently.
            // Use an integer-sized render surface, just like an actual Player backbuffer.
            var oldTarget=camera.targetTexture;
            var target=new RenderTexture(300,1000,0);camera.targetTexture=target;
            try
            {
                camera.aspect=1280f/720; rig.Synchronize();
                camera.aspect=0.3f; // No yield: the sampling method must synchronize before conversion.
                float half=9.4f/(2*camera.aspect); var rect=camera.pixelRect;var world=new Vector2(1,2);
                var screen=new Vector2(rect.center.x+(world.x-4)/(2*half*camera.aspect)*rect.width,rect.center.y+(world.y-4.8f)/(2*half)*rect.height);
                var aim=Object.FindFirstObjectByType<AimInput>();
                Assert.IsTrue(aim.TryPointerDirection(screen,out var actual));
                Assert.IsTrue(AimInput.TryDirection(world,75,out var expected));
                Assert.Less(Vector2.Distance(expected,actual),0.0001f);
            }
            finally { camera.targetTexture=oldTarget;Object.Destroy(target);camera.ResetAspect(); rig.Synchronize(); }
        }
    }
}
