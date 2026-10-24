# Package Bank

A BepInEx mod for Survival Log. Store furniture packages from your yard in a bank and take them out again when you have room or need them.

## What it does

- **Bank button on the HUD:** a box button at the top of the bottom-left tool column opens the bank panel. Press it again or press `Esc` to close the panel. There is no hotkey.
- **The panel** lists the packages in your yard ("In the yard") and the packages in the bank ("In the bank"), each grouped by shelf. `Bank` moves a package from the yard into the bank. `Take out` puts a banked package back on a free package spot in the yard; the number of free spots shows beside the bank's heading.
- **Shelf cards:** a card for each shelf along the top shows how many packages are banked on it against its limit, and turns red when the shelf is full. Click a card to show only that shelf in both lists; click it again, or `Show all` at the bottom, to show everything.
- **Bank from the yard:** click a package in your yard and its menu (Install, Return) gets a `Bank` entry that moves it straight into the bank.
- **In the build shop:** every bought package in the shop's package list gets a `Bank` button next to its Refund button, and the banked packages are listed below the shop's list with their icons, each with `Take out`. You can move packages in and out of the bank while you shop.
- **Size limits:** the bank holds 4 large, 6 medium and 8 small packages, and 4 defenses (sandbags, spike barriers and the like), by default. Limits can be changed or turned off in the config. Each shelf card shows how many are banked and its limit.
- **Story Items:** free one-off packages, such as the Recorder from the hospital, go on a Story Items shelf with no limit.
- **Defenses:** sandbags, spike barriers and the other defense packages go on a Defenses shelf of their own.
- **Buttons say why not:** a greyed-out button shows the reason. A full shelf says `bank full` once on its group in the yard list, no free package spot shows one notice at the top of the bank list, and a package being moved or installed says so under its name.

Works in English and Chinese.

## Good to know

- **A new loop empties the bank.** Anything still in the bank when a new loop starts is gone.
- **To refund a banked package, take it out first**, then use the shop's own Refund button.
- **Looted packages stay non-refundable** after a trip through the bank.
- A package being moved or installed cannot be banked. Car and door boxes cannot be banked.
- On an expedition the panel and the HUD button are hidden.
- Banked packages are not in the yard, so the preparation checklist in the countdown before the disaster does not count them.
- The bank is kept in your save file.

## Requirements

- Survival Log 1.1.19611. A game update can break it until the mod is updated.
- [BepInEx for Survival Log](https://www.nexusmods.com/survivallog/mods/12). Install it first.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.

## Install

1. Install BepInEx. If no other mods were installed before, start the game once so BepInEx finishes setup, then quit.
2. Download `PackageBank-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-PackageBank/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/79), and extract it into the game folder (the folder with `SurvivalLog.exe`), merging the folders. `PackageBank.dll` ends up in `BepInEx\plugins\`.

## Uninstall

Delete `PackageBank.dll` from `BepInEx\plugins`. Take everything out of the bank first: packages still in the bank are lost once the mod is gone.

## Configuration

See [CONFIG.md](CONFIG.md).

## Troubleshooting

The mod writes `BepInEx\package-bank.log`. A working install logs `===== Package Bank 0.3.0 started` when the game starts. If the HUD button never shows, check that line and look for errors in `BepInEx\LogOutput.log`.

## Feedback

Found a bug or have an idea? Come say hi on the [Core Directive Discord](https://discord.gg/trTsyxUFqg). For bugs, please include `BepInEx\LogOutput.log` and `BepInEx\package-bank.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Build

```
dotnet build src/PackageBank -c Release -p:GameDir="C:/path/to/Survival Log"
dotnet test tests/PackageBank.Tests
```

## License

MIT, by AnExiledDev. See [LICENSE](LICENSE).
