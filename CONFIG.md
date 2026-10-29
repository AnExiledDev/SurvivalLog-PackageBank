# Package Bank settings

Settings live in `BepInEx\config\com.dev.survivallog.packagebank.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Delete the file to get the defaults back. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start.

## Bank

| Setting | Default | What it does |
| --- | --- | --- |
| `LimitBySize` | `true` | Limits how many packages of each size the bank holds. `false` lets the bank hold any number of every size. |
| `LargeSlots` | `4` | How many large packages the bank holds. Any number from 0 to 100. |
| `MediumSlots` | `6` | How many medium packages the bank holds. Any number from 0 to 100. |
| `SmallSlots` | `8` | How many small packages the bank holds. Any number from 0 to 100. |
| `DefenseSlots` | `4` | How many defense packages (sandbags, spike barriers and other defenses) the bank holds. Any number from 0 to 100. |

Story Items have no limit. Lowering a limit below what is already banked keeps those packages in the bank; nothing more of that size goes in until the count drops below the limit.

## Interface

| Setting | Default | What it does |
| --- | --- | --- |
| `UiScale` | `0` | How big Package Bank's own windows, panels and messages are drawn, in percent, from 75 to 200. At `0` they follow the UI size set in Exiled's Toolkit (its `UiScale`). Needs Exiled's Toolkit 0.11.0 or newer. |
