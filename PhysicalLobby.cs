using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace PhysicalLobby;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PhysicalLobby : BaseUnityPlugin {
    internal static PhysicalLobby Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }
    
    private void Awake() { // set stuff up
        Instance = this;
        Harmony ??= new Harmony($"at.kibbyc.{MyPluginInfo.PLUGIN_GUID}");
        Harmony.PatchAll(typeof(PhysicalLobby));
        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    [HarmonyPatch(typeof(MenuPageLobby), nameof(MenuPageLobby.Start)), HarmonyPostfix]
    public static void UnmenuThePageOnLobbyMenu() => MenuManager.instance.PageClose(MenuPageIndex.Lobby);

    [HarmonyPatch(typeof(SemiFunc), nameof(SemiFunc.MenuLevel)), HarmonyPrefix]
    public static bool MakeLobbyMenuNotMenuLevel(ref bool __result) {
        if (!SemiFunc.RunIsLobbyMenu()) return true;
        __result = false;
        return false;
    }

    private static readonly MethodInfo LobbyMenuInfo = AccessTools.Method(typeof(SemiFunc), nameof(SemiFunc.RunIsLobbyMenu));
    private static readonly MethodInfo MenuLevelInfo = SymbolExtensions.GetMethodInfo(() => SemiFunc.MenuLevel());
    
    // Fix misuses of "IsRunLobbyLevel"
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