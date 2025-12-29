# Lone EFT DMA Radar（汉化版）
<img width="512" height="512" alt="image" src="https://github.com/user-attachments/assets/dc52d50b-66dd-4a9d-bbf2-c7d9b8c24aba" />

这是基于上游项目的**中文汉化版本**（UI 文本与运行时翻译文件）。

- 上游原项目：<https://github.com/lone-dma/Lone-EFT-DMA-Radar>
- 本分支目标：尽量不改动上游逻辑，只做本地化接入，便于持续同步更新。

## 语言切换
在程序内：`Settings → General → Language` 可在 **English / Chinese** 间切换，并会写入配置（下次启动自动生效）。

## 翻译文件
程序会在首次启动时自动生成默认中文翻译文件：

- `%AppData%\Lone-EFT-DMA\lang\zh-CN.json`

你也可以直接编辑该 JSON，重启后生效。

## ⚠️ 重要：请先阅读
上游作者已将 Lone EFT DMA Radar 调整为 **只读（READ-ONLY）**、**仅雷达（RADAR-ONLY）**。

如果你在寻找旧版 **全功能（FULL FEATURED）** 的 Lone EFT DMA，现在由 [DMA Educational Resources](https://github.com/dma-educational-resources/eft-dma-radar) 维护。

请不要在 Issues 里提交功能需求。

## 👋 欢迎
这是 Lone 发布的原版 Lone EFT DMA Radar，并包含一些与早期版本不同的关键点：
1. **仅雷达 / 不进行内存写入（No Memwrites）**
2. 全新且改进的 Silk.NET / ImGui 界面

这个版本的目标是尽可能降低未来被检测的风险：它提供你所需的信息，同时也尽量让你能完整体验游戏本身。

## 💾 安装与设置
- 在 [Releases](https://github.com/lone-dma/Lone-EFT-DMA-Radar/releases) 下载最新版本。
- 查看 [安装指南（Setup Guide）](https://github.com/lone-dma/Lone-EFT-DMA-Radar/wiki/Radar-Setup-Guide)。

## 💸 捐助
如果你觉得这个软件对你有帮助，欢迎捐助支持！每一份支持都很有意义 :) 详见：[捐助信息](https://github.com/lone-dma#-support-the-project)

## 💖 特别感谢
- @xx0m 与 @Mambo-Noob
  - 感谢你们维持 DER 社区运转，并持续维护另一个 eft-dma-radar fork，辛苦了！
- Marazm
  - 感谢你在地图方面的付出，以及愿意将其贡献到公开领域。
- Keeegi
  - 感谢你在 Unity/IL2CPP 逆向方面提供的有价值见解。

---

# Original README (English)
# Lone EFT DMA Radar
<img width="512" height="512" alt="image" src="https://github.com/user-attachments/assets/dc52d50b-66dd-4a9d-bbf2-c7d9b8c24aba" />

## ⚠️ IMPORTANT Read First
I have since changed Lone EFT DMA Radar to be **READ-ONLY** and **RADAR-ONLY**.

If you are looking for the old **FULL FEATURED** version of Lone EFT DMA, it is now being maintained by [DMA Educational Resources](https://github.com/dma-educational-resources/eft-dma-radar).

No feature requests in Issues please.

## 👋 Welcome
This is the original Lone EFT DMA Radar by Lone, with some key differences from the original version:
1. **Radar Only/No Memwrites.**
3. New & Improved Silk.NET/ImGui Interface.

This version is designed to be as safe as possible from any future detections, and gives you the information that you need while allowing you to fully experience the game for yourself.

## 💾 Setup
- Download the latest version in [Releases](https://github.com/lone-dma/Lone-EFT-DMA-Radar/releases).
- See our [Setup Guide](https://github.com/lone-dma/Lone-EFT-DMA-Radar/wiki/Radar-Setup-Guide).

## 💸 Donations
If you find this software useful, _please_ consider donating! Every little bit helps :) [See here for Donations Info](https://github.com/lone-dma#-support-the-project)
 
## 💖 Special Thanks
- @xx0m and @Mambo-Noob
  - For keeping the DER Community running, and doing an awesome job keeping the other eft-dma-radar fork maintained. Thank you!
- Marazm
  - For your hard work on maps, and your willingness to contribute them to the open domain. Thank you!
- Keeegi
  - For your helpful insights on Unity/IL2CPP Reversing. Thank you!
