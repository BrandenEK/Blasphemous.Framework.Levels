﻿using BepInEx;

namespace Blasphemous.Framework.Levels;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "2.4.1")]
internal class Main : BaseUnityPlugin
{
    public static LevelFramework LevelFramework { get; private set; }
    public static Main Instance { get; private set; }

    private void Start()
    {
        Instance = this;
        LevelFramework = new LevelFramework();
    }
}
