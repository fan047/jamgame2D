using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Holedown
{
    // Reads the configuration that ships inside the build (Android StreamingAssets). The files
    // are produced by the Android build from the project's Config, so the phone never depends
    // on an external directory and never falls back to a single built-in stage.
    public static class BundledConfiguration
    {
        public const string Folder = "Config";
        public const string SettingsFile = "settings.json";
        public const string LevelFile = "level.json";
        // Test seam: a local folder that stands in for Assets/StreamingAssets.
        public static string RootOverrideForTests { get; set; }

        public static string Root => RootOverrideForTests ?? System.IO.Path.Combine(Application.streamingAssetsPath, Folder);

        public static IEnumerator Read(Action<string> onSettings, Action<string> onLevel, Action<string> onError)
        {
            string settings = null, level = null, error = null;
            yield return ReadFile(SettingsFile, text => settings = text, message => error = message);
            if (error == null) yield return ReadFile(LevelFile, text => level = text, message => error = message);
            if (error != null) { onError?.Invoke(error); yield break; }
            onSettings?.Invoke(settings);
            onLevel?.Invoke(level);
        }

        static string Uri(string root, string file) => System.IO.Path.Combine(root, file);

        static IEnumerator ReadFile(string file, Action<string> onText, Action<string> onError)
        {
            string path = Uri(Root, file);
            // On device the path already points inside the APK; on desktop a plain path needs a
            // file:// prefix before UnityWebRequest can open it.
            string url = path.Contains("://") ? path : "file://" + path.Replace('\\', '/');
            using (var request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"{Folder}/{file}: {request.error ?? "read failed"}");
                    yield break;
                }
                onText?.Invoke(request.downloadHandler.text);
            }
        }
    }
}
