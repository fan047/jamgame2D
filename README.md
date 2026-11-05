# Holedown 2D 项目导航

Unity 2D 弹球消块项目。此页整理工程入口与目录职责；功能交付状态以对应交付说明为准，后续设计方案不代表已实现。

## 快速入口

- **开发环境**：Unity `6000.4.3f1`，URP、New Input System；用 Unity Hub 打开本目录。
- **游戏入口**：[SampleScene](Assets/Scenes/SampleScene.unity)，打开后进入 Play Mode，点击“开始游戏”进入第 1 关。左键发射、空格召回、Esc／“暂停”暂停、“继续游戏”恢复。
- **策划工具**：[离线编辑器](Tools/PlannerEditor/index.html)，用 Chrome / Edge 打开，无需服务器；操作见[快速上手](Tools/PlannerEditor/快速上手.txt)。
- **项目配置**：[球参数](Config/settings.json)、[关卡](Config/level.json)。工具不会自动读取这两份文件，需手动导入；导出后需手动替换，再在游戏中点击“重载并重开”。
- **当前交付**：[关卡工作台重设计 · 交付 11](Docs/成长版/策划工具工作台重设计交付说明.md)，目录 `Builds/11/Windows`；数值表独占整行，重排预览／属性／批量编辑，完善窄窗口适配。本次只更新网页工具，游戏沿用[全中文构建 10](Docs/成长版/全中文界面交付说明.md)。继承[构建 09](Docs/成长版/策划工具整关总览交付说明.md)的整关浏览与跨组编辑，以及[构建 08](Docs/成长版/九关程序扩展预览交付说明.md)的九关程序与指定关测试能力。正式配置仍为策划确认的三关；正式九关及两张新背景待提供。现有美术接入见[构建 05](Docs/成长版/正式美术样片交付说明.md)，正式 UI 见[开放简报](Docs/成长版/UI美术制作清单.md)。

## 目录职责

| 路径 | 用途与整理约定 |
|---|---|
| `Assets/Holedown/Scripts/Rules` | 棋盘、会话、战役规则；程序集 `Holedown.Rules` |
| `Assets/Holedown/Scripts/Runtime` | Unity 运行时流程、输入、球、棋盘表现、HUD、配置加载；程序集 `Holedown.Runtime` |
| `Assets/Holedown/Scripts/Editor` | Unity 编辑器与 Windows 构建入口；程序集 `Holedown.Editor` |
| `Assets/Holedown/Scripts/Tests` | Unity EditMode / PlayMode 测试及各自程序集 |
| `Assets/Holedown/Art` | 游戏图片与材质，包括现有占位裂痕 |
| `Assets/Holedown/Data` | 内置关卡、默认设置、物理材质与 VisualTheme 素材引用配置；分组背景缺图回退既有主题 |
| `Assets/Holedown/Prefabs` | 球与方块预制体 |
| `Assets/Scenes` | 游戏场景 |
| `Assets/Settings` | 渲染管线配置与场景模板 |
| `Assets/TextMesh Pro` | 字体相关依赖资源 |
| `Config` | Editor 默认使用、构建时复制的外置配置 |
| `Config/Examples` | 可选示例，不自动替换正式配置 |
| `Tools/PlannerEditor` | 离线策划工具源码，浏览器测试与 Node 测试位于其 `tests` 子目录 |
| `Tools` 下的 `Generate*.cjs` | 配置与测试样本生成脚本，写入范围见下文 |
| `Tests/Fixtures` | C# / JS 测试共享的 JSON 样本，不是游戏默认配置 |
| `Docs` | 分阶段设计、技术、使用与交付文档，见文档索引 |
| `VISUAL_DESIGN.md` | 美术素材交接约定，保留原路径供现有文档引用 |
| `Packages`、`ProjectSettings` | Unity 包与项目设置，不作为普通资料移动 |
| `Locus` | 开发工具配置、知识与工作区相关文件，不混入游戏资源整理 |

`Config`、`Tests/Fixtures`、`Tools/PlannerEditor` 和场景路径有代码或测试依赖，移动前必须同步检查引用。Unity 资源移动应通过 Unity API 保留 `.meta` 与 GUID。

## 配置与试玩

- Editor 默认读取项目根目录的 `Config`；Player 默认读取 exe 同目录的 `Config`。显式配置目录可覆盖默认值。
- 两份 JSON 都缺失时才回退内置配置；只缺一份或内容非法会报错并停止发射。
- “重新开始全程”使用已加载快照；普通模式读取磁盘修改需用“重载并重开”，从第一关重开。
- Player 同目录 `策划测试.cmd` 用 `--planner-test` 启动选关测试，支持基础／满前关／手填球数；该模式重载按稳定 ID 回当前关，失败保留旧局。
- 文档总览见 [Docs/README.md](Docs/README.md)；工程配置和工具默认值已同步构建 05 策划三关基线，来源哈希见构建 08 交付说明。
- [竖块示例](Config/Examples/level-vertical.json)需先备份原配置，再复制为 `Config/level.json`；体验后恢复备份并重载。示例不是正式默认种子。
- 配置版本兼容性、操作细节和限制见[策划快速上手](Tools/PlannerEditor/快速上手.txt)与[P1 交付说明](Docs/成长版/P1交付说明.md)。

## 测试入口

### Unity

在 Unity Test Runner 中选择相应模式与程序集：

- EditMode：`Holedown.EditTests`，源码位于 `Assets/Holedown/Scripts/Tests/EditMode`。
- PlayMode：`Holedown.PlayTests`，源码位于 `Assets/Holedown/Scripts/Tests/PlayMode`。

运行前保存编辑中的场景。本文不把历史交付中的测试结果视为当前工作区的验证结果。

### Node

在项目根目录运行（需安装 Node.js）：

```sh
node --test Tools/PlannerEditor/tests/model.test.cjs Tools/PlannerEditor/tests/state.test.cjs Tools/PlannerEditor/tests/p1.test.cjs
```

真实浏览器测试另见 `Tools/PlannerEditor/tests/browser.test.cjs`、`growth.browser.cjs`、`p1.browser.cjs`；运行前检查脚本所需环境，不包含在上面的纯 Node 测试命令中。

## 构建与生成脚本

- Unity 菜单：`Holedown > Build Windows Growth`，需要 Windows x64 Build Support，并退出 Play Mode。
- 构建实现：[WindowsBuild.cs](Assets/Holedown/Scripts/Editor/WindowsBuild.cs)。每次自动创建 `Builds/01`、`Builds/02`……，不覆盖旧包，也不补用旧编号空缺。
- 每个编号目录由脚本生成 `Windows/HoledownGrowth.exe`、配套运行文件、配置、策划工具与 `build-result.txt`。ZIP 和交付说明不是该构建方法自动生成的内容，需要单独准备。
- 新包复制项目当前配置，不会自动继承旧包调参；分发必须保留完整运行目录，不能只复制 exe。

以下脚本会写文件，不要把它们当只读校验命令：

| 脚本 | 写入范围与注意事项 |
|---|---|
| [GenerateGrowthConfig.cjs](Tools/GenerateGrowthConfig.cjs) | 从 Git 历史提交 `b0244f9` 生成 `Tests/Fixtures/legacy-level.json`、`growth-level.json`；加 `--publish` 还会覆盖 `Config/level.json` 和 `Tools/PlannerEditor/defaults.js`，运行前备份调参 |
| [GenerateP1Example.cjs](Tools/GenerateP1Example.cjs) | 根据当前 `Config/level.json` 覆盖 `Config/Examples/level-vertical.json`，不修改默认关卡 |

## 自动生成与本地文件

`Library`、`Temp`、`Obj`、`Logs`、`Builds`、`UserSettings` 及根目录 `.csproj`、`.slnx` 等由 Unity、IDE 或构建产生，已有忽略规则；不需要为整理源码而移动或删除。历史构建应保留在各自独立目录中。
