# UI 设计预览使用说明

打开本目录 index.html 查看可切换的页面设计。全部资源位于工程本地，不请求网络；中文字体引用工程内 NotoSansCJKsc-Regular.otf，请保留目录关系。

左侧切换开始页、局内 HUD、暂停、通关、失败、全程通关和配置错误；可勾选策划模式、奖励、发射中和长文本。按钮响应仅演示视觉分支，数值和方块布局不是实机关卡。

可在地址后添加 ?clean=1&page=hud 仅查看竖版舞台，page 可取 title、hud、pause、win、lose、final、error。窗口比例改变时整体缩放舞台，横屏留边。原型并未执行真实输入、物理或配置命令。

## 资源来源与生成记录

- Logo参考.png：从旧修整前备份复制，用于辅助徽章的比例演示。
- 岩壁背景参考.png：内置 image_gen 基于已完成的岩壁边缘视觉稿移除中央方块、数字、发射点、小球与危险线得到。用于 UI 预览，尚未作为 Unity 运行素材接入。
- UI 面板、切角、按钮、图标、动态文字和示意方块由 HTML/CSS/SVG 编写，不烘焙在图片中。

背景清理的完整提示词：

Precise cleanup edit for a game UI design background. Remove ALL gameplay objects from the central area of this image: all nine colored rectangular mineral blocks, all 99 and +3 text and badges, the small ivory launcher ring, the ivory ball and its dotted trail, and the horizontal red line. Replace these areas with the same quiet deep navy flat background that surrounds them, with no seams or new details. Preserve the canvas aspect ratio and the natural dark discontinuous rock planes at the far left, right and upper corners. Preserve the existing bottom boulder group and its subdued teal crystals exactly. Do not add UI, borders, frames, text, lights, extra rocks, texture or any new objects. This must be an empty, clean dark-blue cave stage with peripheral and bottom rock decoration, suitable to overlay separately coded UI.

## 检查范围

详见自检记录.md。已完成脚本语法、资源与控件引用、色彩对比和舞台适配计算检查；浏览器实际渲染及交互未实测，工具拒绝 file URL 后没有绕过限制。Unity 接入须执行 UI设计方案.md 中的 PlayMode 与实际窗口验收，不能以本原型替代。
