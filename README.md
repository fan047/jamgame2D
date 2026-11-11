# Lucky Break

**Lucky Break（水晶矿洞）** 是一款受 holedown 启发的竖屏弹球解谜游戏。瞄准矿洞发射一轮球，让它们在岩壁与方块之间反弹；击碎青色水晶推进本关，击碎橙色奖励块让下一关的球更多。

<img src="Assets/Holedown/Art/Theme/title_key_art.png" alt="Lucky Break 标题键图" width="320">

**当前版本：1.0.0-rc1** · Android 竖屏 · 九关 · 三组矿洞 · 中文 UI

## 玩法

- 从矿洞顶部瞄准并发射一轮球；球全部回收且待发队列清空后统一结算。
- 普通灰蓝岩块是障碍；青色水晶是目标块，击碎它才能推进本关；橙色上箭头是奖励块，通关时确认，从下一关起增加每轮球数。
- 结算后存量方块上移一格，下一排从底部升入；底部的灰蓝轮廓是下一排预告。
- 方块越过危险线即失败，可立即重试。方块有轻/重两档裂痕，数字、徽记和裂痕都由程序叠加，不烘焙在图片里。
- 同一轮内球速会从基础速度逐渐升到 2 倍；暂停冻结，换轮、重试和切关回到 1 倍。
- 九关分青晶、紫晶、玫红金晶三组矿洞；奖励跨关累计，入关球数可从 12 成长到 80。

## 操作

| 平台 | 操作 |
| --- | --- |
| Android | 拖动屏幕瞄准，松手发射；点击底部「召回」；右上角「暂停」；标题页或暂停菜单的「设置」调音量 |
| Windows | 移动鼠标瞄准，左键发射；Space 或「召回」；Esc 或「暂停」；「继续游戏」恢复 |

切出窗口会自动暂停；回到窗口后需要先激活，再点「继续游戏」。

## 运行

### Android（主要交付）

- 当前试玩包为 **Demo-03**（versionCode 3，Android 7.0 / API 25 及以上，arm64，竖屏锁定）。
- 应用名显示 **Lucky Break**；APK 文件名和包名仍是早期命名 `CrystalMinePlaytest` / `com.fan047.crystalmine.playtest`。
- 关卡与球参随包内置，不需要外部配置文件；音量设置会保存到本机。
- 安装步骤、包大小与 SHA-256 见 [GameJam 提交信息](Docs/最终版/GameJam提交信息.md)。
- 源码构建：Unity 6000.4.3f1，菜单 **Holedown → Build Android Playtest**。

### Windows / 从源码运行

需要 **Unity 6000.4.3f1** 和 **Git LFS**。

```sh
git lfs pull
```

用 Unity Hub 打开仓库根目录，打开 [SampleScene](Assets/Scenes/SampleScene.unity)，进入 Play Mode，点击「开始游戏」。

构建 Windows 包：退出 Play Mode，选择 **Holedown → Build Windows Growth**；输出到 `Builds/Windows/<编号>`。本仓库不跟踪 `Builds/`，发布包由团队单独分发。

## 策划工作台

用 Chrome 或 Edge 打开 [离线工作台](Tools/PlannerEditor/index.html)，无需启动服务器。可以编辑九关布局、补行、血量和奖励，也可以试听、调音量、静音或替换音效。

| 配置 | 应用方式 |
| --- | --- |
| `Config/settings.json`、`Config/level.json` | 工作台导入后分别导出，放回 `Config`；策划测试暂停菜单点「重载当前关」。 |
| 可选的 `Config/audio.json` | 工作台「音效」页单独导入/导出；暂停菜单点「重载音效」，可继续当前局。 |

Unity Editor 读取项目根目录 `Config`，打包后读取随包配置。策划测试入口与工作台操作见 [交付与操作](Docs/最终版/交付与操作.md)。

## 项目结构

| 目录 | 内容 |
| --- | --- |
| `Assets/Holedown/Scripts` | 规则、运行时、主题呈现、构建入口与测试 |
| `Assets/Holedown/Art` | 三组矿洞背景、前景、方块、标题键图与 UI 素材 |
| `Assets/Holedown/Fonts` | Noto Sans CJK SC 与 ZLabs Pixel 字体资产及 OFL 许可 |
| `Config` | 球参、九关配置与示例 |
| `Tools/PlannerEditor` | 离线策划工作台与前端测试 |
| `Docs/最终版` | 产品、技术、交付、提交信息与验收记录 |
| `Docs/中期版`、`Docs/成长版` | 阶段方案、构建记录与历史资料 |

## 测试

在 Unity Test Runner 中运行 `Holedown.EditTests` 与 `Holedown.PlayTests`。工作台的配置与音效核心测试：

```sh
node --test Tools/PlannerEditor/tests/model.test.cjs Tools/PlannerEditor/tests/state.test.cjs Tools/PlannerEditor/tests/audio.test.cjs
```

画面验收覆盖 540×960、720×1280、1280×720 与运行时缩放；规则、配置、输入、音效与发布包的验收项见 [技术与验收](Docs/最终版/技术与验收.md)。

## 文档

- [产品与玩法](Docs/最终版/产品与玩法.md)
- [技术与验收](Docs/最终版/技术与验收.md)
- [交付与操作](Docs/最终版/交付与操作.md)
- [GameJam 提交信息](Docs/最终版/GameJam提交信息.md)
- [Android 真机试玩方案](Docs/最终版/Android真机试玩方案.md)
- [文档索引](Docs/README.md)

## 素材与许可

- **引擎**：Unity 6000.4.3f1。
- **中文界面字体**：Noto Sans CJK SC Regular，SIL Open Font License 1.1；见 [README](Assets/Holedown/Fonts/NotoSansCJK/README.txt) 与 [OFL](Assets/Holedown/Fonts/NotoSansCJK/OFL.txt)。
- **像素字体**：Z Labs Pixel 12/24，作者 Astro_2539，SIL Open Font License 1.1；见 [OFL](Assets/Holedown/Fonts/Pixel/OFL.txt)。
- **音效**：Kenney、Almitory、Opus Sounds Directory、Robin Lamb 等 CC0 素材；背景音乐 Cleyton Kauffman《Exploration Theme》CC0。逐条来源见 [首批音效素材来源](Docs/成长版/首批音效素材来源.md)。
- **美术**：团队提供与本地程序化提取/修复；来源与处理见 [美术素材来源清单](Docs/成长版/美术素材来源清单.md)。
- **灵感**：玩法受 holedown 启发，代码与关卡为本项目原创。

## 发布分支

当前发布候选分支为 `release/1.0-rc1`，产品版本 `1.0.0-rc1`。`main` 保留项目基线，开发历史见 `dev/game-polish`。
