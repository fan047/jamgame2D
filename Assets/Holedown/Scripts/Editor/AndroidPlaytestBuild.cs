using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Holedown.Editor
{
    // Reproducible Android playtest package: ARM64, IL2CPP, portrait, min API 25, target API 36,
    // the playtest bundle id and Unity's local debug signature. Each build lands in the next
    // free Builds/Android/NN folder, so earlier packages are never overwritten.
    public static class AndroidPlaytestBuild
    {
        public const string BundleId = "com.fan047.crystalmine.playtest";
        public const int MinimumApiLevel = 25;
        public const int TargetApiLevel = 36;
        public const string ApkName = "CrystalMinePlaytest.apk";

        [MenuItem("Holedown/Build Android Playtest")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new BuildFailedException("Exit Play Mode before building.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Android build support is missing.");
            string buildDirectory = NextBuildDirectory("Builds/Android");
            // The folder number is also the version code, so installing 02 over 01 is a plain
            // overwrite install and the device shows which package is running.
            int versionCode = int.Parse(Path.GetFileName(buildDirectory), System.Globalization.CultureInfo.InvariantCulture);
            string apk = Path.Combine(buildDirectory, ApkName);
            // The Android player settings are applied for this build only: the project file is
            // restored afterwards so unrelated local settings (runInBackground and friends)
            // never leak into the commit.
            byte[] projectSettings = File.ReadAllBytes(ProjectSettingsPath);
            var previous = AndroidSettings.Capture();
            // Burst's native linker needs a writable temporary directory on this workstation; the
            // Windows build redirects it for the same reason, so the Android build does too.
            string temp = Path.GetFullPath(Path.Combine("Library", "Locus", "tmp", "android-build-temp"));
            Directory.CreateDirectory(temp);
            string oldTemp = Environment.GetEnvironmentVariable("TEMP"), oldTmp = Environment.GetEnvironmentVariable("TMP");
            try
            {
                Environment.SetEnvironmentVariable("TEMP", temp);
                Environment.SetEnvironmentVariable("TMP", temp);
                ApplyAndroidPlayerSettings(versionCode);
                VerifyAndroidSettings(versionCode);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                    locationPathName = apk,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                var summary = report.summary;
                string message = $"result={summary.result}\nerrors={summary.totalErrors}\nwarnings={summary.totalWarnings}\nbytes={summary.totalSize}\nduration={summary.totalTime}\npath={summary.outputPath}\nversionCode={versionCode}";
                File.WriteAllText(Path.Combine(buildDirectory, "build-result.txt"), message);
                if (summary.result != BuildResult.Succeeded) throw new BuildFailedException(message);
                Debug.Log(message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("TEMP", oldTemp);
                Environment.SetEnvironmentVariable("TMP", oldTmp);
                // The setters change the Editor's live player settings; restoring the file alone
                // would leave IL2CPP, the playtest bundle id and Portrait in memory. Put every
                // touched value back through the same API, then make sure the file matches.
                previous.Restore();
                if (!File.Exists(ProjectSettingsPath) || !SameBytes(projectSettings, File.ReadAllBytes(ProjectSettingsPath)))
                {
                    File.WriteAllBytes(ProjectSettingsPath, projectSettings);
                    AssetDatabase.ImportAsset(ProjectSettingsAsset, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        // AssetDatabase wants the project-relative path; the byte copy uses the absolute one.
        const string ProjectSettingsAsset = "ProjectSettings/ProjectSettings.asset";
        static string ProjectSettingsPath => Path.GetFullPath(ProjectSettingsAsset);

        public static void ApplyAndroidPlayerSettings(int versionCode)
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinimumApiLevel;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetApiLevel;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = versionCode;
            PlayerSettings.Android.useCustomKeystore = false;   // Unity's local debug signature
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
        }

        // Prints and checks what the package will carry, so a wrong platform switch cannot
        // silently ship a debug-style or wrong-ABI build.
        public static void VerifyAndroidSettings(int versionCode)
        {
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != BundleId) throw new BuildFailedException("Android bundle id was not applied.");
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP) throw new BuildFailedException("Android scripting backend must be IL2CPP.");
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64) throw new BuildFailedException("Android target architectures must be ARM64 only.");
            if ((int)PlayerSettings.Android.minSdkVersion != MinimumApiLevel) throw new BuildFailedException("Android min API level mismatch.");
            if ((int)PlayerSettings.Android.targetSdkVersion != TargetApiLevel) throw new BuildFailedException("Android target API level mismatch.");
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait) throw new BuildFailedException("Android build must be portrait.");
            if (PlayerSettings.Android.bundleVersionCode != versionCode) throw new BuildFailedException("Android version code mismatch.");
            if (PlayerSettings.Android.useCustomKeystore) throw new BuildFailedException("The playtest must use Unity's debug signature.");
            Debug.Log($"Android playtest settings: {BundleId} v{PlayerSettings.bundleVersion}({versionCode}) min {MinimumApiLevel} target {TargetApiLevel} arm64 il2cpp portrait debugKey");
        }

        // Test seam: applies the playtest settings, reports what they look like, and restores
        // the previous values in a finally, so the restore path can be checked without a build.
        public static string ApplyForTestsAndGetSignature(int versionCode)
        {
            var previous = AndroidSettings.Capture();
            try
            {
                ApplyAndroidPlayerSettings(versionCode);
                return SettingsSignature();
            }
            finally { previous.Restore(); }
        }

        public static string SettingsSignature() => string.Join("|", new[]
        {
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
            PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString(),
            PlayerSettings.Android.targetArchitectures.ToString(),
            ((int)PlayerSettings.Android.minSdkVersion).ToString(),
            ((int)PlayerSettings.Android.targetSdkVersion).ToString(),
            PlayerSettings.Android.bundleVersionCode.ToString(),
            PlayerSettings.Android.useCustomKeystore.ToString(),
            PlayerSettings.Android.forceInternetPermission.ToString(),
            PlayerSettings.defaultInterfaceOrientation.ToString(),
            PlayerSettings.allowedAutorotateToPortrait.ToString(),
            PlayerSettings.allowedAutorotateToPortraitUpsideDown.ToString(),
            PlayerSettings.allowedAutorotateToLandscapeLeft.ToString(),
            PlayerSettings.allowedAutorotateToLandscapeRight.ToString(),
            PlayerSettings.bundleVersion
        });

        static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        // Snapshot of every player setting the Android build touches, so the Editor returns to
        // the project's own configuration instead of staying on the playtest one.
        readonly struct AndroidSettings
        {
            readonly string applicationIdentifier, bundleVersion;
            readonly ScriptingImplementation backend;
            readonly AndroidArchitecture architectures;
            readonly AndroidSdkVersions minSdk, targetSdk;
            readonly int versionCode;
            readonly bool customKeystore, internetPermission;
            readonly UIOrientation orientation;
            readonly bool portrait, portraitUpsideDown, landscapeLeft, landscapeRight;
            readonly bool appBundle;
            readonly AndroidBuildSystem buildSystem;
            readonly AndroidBuildType buildType;
            readonly MobileTextureSubtarget subtarget;
            AndroidSettings(string id, ScriptingImplementation scripting, AndroidArchitecture arch,
                AndroidSdkVersions min, AndroidSdkVersions target, int code, bool keystore, bool internet,
                UIOrientation ui, bool p, bool pu, bool ll, bool lr, bool bundle,
                AndroidBuildSystem system, AndroidBuildType type, MobileTextureSubtarget sub)
            {
                applicationIdentifier = id; backend = scripting; architectures = arch; minSdk = min; targetSdk = target;
                versionCode = code; customKeystore = keystore; internetPermission = internet; orientation = ui;
                portrait = p; portraitUpsideDown = pu; landscapeLeft = ll; landscapeRight = lr; appBundle = bundle;
                buildSystem = system; buildType = type; subtarget = sub; bundleVersion = PlayerSettings.bundleVersion;
            }
            public static AndroidSettings Capture() => new AndroidSettings(
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android),
                PlayerSettings.Android.targetArchitectures,
                PlayerSettings.Android.minSdkVersion, PlayerSettings.Android.targetSdkVersion,
                PlayerSettings.Android.bundleVersionCode, PlayerSettings.Android.useCustomKeystore,
                PlayerSettings.Android.forceInternetPermission, PlayerSettings.defaultInterfaceOrientation,
                PlayerSettings.allowedAutorotateToPortrait, PlayerSettings.allowedAutorotateToPortraitUpsideDown,
                PlayerSettings.allowedAutorotateToLandscapeLeft, PlayerSettings.allowedAutorotateToLandscapeRight,
                EditorUserBuildSettings.buildAppBundle, EditorUserBuildSettings.androidBuildSystem,
                EditorUserBuildSettings.androidBuildType, EditorUserBuildSettings.androidBuildSubtarget);
            public void Restore()
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, applicationIdentifier);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
                PlayerSettings.Android.targetArchitectures = architectures;
                PlayerSettings.Android.minSdkVersion = minSdk;
                PlayerSettings.Android.targetSdkVersion = targetSdk;
                PlayerSettings.Android.bundleVersionCode = versionCode;
                PlayerSettings.Android.useCustomKeystore = customKeystore;
                PlayerSettings.Android.forceInternetPermission = internetPermission;
                PlayerSettings.defaultInterfaceOrientation = orientation;
                PlayerSettings.allowedAutorotateToPortrait = portrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = portraitUpsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft = landscapeLeft;
                PlayerSettings.allowedAutorotateToLandscapeRight = landscapeRight;
                PlayerSettings.bundleVersion = bundleVersion;
                EditorUserBuildSettings.buildAppBundle = appBundle;
                EditorUserBuildSettings.androidBuildSystem = buildSystem;
                EditorUserBuildSettings.androidBuildType = buildType;
                EditorUserBuildSettings.androidBuildSubtarget = subtarget;
            }
        }

        public static string NextBuildDirectory(string root)
        {
            Directory.CreateDirectory(root);
            int highest = 0;
            foreach (string entry in Directory.EnumerateFileSystemEntries(root))
                if (int.TryParse(Path.GetFileName(entry), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int number) && number > highest)
                    highest = number;
            string path = Path.Combine(root, checked(highest + 1).ToString("D2", System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(path);
            return path;
        }
    }

    // Ships the project's settings.json and level.json inside the APK's StreamingAssets/Config.
    // Nothing is copied into the project: the files are added to this build only.
    public sealed class BundledConfigProcessor : UnityEditor.Build.BuildPlayerProcessor
    {
        public override int callbackOrder => 0;
        public override void PrepareForBuild(UnityEditor.Build.BuildPlayerContext buildPlayerContext)
        {
            if (buildPlayerContext.BuildPlayerOptions.target != BuildTarget.Android) return;
            string staging = Path.GetFullPath(Path.Combine("Library", "Locus", "tmp", "android-config", "Config"));
            Directory.CreateDirectory(staging);
            foreach (string name in new[] { "settings.json", "level.json" })
            {
                string source = Path.GetFullPath(Path.Combine("Config", name));
                if (!File.Exists(source)) throw new BuildFailedException($"Config/{name} is required for the Android package.");
                File.Copy(source, Path.Combine(staging, name), true);
            }
            buildPlayerContext.AddAdditionalPathToStreamingAssets(staging, BundledConfiguration.Folder);
        }
    }
}
