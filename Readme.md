# Magic Storage

Are you tired of having a mess of chests in your base? Never remember where you put your items, and have to run across your entire house to get from chest to chest? This mod will solve all of your problems!

This mod offers a solution to storage problems once and for all. It allows you to construct a central network to store all your items, that you can access from one single block. If desired, you can even set up multiple access points to use your storage from anywhere in the world. You can search your storage for items with a certain name, filter by item types, etc. The magic storage can even craft items for you!

The magic storage scales as you progress in your playthrough. It is accessible very early in the game, but with limited power. As you defeat bosses and earn more materials, you will be able to upgrade your storage to perform more functions and more easily expand the storage capacity.

Are you unable to keep track of the dozens of crafting stations in your base? This mod will help you to keep track of all of them with its Combined Crafting Stations! The Combined Crafting Stations combine the functionality of several crafting stations into each tier, with the next tier having all of the previous tiers' crafting stations.

## Credits
* [@AdipemDragon](https://forums.terraria.org/index.php?members/adipemdragon.2930/) - Spriting

## Contributing

### Dependencies
* Join the [Discord Server](https://discord.gg/FemPG7eev4) to discuss
* [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (`global.json` uses `8.0.422` as the reproducible baseline)
* tModLoader 1.4.4
* SerousCommonLib (absoluteAquarian Utilities)
	- Download the files from [the latest GitHub release](https://github.com/absoluteAquarian/SerousCommonLib/releases/latest).
	- You can also clone its GitHub repository and build the solution in Visual Studio or via `dotnet build`.

### Setting Up the Project Environment
Before proceeding further, find your saves directory.  It should be at these locations:
- (Windows) `Documents/My Games/Terraria/tModLoader/`
- (Mac) `~/Library/Application support/Terraria/tModLoader/`
- (Linux) `~/.local/share/Terraria/tModLoader/`

This folder will be indicated by `<saves>` hereon.

Next, follow these instructions to properly downloaded and set up:
1. Run `git clone https://github.com/blushiemagic/MagicStorage.git` in `<saves>/ModSources/` to clone the repository.
2. Create folders to make the folder path to `<saves>/ModSources/references/`.
3. Download the SerousCommonLib `.dll`, `.pdb` and `.xml` assembly files from the GitHub release and move them to that `references` folder.
   Alternatively, if you've cloned SerousCommonLib to `ModSources`, then you can just build its project and the relevant files will be copied to `references` automatically.
4. If you're using Visual Studio 2022 Community, open the `.sln` file in the folder created by Step 1, then either press F6 or select `Build > Build Solution`.  
   Otherwise, run `dotnet build` in `<saves/ModSources/MagicStorage/`.  
   The SDK may roll forward to a compatible .NET 8 patch installed on the machine.
5. Click play!
