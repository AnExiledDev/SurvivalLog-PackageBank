# Changelog

## 0.7.3

- Clearer wording on screen and on the Mods screen.
- Made for Survival Log 1.1.20719.

## 0.7.2

- New setting `PackUp`, on by default. Set it to false to take Pack up out of the click menu on placed furniture. The change applies right away.
- Made for Survival Log 1.1.20719.

## 0.7.1

- Fixed: Pack up no longer shows on the basement entrance, or on anything else the Layout screen can't remove. A packed-up basement entrance could not be put back.
- Fixed: Pack up no longer shows on the ladders between floors, or on anything else that doesn't sit on a build spot. Their packages could not be installed anywhere.
- Fixed: Pack up no longer shows on furniture in the shops you visit before the disaster. Pressing it there gave an error.
- If you packed up a basement entrance, a ladder or another fixture with no build spot (some lamps and workbenches too) with 0.7.0, it goes back where it was when you load the save at home. Its package, its bank entry or the copy you installed on a large spot goes away. A fixture that came with items in it, or one the story still keeps hidden, is left as it is.
- New button on Cheat Menu's Mod tests tab, "Fix the basement entrance and ladders", does the same repair right away while you're at home. If nothing is broken, it says nothing needed fixing.
- Made for Survival Log 1.1.20719.

## 0.7.0

- New: **Pack up** turns placed furniture back into its package. Click a piece you can move and its menu gets a `Pack up` entry. The package goes on a free package spot in the yard, or into the bank when the yard has none.
- A packed-up piece keeps its color, and the shop's Refund pays its current price, wherever it came from.
- Pack up is greyed out while a storage piece still holds items, and while the game won't let the piece come down (switched on, in use, a plant growing). It says why.
- Story pieces can be packed up too. Their entry warns that the story uses them.
- When you pack something up on the Layout screen, the message reminds you to leave Layout before you install it, because the game ignores clicks on yard packages there.
- Made for Survival Log 1.1.20719.

## 0.6.1

- **Warning:** needs Exiled's Toolkit 0.19.0 or newer. Update the Toolkit first.
- Now uses Exiled's Toolkit for code it shares with other mods.
- Made for Survival Log 1.1.20719.

## 0.6.0

- Each world can keep its own size limits: `LimitBySize`, `LargeSlots`, `MediumSlots`, `SmallSlots` and `DefenseSlots`. With Exiled's Toolkit 0.17.0 or newer, pick them for a new world in Customizable Difficulty, or change them for the world you're playing on the Mod Manager's Mods screen. `UiScale` stays the same for every world.
- Made for Survival Log 1.1.20151.

## 0.5.2

- Floor Plan 0.2.0 can move furniture that no longer fits its spot into the bank, keeping its color. This works for any size, even when the bank is full.
- Made for Survival Log 1.1.20151.

## 0.5.1

- **Warning:** needs Exiled's Toolkit 0.11.0 or newer. Update the Toolkit first.
- With the bank panel closed, Package Bank's messages now show as Exiled's Toolkit toasts. Those toasts also go to the Notifications history.
- Made for Survival Log 1.1.20151.

## 0.5.0

- Defense Manager 0.9.0 can install defense packages straight from the bank. Pick an empty yard square and a banked defense in its panel, and the defense is taken out onto a free package spot and installed.
- Made for Survival Log 1.1.19611.

## 0.4.0

- New: `UiScale` under Interface sets the size of Package Bank's windows by themselves, from 75% to 200%, whatever Exiled's Toolkit's UI size is. At 0, the default, they follow the Toolkit's. Needs Exiled's Toolkit 0.11.0 or newer.
- Made for Survival Log 1.1.19611.

## 0.3.4

- Fixed: the game could stutter for a moment the first time you clicked a yard package in a session.
- Made for Survival Log 1.1.19611.

## 0.3.3

- The bank panel and its pop-up messages can be drawn bigger or smaller with the UI size setting of Exiled's Toolkit 0.10.0 or newer. A change shows right away, and the panel only grows as far as it still fits on the screen.
- The HUD button and the Bank buttons in the build shop and the click menu stay the same size as the game's buttons around them.
- Made for Survival Log 1.1.19611.

## 0.3.2

- The game runs smoother with the mod installed. It now does almost no work while the bank panel, the build shop and a package's click menu are all closed, and less while they are open.
- The bank panel updates faster when a package is banked or taken out.
- Less of a stutter the first time the bank panel opens.

## 0.3.1

- The Bank entry in a package's click menu shows with the menu's other buttons, instead of a moment after them.

## 0.3.0

- A new look for the bank panel, in the warm gold style of the newer mod panels.
- A card for each shelf along the top shows how many packages are on it out of its limit, and turns red when the shelf is full.
- Click a shelf card to show only that shelf in both lists. Click it again, or Show all, to show everything.
- Both lists are grouped by shelf. A full shelf shows one note on its group.
- When the yard has no free package spot, one notice shows above the banked packages instead of a line under every Take out.
- The number of free package spots shows beside the bank's heading, and empty lists say what to do next.

## 0.2.0

- Defenses can be banked: the free starter Sandbags, Spike Barriers and the other defense packages go on a new Defenses shelf, 4 by default. New setting: `DefenseSlots`.
- The Bank entry in a yard package's menu now shows as soon as the menu opens, instead of up to a second later.
- With Exiled's Toolkit 0.6.0 or later, the HUD button lines up neatly with other mods' buttons.
- After a game update, Mod Manager can tell you if this mod may need an update.

## 0.1.0

- First release.
- A box button on the HUD opens the bank panel: bank packages from your yard and take them out again.
- Clicking a package in the yard adds a Bank entry to its menu.
- The build shop gets a Bank button beside each bought package's Refund button, and lists the banked packages below its package list with Take out.
- Size limits for large, medium and small packages, 4, 6 and 8 by default. You can change them in the config or turn them off. Story Items have no limit.
