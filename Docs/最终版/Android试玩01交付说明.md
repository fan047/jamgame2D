# Android 试玩 01 交付说明

2026-11-10。首个 Android 真机试玩包，同工程同 `SampleScene`，只跑正式流程与已有音量设置。

## 交付物

| 内容 | 位置 |
|---|---|
| APK | `Builds/Android/01/CrystalMinePlaytest.apk` |
| 构建记录 | `Builds/Android/01/build-result.txt`：Succeeded，errors=0，warnings=996，1266420518 bytes，versionCode=1 |
| APK 校验 | 73,927,281 字节；SHA-256 `A50F41567C3BBF4330853695EC6D6E2E6EE7CB564EAD2B0D830F1CF615B41456` |
| IL2CPP 符号 | `Builds/Android/01/CrystalMinePlaytest_BackUpThisFolder_ButDontShipItWithYourGame`（本地留存，不随包分发） |
| 首次失败记录 | `Builds/Android/01-failed-burst-temp.txt` |

包信息（`aapt2 dump badging` + `apksigner verify`）：包名 `com.fan047.crystalmine.playtest`，versionCode **1**、versionName 1.0，minSdk **25**、targetSdk **36**，native-code 仅 **arm64-v8a**，竖屏，`CN=Android Debug` 签名（v2 方案校验通过）。包内 `assets/Config/settings.json`、`assets/Config/level.json` 与仓库 `Config` 逐字节一致（关卡 `mid-11`、九关）。

## 实现要点

- **随包配置**：构建时由 `BuildPlayerProcessor.PrepareForBuild` 把项目 `Config` 的两份文件加入 `StreamingAssets/Config`（不写进工程、不改场景）。运行时 `BundledConfiguration` 用 `UnityWebRequest` 读取，交给 `ExternalConfiguration.Parse` 的同一套严格校验；**必须恰九关**，缺文件、坏 JSON、少关卡都进配置错误页，不回退单关。`ConfigurationDirectory` 在 Android 保持为空，`FeedbackPlayer` 不再等它、直接用内置音效快照。
- **手机界面**：隐藏外置重载（`PauseView` 的「重载并重开」在随包模式不显示，帮助文字改为触屏说明）；`HudView` 的 reload 维持原有「始终隐藏」；召回提示跟随 `playing`。开始页、暂停与召回按钮沿用现有控件，提示文字在移动端换成触屏用词（`TouchHint`）。
- **触控**：单指按住拖动预瞄、合法松手发射一次。只有真正的 `Ended` 相且手指 ID 与起手一致才发射；`Canceled`、第二指、越界、滑出后滑回、换指一律取消且不重新预瞄；双指取消后必须等所有触摸释放才接受下一次；UI 与留边起手无效。触屏模式不走鼠标路径，不会被移动端鼠标模拟重复发射；Windows 鼠标保持「按下即发射」。
- **回前台**：`OnApplicationPause`／`OnApplicationFocus` 统一走 `LeaveForeground`／`RestoreForeground`；切后台立即暂停并清空输入，回前台只恢复按钮可用性，游戏仍停在暂停，按继续才恢复。
- **安全区**：`BoardCamera` 以 `Screen.safeArea` 为可用视口等比容纳舞台与世界空间 UI，并按安全区中心偏移视角；触点换算用同一相机。

## 自测

| 项目 | 结果 |
|---|---|
| Unity EditMode | 134/134（含 `BundledConfigurationTests` 5 项、`AndroidBuildSettingsTests` 2 项） |
| Unity PlayMode | 107/107（含 `AndroidPlaytestTests` 13 项） |
| 随包配置 | 九关加载成功；坏 JSON／缺文件／少关卡均进配置错误页且 `Campaign` 为空；修好文件后「重新读取」恢复到标题页 |
| 触控（设备层） | 虚拟 Touchscreen 走 `QueueStateEvent`：Began→拖动→Ended 只发一轮；移除设备造成的 Canceled 不发射 |
| 触控（直注边界） | UI／留边起手、换指、滑出滑回、双指+等释放、触屏模式忽略鼠标按下 |
| 生命周期 | 仅 Pause 事件、仅焦点事件、两种顺序相邻到达：都停在暂停，回前台后按钮可用，继续才恢复 |
| Windows 回归 | 外置配置、鼠标与键盘、策划入口、`PlayerDeviceTests` 全绿 |
| 构建还原 | `AndroidBuildSettingsTests`：切到试玩设置后逐项还原，`ProjectSettings.asset` 字节不变（工作区仍只有原有 `runInBackground` 差异） |

## 真机验收状态

`adb devices` 当前**无设备**，因此以下真机项尚未执行，不能用编辑器或模拟器结果替代：

1. 离线冷启动（断网、飞行模式）与首次进入第 1 关。
2. 九关三组主题实际画面（青晶／紫晶／玫红金晶）与待上场预告、底边反弹。
3. 切后台再回前台的暂停与继续、系统返回默认行为。
4. 音量设置持久化（杀进程后重开）。
5. 覆盖安装：用目录序号 02 覆盖 01，确认 versionCode 递增且数据保留。
6. 后期多球性能与发热（第 7–9 关满速球群）。

设备接入后按上表逐项补测，并把结果追加到本文件。
