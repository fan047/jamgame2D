# RC 1 构建记录（Android）

## 结论

`1.0.0-rc1` 的 Android 发布候选包已产出并通过全部自动化测试，尚未做真机九关连玩与帧率测量。交付物为 `Builds/Android/04/LuckyBreak-RC1-04.zip`。

## 分支与提交

| 项 | 值 |
|---|---|
| 分支 | `release/1.0-rc1`（从 `dev/game-polish` 拉出） |
| 冻结提交 | `e738ded release: prepare 1.0.0-rc1` |
| 测试与资源修复 | `642ccd9 test: keep the release suite green from a clean machine` |
| 设置修正 | `87005b4 settings: keep run-in-background off in the release branch` |

提交范围排除：Locus 编辑器插件与个人配置、`Tools/__pycache__`、`Assets/_Recovery`、本机专用玩家设置。`.gitignore` 已补对应规则。

## 构建配置

| 项 | 值 |
|---|---|
| 入口 | Unity 菜单 `Holedown → Build Android Playtest` |
| 输出 | `Builds/Android/04`，编号即 versionCode |
| versionName / versionCode | `1.0.0-rc1` / `4` |
| 包名 | `com.fan047.crystalmine.playtest` |
| 显示名 | `Lucky Break` |
| 后端 / ABI | IL2CPP / arm64-v8a |
| SDK | min 25（Android 7.0），target 36 |
| 方向 | 竖屏锁定 |
| 签名 | Unity 本地调试签名 |
| 随包配置 | `assets/Config/{settings,level}.json`，settings 版本 `demo-1`（间隔 0.07 秒、自动召回 30 秒） |
| 结果 | 0 错误，975 警告，4 分 31 秒 |

975 条警告全部来自 `com.unity.ai.inference` 的 Sentis 计算着色器变体提示（`ConvGeneric` 的 `TRANSPOSE_OUTPUT` 变体），不涉及本作代码或资源。

## 产物与校验

| 文件 | 大小（字节） | SHA-256 |
|---|---:|---|
| `LuckyBreak-RC1-04.zip` | 75,357,321 | `4a2486864584c61874c91cb45fef310f1765658aa79154e7c93e0ee8335665ba` |
| `CrystalMinePlaytest.apk` | 77,235,770 | `d4975f87862d404a286e10b42f53a71841c081933bbf0379012746a0530b75b5` |

`RC说明.txt` 随包提供安装步骤、相对 Demo 03 的改动与已知事项。

包内核对（`aapt2 dump badging` 与直接解包）：

- `versionCode='4'`、`versionName='1.0.0-rc1'`、`application-label:'Lucky Break'`。
- `minSdkVersion:'25'`、`targetSdkVersion:'36'`、`native-code: 'arm64-v8a'`。
- 启动图标为自适应图标（`res/Qu.xml`，含 background / foreground 两层），包内 432 / 324 / 216 / 162 / 192 / 144 / 48 等尺寸贴图齐全。
- 包内含 `INTERNET` 权限，由读取包内配置的组件带入；本作不需要联网。

## 测试

| 轮次 | 结果 | 运行 ID |
|---|---|---|
| EditMode 全量 | 144 / 144 | `3713c9bb74db49feb373450e54a92c70` |
| PlayMode 全量 | 115 / 115 | `b3a3fcfb8cbe4e88bdda043e66dff11d` |

构建前的第一次 PlayMode 全量是 102 / 115。13 个失败逐个定位到根因后修复，没有放宽任何断言：

| 现象 | 根因 | 处理 |
|---|---|---|
| `ForegroundAssetTests` 红通道漂移 | 1–3 关背景重做后没有同步前景叠层 | `BuildForegroundLayer.py` 增加 `--preserve-alpha`：保留已验收遮罩，只从新背景取色，前景与背景重新逐像素一致 |
| 血量 5 期望、实际 4 | 本机 PlayerPrefs 残留「简单」难度，Easy 血量 ×0.8 | 三个断言精确血量/奖励的测试类在 `SetUp` 清除 `Holedown.Difficulty` |
| `BlockState` 构造器不存在 | 规则类新增 health 参数，反射夹具仍传 3 个 | 夹具改传 health |
| HUD 测试空引用 | 标题页提示移入 `TitleContent` | 夹具改读 `StartPanel/TitleContent/Controls` |
| 8 个用例报 `Renderer2D Pass: Fake or uninitialized surface` | URP 17 拒绝渲染到无深度缓冲的离屏目标 | 舞台夹具的两处截图目标补 24 位深度 |
| 模拟触点未报告按下 | 鼠标转触点只在 Game 视图持有输入时生效，全量运行时焦点窗口不固定 | 该用例临时固定输入设置并在 finally 还原 |

另一处泄漏：Unity 测试框架在驱动 PlayMode 时会把 `runInBackground` 置 1，该值混进了 RC 提交，已单独修正。本次 APK 在该修正之前构建，差异仅是桌面端播放器设置，Android 侧不受影响，因此没有重新打包。

## 尚未验证

- 真机九关连续试玩：难度曲线、后期球数下的流畅度、切后台再回来、第九关结束的收尾提示。
- 逐帧帧率测量；Demo 03 的 60 帧目标只在本机确认过。
- 各厂商启动器的圆形／圆角／方形裁切下的图标观感。
- 使用 Unity 调试签名，不可上架；包名与 APK 文件名仍是早期的 CrystalMinePlaytest。

## 复现

1. 切到 `release/1.0-rc1`，用 Unity 6000.4.3f1 打开工程，先确保 Git LFS 素材已拉取。
2. EditMode 与 PlayMode 全量跑通。
3. 菜单 `Holedown → Build Android Playtest`，产物落在下一个空闲编号目录。
4. 核对 `build-result.txt` 与本文件中的 versionName / versionCode，再打包交付。
