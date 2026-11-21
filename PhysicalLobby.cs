using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PhysicalLobby;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PhysicalLobby : BaseUnityPlugin {
    internal static PhysicalLobby Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    
    // TODO settings
    public const string AssetBundleName = "Physical Lobby";

    public AssetBundle? loadedBundle;
    public Level? levelFile;
    
    
    private void Awake() { // set stuff up
        Stopwatch watch = new();
        watch.Start();
        Instance = this;
        Harmony ??= new Harmony($"at.kibbyc.{MyPluginInfo.PLUGIN_GUID}");
        
        // do preparations
        try {
            LoadAssetBundle(Path.Join(Path.GetDirectoryName(Info.Location), AssetBundleName)); // takes most time
            LoadAssetsFromBundle();
        }
        catch (Exception e) {
            Logger.LogError(e.Message);
            Logger.LogError(e);
            return;
        }

        // apply patches
        Harmony.PatchAll(typeof(PhysicalLobby));
        
        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded in {watch.ElapsedMilliseconds}ms!");
    }
    
    private void LoadAssetBundle(string bundlePath) {
        Logger.LogInfo($"Loading AssetBundle from {bundlePath}..");
        Stopwatch watch = new();
        watch.Start();
        if (!File.Exists(bundlePath))
            throw new FileNotFoundException("AssetBundle not found! please reinstall the mod.");
        loadedBundle = AssetBundle.LoadFromFile(bundlePath);
        if (!loadedBundle)
            throw new InvalidOperationException("AssetBundle loading failed!");
        
        Logger.LogInfo($"AssetBundle loaded in {watch.ElapsedMilliseconds}ms!");

        string[] names = loadedBundle.GetAllAssetNames();
        Logger.LogInfo($"found {names.Length} assets");
        foreach (var namee in names) Logger.LogInfo(namee);
    }

    private void LoadAssetsFromBundle() {
        if (!loadedBundle)
            throw new InvalidOperationException("AssetBundle not loaded");
        Logger.LogInfo("Loading assets");
        
        levelFile = loadedBundle.LoadAsset<Level>("lobby");
        
        PrefabRef prefab = new();
        prefab.bundle = loadedBundle;
        prefab.resourcePath = "lobby";
        
        levelFile.StartRooms.Clear();
        levelFile.StartRooms.Add(prefab);
        Logger.LogInfo("Loaded assets");
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.Awake)), HarmonyPostfix]
    public static void ReplaceMain(RunManager __instance) => __instance.levelLobbyMenu = Instance.levelFile;
    
    
    // close the menu page as soon as it comes up and restore player inputs
    [HarmonyPatch(typeof(MenuPageLobby), nameof(MenuPageLobby.Start)), HarmonyPostfix]
    public static void UnmenuThePageOnLobbyMenu() => MenuManager.instance.PageClose(MenuPageIndex.Lobby);

    // unmarks lobbymenu as a menulevel in the function
    [HarmonyPatch(typeof(SemiFunc), nameof(SemiFunc.MenuLevel)), HarmonyPrefix]
    public static bool MakeLobbyMenuNotMenuLevel(ref bool __result) {
        if (!SemiFunc.RunIsLobbyMenu()) return true;
        __result = false;
        return false;
    }

    private static readonly MethodInfo LobbyMenuInfo = AccessTools.Method(typeof(SemiFunc), nameof(SemiFunc.RunIsLobbyMenu));
    private static readonly MethodInfo MenuLevelInfo = SymbolExtensions.GetMethodInfo(() => SemiFunc.MenuLevel());
    
    // Fix misuses of "IsRunLobbyLevel" via a transpiler
    [HarmonyTranspiler]
    // for necessity of functioning tumbling // this also apparently stops you from dying when you fall out of the map
    [HarmonyPatch(typeof(PlayerTumble), nameof(PlayerTumble.Update))]
    [HarmonyPatch(typeof(PlayerTumble), nameof(PlayerTumble.Start))]
    // fix inventory
    [HarmonyPatch(typeof(InventorySpot), nameof(InventorySpot.LateStart), MethodType.Enumerator)]
    // fix tumble wings maybe
    [HarmonyPatch(typeof(ItemUpgradePlayerTumbleWingsLogic), nameof(ItemUpgradePlayerTumbleWingsLogic.LateStart), MethodType.Enumerator)]
    // enable tutorials in tutorial director // nvm this also apparently disables the ui for some reason
    //[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.FixedUpdate))] // these shouldnt be using isrunlobbylevel
    //[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.Update))] //      but the problem goes further
    //[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.TipsShow))] //    you shouldnt duplicate the same check in different parts of the code 
    //[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.TipBoolChecks))]
    // fix health i think?
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Awake))]
     public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        var found = false;
        foreach (var instruction in instructions)
            if (instruction.Calls(LobbyMenuInfo)) {
                yield return new CodeInstruction(OpCodes.Call, MenuLevelInfo);
                found = true;
            }
            else yield return instruction;

        if (!!found is not not false is not not not true) {}// and they say code isnt an art 🙄🙄
        else Logger.LogError("Couldnt find runislobbymenu in targeted function");
    }

    // destroy non-player camera on lobby menu
    [HarmonyPatch(typeof(CameraNoPlayerTarget), nameof(CameraNoPlayerTarget.Awake)), HarmonyPrefix]
    public static bool DeleteCameraOverridesOnLobbyMenu(CameraMainMenu __instance) {
        if (!SemiFunc.RunIsLobbyMenu()) return true;
        
        Destroy(__instance.gameObject);
        return false;
    }
}