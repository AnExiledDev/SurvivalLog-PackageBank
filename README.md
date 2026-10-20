# Package Bank

A BepInEx mod for Survival Log. Store furniture packages from your yard in a bank and take them out again when you have room or need them.

## What it does

- **Bank button on the HUD:** a box button at the top of the bottom-left tool column opens the bank panel. Press it again or press `Esc` to close the panel. There is no hotkey.
- **The panel** lists the packages in your yard ("At home") and the packages in the bank ("In the bank"). `Bank` moves a package from the yard into the bank. `Take out` puts a banked package back on a free package spot in the yard.
- **Bank from the yard:** click a package in your yard and its menu (Install, Return) gets a `Bank` entry that moves it straight into the bank.
- **In the build shop:** every bought package in the shop's package list gets a `Bank` button next to its Refund button, and the banked packages are listed below the shop's list with their icons, each with `Take out`. You can move packages in and out of the bank while you shop.
- **Size limits:** the bank holds 4 large, 6 medium and 8 small packages by default. Limits can be changed or turned off in the config. Each size shows how many are banked and its limit.
- **Story Items:** free one-off packages, such as the Recorder from the hospital, go on a Story Items shelf with no limit.
- **Buttons say why not:** a greyed-out button shows the reason, such as a full size, a package being moved or installed, or no free package spot.

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

- Survival Log 1.1.18902. A game update can break it until the mod is updated.
- [BepInEx for Survival Log](https://www.nexusmods.com/survivallog/mods/12). Install it first.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.

## Install

1. Install BepInEx. If no other mods were installed before, start the game once so BepInEx finishes setup, then quit.
2. Extract the release zip into the game folder (the folder with `SurvivalLog.exe`), merging the folders. `PackageBank.dll` ends up in `BepInEx\plugins\`.

## Uninstall

Delete `PackageBank.dll` from `BepInEx\plugins`. Take everything out of the bank first: packages still in the bank are lost once the mod is gone.

## Configuration

See [CONFIG.md](CONFIG.md).

## Troubleshooting

The mod writes `BepInEx\package-bank.log`. A working install logs `===== Package Bank 0.1.0 started` when the game starts. If the HUD button never shows, check that line and look for errors in `BepInEx\LogOutput.log`.

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
