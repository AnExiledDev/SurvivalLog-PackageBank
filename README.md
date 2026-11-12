# Package Bank

A BepInEx mod for Survival Log. Store furniture packages from your yard in a bank and take them out again when you have room or need them.

## What it does

- **Bank button on the HUD:** a box button at the top of the bottom-left tool column opens the bank panel. Press it again or press `Esc` to close the panel. There is no hotkey.
- **The panel** lists the packages in your yard ("In the yard") and the packages in the bank ("In the bank"), each grouped by shelf. `Bank` moves a package from the yard into the bank. `Take out` puts a banked package back on a free package spot in the yard. The number of free spots shows beside the bank's heading.
- **Shelf cards:** a card for each shelf along the top shows how many packages are on it out of its limit, and turns red when the shelf is full. Click a card to show only that shelf in both lists. Click it again, or `Show all` at the bottom, to show everything.
- **Bank from the yard:** click a package in your yard and its menu (Install, Return) gets a `Bank` entry that moves it straight into the bank.
- **Pack up placed furniture:** click a piece of furniture in your home that the Layout screen lets you both move and remove, and its menu gets a `Pack up` entry. The piece turns back into its package, in its color. The package goes on a free package spot in the yard, or into the bank if the yard has no free spot and its shelf has room. The shop's Refund pays the current price for it, wherever it came from. A storage piece has to be empty first. While a piece can't come down (switched on, in use, a plant growing), the entry is greyed out and shows the game's reason. Story pieces can be packed up too, and their entry warns you that the story uses them. Set `PackUp` to `false` to remove the entry.
- **In the build shop:** every bought package in the shop's package list gets a `Bank` button next to its Refund button, and the banked packages are listed below the shop's list with their icons, each with `Take out`. You can move packages in and out of the bank while you shop.
- **Size limits:** you set how many large, medium, small and defense packages (sandbags, spike barriers and the like) the bank holds, or turn the limits off. By default it holds 4 large, 6 medium, 8 small and 4 defense packages.
- **Story Items:** free one-off packages, such as the Recorder from the hospital, go on a Story Items shelf with no limit.
- **Defenses:** sandbags, spike barriers and the other defense packages go on a Defenses shelf of their own. With Defense Manager 0.9.0 or newer, you can install a banked defense straight from Defense Manager's panel. It is taken out onto a free package spot and installed.
- **Greyed-out buttons say why:** when a shelf is full, its group in the yard list says `bank full`. When the yard has no free package spot, a notice at the top of the bank list says so. A package being moved or installed says so under its name.
- **UI size:** Exiled's Toolkit's UI size setting (`UiScale`) draws the bank panel bigger or smaller, as long as it still fits on the screen. The HUD button and the Bank buttons in the build shop and the click menu stay the same size as the game's buttons. Package Bank's own `UiScale` under Interface sets a size for this mod alone. At 0, the default, it follows the Toolkit's.

Works in English and Chinese.

## Good to know

- **A new loop empties the bank.** Anything still in the bank when a new loop starts is gone.
- **To refund a banked package, take it out first**, then use the shop's Refund button.
- **Looted packages stay non-refundable** after a trip through the bank. A packed-up piece is the exception: it can always be refunded.
- **On the Layout screen you can't click a packed-up package**, or any other yard package. Leave Layout, then install it.
- **Pack up only works on what the Layout screen can remove.** The basement entrance, the ladders between floors and the furniture in the shops you visit before the disaster have no Pack up.
- **Fixtures packed up with 0.7.0 go back where they were.** Version 0.7.0 could pack up the basement entrance, the ladders and other fixtures with no build spot (some lamps and workbenches too). Each one goes back where it was the next time you load the save at home, and its package or bank entry goes away. A fixture that came with items in it, or one the story still keeps hidden, is left as it is. With Cheat Menu installed, the "Fix the basement entrance and ladders" button on its Mod tests tab does this right away.
- **A packed-up piece loses its custom name.** A package has no name of its own, so the piece comes back with its normal name.
- A package being moved or installed can't be banked. Packages in the car's spot and the boxes by the doors can't be banked either.
- On an expedition the panel and the HUD button are hidden.
- Banked packages are not in the yard, so the preparation checklist in the countdown before the disaster does not count them.
- The bank is kept in your save file.

## Requirements

- Survival Log 1.1.20719. A game update can break it until the mod is updated.
- [BepInEx for Survival Log](https://www.nexusmods.com/survivallog/mods/12). Install it first.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.19.0 or newer. Package Bank does not load without it. It shows Package Bank's messages as toasts while the bank panel is closed, applies settings changed on the Mod Manager's Mods screen without a restart, and its UI size setting sizes the bank panel.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.

## Install

1. Install BepInEx. If no other mods were installed before, start the game once so BepInEx finishes setup, then quit.
2. Download `PackageBank-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-PackageBank/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/79), and extract it into the game folder (the folder with `SurvivalLog.exe`), merging the folders. `PackageBank.dll` ends up in `BepInEx\plugins\`.
3. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).

## Uninstall

Delete `PackageBank.dll` from `BepInEx\plugins`. Take everything out of the bank first: packages still in the bank are lost once the mod is gone.

## Configuration

See [CONFIG.md](CONFIG.md). Each world can keep its own size limits. Pick them for a new world in Customizable Difficulty, or change them for the world you're playing on the Mod Manager's Mods screen.

## Troubleshooting

The mod writes `BepInEx\package-bank.log`. A working install logs `===== Package Bank 0.7.3 started` when the game starts. If the HUD button never shows, check that line and look for errors in `BepInEx\LogOutput.log`.

## Feedback

Found a bug or have an idea? Come say hi on the [Core Directive Discord](https://discord.gg/trTsyxUFqg). For bugs, please include `BepInEx\LogOutput.log` and `BepInEx\package-bank.log`.

To reach me directly, message `exiled.dev` on Discord. You can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Build

```
dotnet build src/PackageBank -c Release -p:GameDir="C:/path/to/Survival Log"
dotnet test tests/PackageBank.Tests
```

## License

MIT, by AnExiledDev. See [LICENSE](LICENSE).
