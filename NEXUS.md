Name: Package Bank
Summary: Store furniture packages from your yard in a bank and take them out when you need them, and pack placed furniture back into its package. Bank them from the yard, from a HUD panel or right in the build shop, where banked packages are listed below the shop's list. You set the size limits, defenses get their own shelf and story items have no limit.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/save/3-remove-with-care.png[/img][/center]

Your yard only has so many package spots. Package Bank gives you somewhere to put packages you are not ready to install, and lets you move them in and out while you shop.

[size=5]Main features[/size]
[list]
[*][b]Bank panel[/b]: a box button at the top of the bottom-left tool column opens a panel with the packages in your yard and the packages in the bank, grouped by shelf. [b]Bank[/b] moves a package into the bank, and [b]Take out[/b] puts it back on a free package spot.[/*]
[*][b]Shelf cards[/b]: a card for each shelf shows how full it is. Click one to show only that shelf in both lists.[/*]
[*][b]Bank from the yard[/b]: click a package in your yard and its menu gets a [b]Bank[/b] entry beside Install and Return.[/*]
[*][b]Pack up placed furniture[/b]: click a piece of furniture in your home that the Layout screen lets you both move and remove, and its menu gets a [b]Pack up[/b] entry. The piece turns back into its package, in its color. The package goes on a free package spot in the yard, or into the bank when the yard is full. The shop's Refund pays the current price for it. A storage piece has to be empty first, and when the game won't let a piece come down, the entry says why. Story pieces can be packed up too, with a warning.[/*]
[*][b]Bank from the build shop[/b]: every bought package in the shop's package list gets a [b]Bank[/b] button next to its Refund button.[/*]
[*][b]Banked packages in the shop[/b]: the bank is listed below the shop's package list, each package with [b]Take out[/b].[/*]
[*][b]Size limits[/b]: you set how many large, medium and small packages the bank holds, or turn the limits off. By default it holds 4 large, 6 medium and 8 small packages.[/*]
[*][b]Story Items[/b]: free one-off packages, such as the Recorder from the hospital, go on their own shelf with no limit.[/*]
[*][b]Defenses[/b]: sandbags, spike barriers and the other defense packages go on a Defenses shelf of their own, 4 by default. With Defense Manager 0.9.0 or newer, you can install a banked defense straight from Defense Manager's panel.[/*]
[*][b]Greyed-out buttons say why[/b]: a button you can't use shows the reason, like a full shelf or no free package spot.[/*]
[*][b]UI size[/b]: Exiled's Toolkit's UI size setting draws the bank panel bigger or smaller, as long as it still fits on the screen. Package Bank's own UiScale setting can set a size for this mod alone.[/*]
[/list]

[size=5]Hotkeys[/size]
[list]
[*][b]Esc[/b]: close the panel.[/*]
[/list]
There is no hotkey to open the panel. Use the HUD button.

The bank is kept in your save file. A new loop empties it, so take out anything you want to keep before starting one.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]A new loop empties the bank.[/*]
[*]To refund a banked package, take it out first, then use the shop's Refund button.[/*]
[*]Looted packages stay non-refundable after a trip through the bank. A packed-up piece can always be refunded.[/*]
[*]Pack up only works on what the Layout screen can remove. The basement entrance, the ladders between floors and the furniture in the shops you visit before the disaster have no Pack up.[/*]
[*]Version 0.7.0 could pack up the basement entrance, the ladders and other fixtures with no build spot (some lamps and workbenches too). Each one goes back where it was the next time you load the save at home, and its package or bank entry goes away. With Cheat Menu installed, the [b]Fix the basement entrance and ladders[/b] button on its Mod tests tab does this right away.[/*]
[*]A packed-up piece loses its custom name, because a package has no name of its own.[/*]
[*]A package being moved or installed can't be banked. Packages in the car's spot and the boxes by the doors can't be banked either.[/*]
[*]On an expedition the panel and the HUD button are hidden.[/*]
[*]Banked packages are not in the yard, so the preparation checklist in the countdown before the disaster does not count them.[/*]
[/list]

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.20719. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.19.0 or newer. Package Bank does not load without it. It shows Package Bank's messages as toasts while the bank panel is closed, applies settings changed on the Mod Manager's Mods screen without a restart, and its UI size setting sizes the bank panel.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]. With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url]: download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackageBank.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Take everything out of the bank first, then delete [font=Courier New]PackageBank.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder. Packages still in the bank are lost once the mod is gone.

[size=5]Configuration[/size]
You can change the size limits, turn them off, or take Pack up out of the click menu (PackUp). Do it in game on the Mod Manager's Mods screen, or edit [font=Courier New]BepInEx\config\com.dev.survivallog.packagebank.cfg[/font] while the game is closed. The cfg file explains each setting in English and Chinese, and [url=https://github.com/AnExiledDev/SurvivalLog-PackageBank/blob/main/CONFIG.md]CONFIG.md[/url] lists them all. Each world can keep its own size limits. Pick them for a new world in Customizable Difficulty, or change them for the world you're playing on the Mods screen.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\package-bank.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord. You can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-PackageBank]github.com/AnExiledDev/SurvivalLog-PackageBank[/url]

[line]
[size=5]简体中文[/size]

屋外空地上的包裹位有限。Package Bank 让你把暂时不想安装的包裹存起来，购物时也能随时存入或取出。

[size=5]主要功能[/size]
[list]
[*][b]仓库面板[/b]：左下角工具栏顶部的箱子按钮打开面板，按类别分组列出屋外空地上的包裹和仓库中的包裹。[b]存入[/b]把包裹放进仓库，[b]取出[/b]把它放回空的包裹位。[/*]
[*][b]类别卡片[/b]：每个类别一张卡片，显示已存了多少。点击卡片后，两边的列表只显示这一类。[/*]
[*][b]在屋外空地上存入[/b]：点击屋外空地上的包裹，弹出的菜单中会多出[b]存入[/b]选项。[/*]
[*][b]把摆好的家具打包[/b]：点击家中一件在「布局」界面里既能「移动」又能「移除」的家具，弹出的菜单中会多出[b]打包[/b]选项。家具会变回包裹，保留颜色，放在屋外空地上空的包裹位上，屋外空地满了就存入仓库。商店的退货按钮会按当前价格退款。储物家具要先清空。游戏不允许拆下某件家具时，选项会说明原因。剧情家具也可以打包，但会有提醒。[/*]
[*][b]在建造商店存入[/b]：商店包裹列表中每个已购买的包裹旁，退货按钮边上多了一个[b]存入[/b]按钮。[/*]
[*][b]商店里显示仓库[/b]：仓库中的包裹列在商店包裹列表下方，每个都有[b]取出[/b]按钮。[/*]
[*][b]尺寸上限[/b]：大型、中型和小型包裹各能存多少由你设置，也可以关闭上限。默认可存 4 个大型、6 个中型和 8 个小型包裹。[/*]
[*][b]剧情物品[/b]：免费的一次性包裹（例如医院里的记录仪）有单独的类别，不限数量。[/*]
[*][b]防御设施[/b]：沙包、尖刺障碍等防御设施包裹有单独的类别，默认 4 个。使用 Defense Manager 0.9.0 或更新版本时，可以直接在 Defense Manager 的面板中安装仓库里的防御设施。[/*]
[*][b]变灰的按钮会说明原因[/b]：不能用的按钮会显示原因，例如这一类已存满或没有空的包裹位。[/*]
[*][b]界面大小[/b]：Exiled's Toolkit 的界面大小设置可以放大或缩小仓库面板，只要面板还能完整显示在屏幕上。也可以用 Package Bank 自己的 UiScale 设置单独调整本模组的大小。[/*]
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
[*]搜刮来的包裹进出仓库后仍然不能退款。打包得来的包裹总是可以退款。[/*]
[*]只有「布局」界面能移除的家具才能打包。地下室入口、楼层之间的楼梯和灾前去过的商店里的家具没有打包选项。[/*]
[*]0.7.0 版本可以打包地下室入口、楼梯和其他没有建造位的家具（也包括一些灯和工作台）。下次在家中读档时，它们会回到原来的位置，对应的包裹或仓库条目会被移除。装了 Cheat Menu 的话，它「模组测试」页里的「修复地下室入口和楼梯」按钮可以立即修复。[/*]
[*]家具打包后会失去自定义名称，因为包裹没有自己的名称。[/*]
[*]正在搬运或安装的包裹不能存入。车位上的包裹和门口的箱子也不能存入。[/*]
[*]外出探索时，面板和 HUD 按钮会隐藏。[/*]
[*]仓库中的包裹不在屋外空地上，灾前倒计时中的准备清单不会把它们计算在内。[/*]
[/list]

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.20719。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.19.0 或更新版本。没有它本模组不会加载。仓库面板关闭时，它把 Package Bank 的消息显示为弹出通知。它还让在 Mod Manager 的「模组」界面修改的设置无需重启即可生效，它的界面大小设置也能调整仓库面板的大小。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]。装了它，可以在游戏里它的「模组」界面修改每项设置，并看到每项的说明，不用打开配置文件。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url]：下载它的压缩包，解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\PackageBank.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]PackageBank.dll[/font]。卸载前请先把仓库里的包裹全部取出，模组删除后仓库中的包裹会丢失。

[size=5]配置[/size]
可以修改各尺寸的上限、关闭上限，或把「打包」从点击菜单中去掉（PackUp）。可以在游戏内 Mod Manager 的「模组」界面修改，也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.packagebank.cfg[/font]。配置文件中每项设置都有中文说明，完整列表见 [url=https://github.com/AnExiledDev/SurvivalLog-PackageBank/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。每个存档都可以有自己的尺寸上限：新存档可以在「自定义难度」里选，游戏中也可以在「模组」界面只为当前存档修改。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在这里留言。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\package-bank.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-PackageBank]github.com/AnExiledDev/SurvivalLog-PackageBank[/url]
