# About

A mod for Owlcat's Pathfinder: Wrath of the Righteous.

Mount all your animal companions automatically with a customisable keybind. Adapted for compatibility with TabletopTweaks and its Undersized Mount feat, which allows a rider to mount a pet of the same size class (e.g. human riding a level 1 wolf).

# Install
1. Download and install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) and set it up for WOTR ("Pathfinder Second Adventure").
1. Download [AutoMount for TabletopTweaks](https://github.com/DarthParametric/AutoMount_for_TabletopTweaks/releases/latest).
1. Download [ModMenu](https://github.com/WittleWolfie/ModMenu/releases/latest) for a prettier in-game mod settings menu.
1. Drag the mod zips into Unity Mod Manager.
1. Run your game.

### Optional:
As-of v1.2.0, TabletopTweaks is no longer a hard requirement for the mod's base functionality. However, it still supports the Undersized Mount feat if present. To make use of the feat, additionally download and install [TabletopTweaks-Core](https://github.com/Vek17/TabletopTweaks-Core/releases/latest) and [TabletopTweaks-Base](https://github.com/Vek17/TabletopTweaks-Base/releases/latest).

# Features
- Allows mount/dismount with a customisable keybind.
    - Mount: **Ctrl+Shift+A** (default).
    - Dismount: **Ctrl+Shift+D** (default).
- Auto mount when entering an area (toggleable).
- Whitelist/blacklist party members mounting by party slot position (default is all enabled). Includes provision for larger parties via the [More Party Slots](https://github.com/xADDBx/MorePartySlots/releases/latest) mod, if it is installed.

# Notes
- Not compatible with zephe0n's original version of AutoMount. Pick one or the other.
- Not available via ModFinder (it installs zephe0n's version). Install this version via UMM or manually extract into the game's Mod folder.
- Even though you can technically mount/dismount anywhere, any time (as long as the game allows it) it **will** consume your move action if you perform it while in combat.
- When using the mount hotkey, riders will instantly teleport directly onto their respective mounts, regardless of distance, with no animation. There are no plans to change this.
- If using the ToyBox cheat to autorest at the end of combat, you will be dismounted. There are no plans to change this.
- Enabling the option to auto mount when entering an area may cause issues in certain cutscenes (e.g. Arueshalae dream sequences), but the dismount hotkey should work in these cases.

# Thanks & Acknowledgements
- zephe0n - Author of the original AutoMount, which provides 95% of the mod's core functionality.
- xADDBx - Provided advice on the initial implementation of Undersized Mount support and general newb hand-holding on Visual Studio project setup and compiling.
- microsoftenator2022 - Provided the full code for the function to check for the presence of More Party Slots and parse its config file, and provided corrections, fixes and suggestions for various coding issues.
- AlterAsc - Provided advice and code snippets for an alternative implementation of checking mount size and validity, thereby fixing a bug and removing the hard TTT dependency.
- Everyone in the `#mod-dev-technical` channel of the Owlcat Discord server for various modding-related discussions and suggestions, help troubleshooting issues, and answering general questions.
