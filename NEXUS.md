Name: Package Bank
Summary: Store furniture packages from your yard in a bank and take them out again when you need them. A HUD button opens the bank, clicking a yard package adds Bank to its menu, and the build shop gets a Bank button on every bought package plus the banked packages below its list, so you can shuffle packages while you shop. Holds 4 large, 6 medium and 8 small packages and 4 defenses by default, and story items without limit. 把院子里的家具包裹存进仓库，需要时再取出。HUD 按钮打开仓库，点击院子里的包裹时菜单中会多出「存入」，建造商店里每个已购买的包裹旁都有「存入」按钮，列表下方还会显示仓库中的包裹，购物时可以随时调整。默认可存 4 个大型、6 个中型和 8 个小型包裹以及 4 个防御设施，剧情物品不限数量。

Description (BBCode):
Your yard only has so many package spots. Package Bank gives you somewhere to put packages you are not ready to install, and lets you move them in and out while you shop.

[size=5]Main features[/size]
[list]
[*][b]Bank panel[/b]: a box button at the top of the bottom-left tool column opens a panel with the packages in your yard and the packages in the bank. [b]Bank[/b] moves a package into the bank, [b]Take out[/b] puts it back on a free package spot.[/*]
[*][b]Bank from the yard[/b]: click a package in your yard and its menu gets a [b]Bank[/b] entry beside Install and Return.[/*]
[*][b]Bank from the build shop[/b]: every bought package in the shop's package list gets a [b]Bank[/b] button next to its Refund button.[/*]
[*][b]Banked packages in the shop[/b]: the bank is listed below the shop's package list, each package with [b]Take out[/b].[/*]
[*][b]Size limits[/b]: the bank holds 4 large, 6 medium and 8 small packages by default. Change the limits or turn them off in the config.[/*]
[*][b]Story Items[/b]: free one-off packages, such as the Recorder from the hospital, go on their own shelf with no limit.[/*]
[*][b]Defenses[/b]: sandbags, spike barriers and the other defense packages go on a Defenses shelf of their own, 4 by default.[/*]
[*][b]Buttons say why not[/b]: a greyed-out button shows the reason, like a full size or no free package spot.[/*]
[/list]

[size=5]Hotkeys[/size]
[list]
[*][b]Esc[/b]: close the panel.[/*]
[/list]
There is no hotkey to open the panel; use the HUD button.

The bank is kept in your save file. A new loop empties it, so take out anything you want to keep before starting one.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]A new loop empties the bank.[/*]
[*]To refund a banked package, take it out first, then use the shop's Refund button.[/*]
[*]Looted packages stay non-refundable after a trip through the bank.[/*]
[*]A package being moved or installed cannot be banked. Car and door boxes cannot be banked.[/*]
[*]On an expedition the panel and the HUD button are hidden.[/*]
[*]Banked packages are not in the yard, so the preparation checklist in the countdown before the disaster does not count them.[/*]
[/list]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19131. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, to change settings without a restart: install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackageBank.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Take everything out of the bank first, then delete [font=Courier New]PackageBank.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder. Packages still in the bank are lost once the mod is gone.

[size=5]Configuration[/size]
To change the size limits or turn them off, edit [font=Courier New]BepInEx\config\com.dev.survivallog.packagebank.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen (see [url=https://github.com/AnExiledDev/SurvivalLog-PackageBank/blob/main/CONFIG.md]CONFIG.md[/url]; the cfg file explains each setting in English and Chinese). Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\package-bank.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].
Coming soon: a Survival Log wiki with detailed documentation for all of my mods.

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-PackageBank]github.com/AnExiledDev/SurvivalLog-PackageBank[/url]

[line]
[size=5]简体中文[/size]

院子里的包裹位有限。Package Bank 让你把暂时不想安装的包裹存起来，购物时也能随时存入或取出。

[size=5]主要功能[/size]
[list]
[*][b]在院子里存入[/b]：点击院子里的包裹，弹出的菜单中会多出[b]存入[/b]选项。[/*]
[*][b]仓库面板[/b]：左下角工具栏顶部的箱子按钮打开面板，列出院子里的包裹和仓库中的包裹。[b]存入[/b]把包裹放进仓库，[b]取出[/b]把它放回空的包裹位。[/*]
[*][b]在建造商店存入[/b]：商店包裹列表中每个已购买的包裹旁，退货按钮边上多了一个[b]存入[/b]按钮。[/*]
[*][b]商店里显示仓库[/b]：仓库中的包裹列在商店包裹列表下方，每个都有[b]取出[/b]按钮。[/*]
[*][b]尺寸上限[/b]：仓库默认可存 4 个大型、6 个中型和 8 个小型包裹。可在配置中修改或关闭上限。[/*]
[*][b]剧情物品[/b]：免费的一次性包裹（例如医院里的记录仪）放在单独的一栏，不限数量。[/*]
[*][b]防御设施[/b]：沙包、尖刺障碍等防御设施包裹放在单独的一栏，默认 4 个。[/*]
[*][b]按钮会说明原因[/b]：变灰的按钮会显示原因，例如该尺寸已满或没有空的包裹位。[/*]
[/list]

[size=5]快捷键[/size]
[list]
[*][b]Esc[/b]：关闭面板。[/*]
[/list]
没有打开面板的快捷键，请使用 HUD 按钮。

仓库保存在你的存档里。开始新的轮回会清空仓库，开始前请先取出想保留的包裹。

[size=5]已知问题[/size]
[list]
[*]开始新的轮回会清空仓库。[/*]
[*]想退款仓库中的包裹，请先取出，再用商店的退货按钮。[/*]
[*]搜刮来的包裹进出仓库后仍然不能退款。[/*]
[*]正在搬运或安装的包裹不能存入。车上和门口的箱子不能存入。[/*]
[*]外出探索时，面板和 HUD 按钮会隐藏。[/*]
[*]仓库中的包裹不在院子里，灾前倒计时中的准备清单不会把它们计算在内。[/*]
[/list]

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19131。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面修改的设置无需重启游戏即可生效；没装则在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackageBank.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]PackageBank.dll[/font]。卸载前请先把仓库里的包裹全部取出，模组删除后仓库中的包裹会丢失。

[size=5]配置[/size]
如需修改各尺寸的上限或关闭上限，请在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.packagebank.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-PackageBank/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。也可以在游戏内 Mod Manager 的「模组」界面修改；没装 Exiled's Toolkit 时，修改在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在这里留言。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\package-bank.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。
即将推出：Survival Log 维基，收录我所有模组的详细说明。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-PackageBank]github.com/AnExiledDev/SurvivalLog-PackageBank[/url]
