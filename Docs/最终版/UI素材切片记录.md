# UI 素材切片记录（面板与按钮）

日期：2026-11-10。范围：把美术提供的 UI 素材拆成可伸缩的面板与按钮切片，先在暂停与结算弹层试用，再复用到开始页、设置与配置错误页的共享按钮。文字、图标与交互仍由现有 uGUI 组件负责。

## 来源与归档

| 项目 | 内容 |
|---|---|
| 原图 | `D:\Downloads\picture\UI素材1.png`，1312×1199 PNG（白底，无透明通道） |
| 归档 | `Assets/Holedown/Art/UI/Source/UISheet1.png`（原样保留，未做任何修改） |
| 处理脚本 | `Tools/SliceUIAssets.py`（只读取归档图，输出到 `Assets/Resources/MineUI/`） |
| 输出 | `UIPanelFrame.png`、`UIButtonIvory.png`、`UIButtonBlue.png`、`UIButtonGold.png` |

原图许可与作者未在文件中附带；本记录只登记取得路径与加工步骤，公开发布前需按实际授权补充说明。

## 切片与加工步骤

1. 按源图实测裁剪：面板 `x 56–1255, y 67–856`，橙金按钮 `x 296–1014, y 951–1118`，蓝灰按钮 `x 337–966, y 609–739`。
2. 去底：从裁剪边缘向内做泛滥填充，白色片底（面板、橙金按钮）与面板深蓝片底（蓝灰按钮）判为透明；硬边按一像素保留 55% 半透明边，避免切边发黑。
3. 面板中心清理：源图面板内部自带演示用按钮，九宫拉伸会把它们拉成色块，因此把按钮区域用同高度的干净内壁列平铺覆盖，只保留边框、切角与内层高光。
4. 象牙白主按钮：取蓝灰按钮的剪影与浮雕，按亮度重映射到象牙白渐变（影 `#8A866E` → 面色 `#F5F0D8` → 亮面 `#FFFCEE` → 高光 `#FFFFFF`），保持既有“主操作象牙白”的颜色职责。
5. 橙金按钮按原样保留，作为奖励色角色的备用切片，本轮未接线。

## 导入设置

| 切片 | 尺寸 | 九宫边 | 设置 |
|---|---|---|---|
| `UIPanelFrame` | 1208×798 | 72 px | Sprite、Single、Full Rect、无压缩、双线性 |
| `UIButtonIvory` / `UIButtonBlue` | 637×138 | 44 px | 同上 |
| `UIButtonGold` | 727×176 | 50 px | 同上 |

- 必须为 **Full Rect** 网格：Tight 网格下九宫切片不生效。
- 切片放在 `Assets/Resources/MineUI/`，`MineUIStyle.ButtonArt(primary)` 按角色取图；面板边框由场景直接引用。

## 对应控件

| 控件 | 用法 |
|---|---|
| `UI/PausePanel/Card`、`UI/ResultPanel/Card` | 面板边框，`Sliced`，`pixelsPerUnitMultiplier = 2.4` |
| 继续游戏、进入下一关、开始游戏、配置错误下的“重新读取配置” | 主操作，象牙白切片（角色由 `MineUIStyle.Button(button, true)` 指定） |
| 重载并重开、重新开始全程、重载音效、重载配置、设置 | 次要操作，蓝灰切片（`MineUIStyle.Button(button)`） |

## 代码接入

- `CutCornerImage`：原来的平色切角控件自己生成无 UV 的多边形，贴图采样为透明。现在若图片带有九宫边框则走 `Image` 的标准切片网格，否则保持原有平色切角，既有按钮不受影响。
- `MineUIStyle.Button`：按角色分配切片，并把 `pixelsPerUnitMultiplier` 设为 `高度/34`，让 60 到 100 单位高的按钮共用同一像素比例的浮雕；取不到切片时回落到原来的象牙白／蓝灰平色。

## 验收与遗留

- 截图：`Library/Locus/tmp/ui/ui-01-title.png`、`ui-02-pause.png`、`ui-03-result.png`（Game 视图 406×686，实际发布分辨率需再对照）。
- 测试：EditMode 139/139，PlayMode 110/110。
- 遗留：HUD 的“暂停／召回”仍是平色胶囊；橙金按钮尚未接线；面板边框在小尺寸弹层上的切角落点还需在 540×960 与 720×1280 实机各看一次。
