# 水晶矿洞

一款受 **holedown** 启发的 Unity 2D Game Jam 弹球游戏。瞄准发射一串球，利用反弹打碎矿洞里的方块。击碎目标块并完成本轮即可过关。击碎奖励块后，本关通关时会确认奖励，从下一关起每轮可以发射更多球。

<img src="Assets/Holedown/Art/Theme/title_logo.png" alt="水晶矿洞游戏徽章" width="240">

当前交付为 **Windows 构建 23**，包含九关、青晶／紫晶／玫红金晶三组矿洞、正式中文 UI、八类音效和离线策划工作台。团队以[最终版文档](Docs/README.md)为准。[构建 23 交付记录](Docs/中期版/构建23交付说明.md)

## 运行游戏

从源码运行需要 **Unity 6000.4.3f1** 和 **Git LFS**。克隆后执行 `git lfs pull` 获取图片、音频和字体，再用 Unity Hub 打开仓库根目录。打开 [SampleScene](Assets/Scenes/SampleScene.unity)，进入 Play Mode 并点击「开始游戏」。

已有 Windows 试玩包时，完整解压后运行 `HoledownGrowth.exe`。从源码打包时，在 Unity 安装 Windows x64 Build Support，退出 Play Mode，然后选择 **Holedown → Build Windows Growth**。程序、离线工作台和配置会写入本地的 `Builds/<编号>/Windows`，每次构建使用新编号。构建产物单独分发。

| 操作 | 输入 |
| --- | --- |
| 瞄准与发射 | 移动鼠标瞄准，在棋盘内按左键发射一轮 |
| 主动召回 | Space 或「召回」 |
| 暂停 | Esc 或「暂停」 |
| 恢复 | 「继续游戏」 |

球全部收回且待发队列清空后进行结算。目标块还在时，棋盘会上移，灰蓝色的下一排预告从底部升入。推进后方块触及危险线会失败，可以重试当前关。

## 策划工作台

用 Chrome 或 Edge 打开[离线工作台](Tools/PlannerEditor/index.html)，无需启动服务器。可以新增、复制、重排关卡，浏览整关布局，批量改血量和奖励，也可以试听、调整音量、静音或替换内置音效。

| 配置 | 编辑和应用 |
| --- | --- |
| `settings.json`、`level.json` | 在工作台同时导入两份文件，编辑后分别导出。放回 `Config`，在策划测试暂停菜单点「重载当前关」。正式模式的「重载并重开」会回到第 1 关。 |
| 可选的 `audio.json` | 在「音效」页单独导入或导出。放回 `Config`，在策划测试暂停菜单点「重载音效」，可继续当前一局。普通模式启动时也会读取音效配置。 |

Unity Editor 读取项目根目录的 `Config`，Windows 试玩包读取 exe 同目录的 `Config`。双击试玩包里的「策划测试.cmd」可直达指定关，入关球数可选基础值、满前关奖励或手填。[策划与交付指南](Docs/最终版/交付与操作.md)

## 开发与验证

| 目录 | 内容 |
| --- | --- |
| [Assets/Holedown/Scripts](Assets/Holedown/Scripts) | 棋盘与成长规则、Unity 运行时、构建入口和测试 |
| [Assets/Holedown](Assets/Holedown) | 游戏图片、预制体、音频与数据资产 |
| [Config](Config) | 球参、关卡配置与可选示例 |
| [Tools/PlannerEditor](Tools/PlannerEditor) | 离线工作台源码与前端测试 |
| [Docs](Docs/README.md) | 最终版产品、技术、交付指南与历史资料 |

Unity 回归可在 Test Runner 中运行 `Holedown.EditTests` 和 `Holedown.PlayTests`。工作台的配置与音效核心测试可在仓库根目录使用 Node.js 运行。

```sh
node --test Tools/PlannerEditor/tests/model.test.cjs Tools/PlannerEditor/tests/state.test.cjs Tools/PlannerEditor/tests/audio.test.cjs
```

浏览器自动化需要 Playwright 和 Chrome，入口见[工作台测试目录](Tools/PlannerEditor/tests)。素材出处见[美术来源](Docs/成长版/美术素材来源清单.md)与[音效授权记录](Docs/成长版/首批音效素材来源.md)。
