# 水晶矿洞 · Android 真机试玩方案

2026-11-10 已确认，待实施。目标是在同一工程、同一 `SampleScene` 上产出首个可上真机试玩的 Android 包（`Builds/Android/01` 起编号），手机只跑正式流程与已有音量设置，Windows 继续保留外置配置与策划工作台。

## 构建入口

菜单 **Holedown → Build Android Playtest**（`Assets/Holedown/Scripts/Editor/AndroidPlaytestBuild.cs`）。工程当前没有 Build Profile 资产，构建不依赖它：入口自己切平台设置、出包并还原。构建期间把 `TEMP/TMP` 指向 `Library/Locus/tmp/android-build-temp`（Burst 原生链接需要可写临时目录，与 Windows 构建同一做法），`finally` 还原全部改动过的 Player 设置与 `ProjectSettings.asset` 字节。

## 构建配置

| 项目 | 取值 |
|---|---|
| Unity | 本机 6000.4.3f1（含 Android Build Support、SDK／NDK／OpenJDK） |
| 场景 | 仅 `Assets/Scenes/SampleScene.unity` |
| 脚本后端 | IL2CPP |
| 架构 | ARM64（`AndroidTargetArchitectures = ARM64`） |
| 方向 | 竖屏（`portrait`），横屏不旋转 |
| min API | 25 |
| target API | 36 |
| 包名 | `com.fan047.crystalmine.playtest` |
| 签名 | 本机 Unity 调试签名（未配置自定义 keystore 时 Unity 使用 `~/.android/debug.keystore`） |
| 构建系统 | Gradle |
| 输出 | `Builds/Android/01`、`02`…，每次递增，不覆盖旧包 |
| versionCode | 与目录序号相同（首包 1），便于在设备上验证覆盖安装 |

Windows 构建入口 `Holedown → Build Windows Growth` 与既有 `Builds/*` 发布包保持不变，`Builds/Android` 独立目录，互不影响。

## 随包配置

- 构建时把项目根目录 `Config/settings.json`、`Config/level.json` 注入 `StreamingAssets/Config`。用 `IPostprocessBuildWithReport`／`BuildPlayerProcessor` 的 `BuildPlayerContext.AddAdditionalPathToStreamingAssets` 完成，不手工改场景、不写死路径。
- 运行时 Android 从 `StreamingAssets/Config` 用 `UnityWebRequest` 读取，交给现有 `ExternalConfiguration` 的**同一套严格校验**：
  - 必须九关、稳定 ID 唯一、恰有一个目标且在最后一条补行、最终关无奖励等既有规则全部照旧。
  - 缺失或非法 → 显示现有配置错误页，**不允许退回内置单关**。
- 手机不读 exe 同级目录、不读外部 `Config`，`ConfigurationDirectory` 在 Android 不作为等待条件；`FeedbackPlayer` 目前会等待该目录，Android 分支必须直接跳过，不能悬挂或读到意外的外部文件。
- Windows 保持现有外置 `Config`、`ExternalConfiguration.Load`、策划工作台与「重载当前关／重载并重开」不变。

## 手机界面与输入

- 只保留正式模式、暂停／继续、召回、音量设置。隐藏外置重载与策划专用入口（策划测试视图、配置目录提示等），Windows 继续显示。
- 触控：在有效棋盘内**单指按住拖动**预瞄，合法松手发射一次。
  - 复用 `AimInput` 的几何（发射点、半径、首次碰撞预测）、UI 射线与既有命令门禁。
  - 起手落在 UI 或舞台留边无效；滑出有效区域、触到 UI、第二根手指、系统取消、切后台都立即取消手势，必须重新按下才能发射。
  - 首发那一下不能既当 UI 点击又当发射：禁用残留按压，并关闭移动端的鼠标模拟，避免同一次触摸发两球。
  - Windows 鼠标保持原有语义：鼠标按下即发射，松手发射只属于触屏方案。
- UI 文案在 Android 换成触屏用词（如「按住拖动瞄准」「松手发射」「轻触召回」），Windows 保持鼠标与空格提示。
- 相机：以 `Screen.safeArea` 作为可用视口，等比把棋盘与世界空间 UI 一起装进安全区（刘海、圆角、手势条内缩），触点换算用同一视口，避免瞄准点与手指错位。
- 切后台：立即暂停本局并清理输入状态（含未完成的手势）；回前台不自动继续，按现有「失焦后显式继续」语义让玩家点继续。系统返回键保留默认行为，不改按键映射。

## 验收

| 项目 | 口径 |
|---|---|
| 构建 | `Builds/Android/01` 产出 APK，包名、ABI（arm64-v8a）、min/target SDK、调试签名、九关随包配置全部检查 |
| 校验 | APK 内 `assets/Config/settings.json`、`level.json` 与项目 `Config` 一致；APK SHA-256 记录 |
| 配置错误 | 删除或破坏随包配置后启动，显示配置错误页，不退回单关 |
| 触控 | 按下拖动预瞄、松手发射；UI 起手、滑出、第二指、切后台全部取消；不出现重复发射 |
| 回归 | Windows 鼠标与键盘、外置配置、策划工作台、音量设置不变 |
| 真机 | `adb devices` 选一台常用 ARM64 手机加一台 Android 16 刘海屏：离线冷启动、九关三组主题、切后台恢复、音量持久化、覆盖安装、后期多球性能与发热 |

真机不可用时，先完成所有可执行的构建与静态检查，并在交付说明中**逐项列出待真机项目**；编辑器与模拟器的结果不得当作真机结论。

## 主要文件

构建入口 `Assets/Holedown/Scripts/Editor/`（新增 Android 构建与 StreamingAssets 注入），运行时 `ExternalConfiguration`、`GameFlowController`、`PlayerInputController`、`AimInput`、`BoardCamera`／`SceneVisuals`、`FeedbackPlayer`、`HudView`／`StartScreenView`／`PauseView`（Android 文案与入口裁剪），配置 `ProjectSettings/ProjectSettings.asset`（包名、SDK、架构、方向）。
