using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MoonCleaners;
using UnityEngine;

namespace MoonSweepers.MoonRadio;

[BepInPlugin("moon-sweepers.moon-radio", "Moon Radio", "0.1.1")]
public sealed class Plugin : BasePlugin
{
    internal static BepInEx.Logging.ManualLogSource Logger;
    public override void Load(){Logger=Log;new Harmony("moon-sweepers.moon-radio").PatchAll();Log.LogInfo("Moon Radio loaded. R opens internet radio in stations and vehicles. Each radio has its own 200m positional speaker.");}

    [HarmonyPatch(typeof(MoonSolarSystem),"LateUpdate")]
    static class Tick
    {
        static float lastError;
        static void Postfix()
        {
            try{RadioRuntime.Tick();}
            catch(Exception ex){if(Time.unscaledTime-lastError>5){lastError=Time.unscaledTime;Logger.LogError(ex);}}
        }
    }
    [HarmonyPatch(typeof(MoonRadioMenu),"Update")]
    static class NativeRadio {static bool Prefix()=>RadioRuntime.Walker==null;}
    [HarmonyPatch(typeof(MoonWalker),"OnDestroy")]
    static class LeaveWorld {static void Prefix(MoonWalker __instance){if(RadioRuntime.Walker==__instance)RadioRuntime.Reset();}}
}
