using MoonCleaners;
using UnityEngine;

namespace MoonSweepers.MoonRadio;

// Explicit opt-in helper for a disposable slot-5 test save. It refuses to touch
// any other slot so normal progress remains separate.
internal static class RadioTestSave
{
    static bool done;
    public static void Tick(MoonWalker walker)
    {
        if(done || !Environment.GetCommandLineArgs().Contains("--moon-radio-test-save"))return;
        var world=UnityEngine.Object.FindObjectsOfType<MoonWorld>().FirstOrDefault();
        if(world==null || string.IsNullOrEmpty(world.SaveDirectory))return;
        string save=world.SaveDirectory.Replace('/','\\');
        if(!save.Contains("\\slot-5\\",StringComparison.OrdinalIgnoreCase) &&
           !save.EndsWith("\\slot-5",StringComparison.OrdinalIgnoreCase))
        {
            Plugin.Logger.LogError("Test-save helper refused to run because the active save is not slot-5: "+save);
            done=true;return;
        }
        string player=walker?.UpgradePlayer;
        if(string.IsNullOrWhiteSpace(player))return;
        done=true;
        Plugin.Logger.LogInfo($"TEST SAVE: active directory verified ({save}); player={player}");
        try
        {
            var millions=new Il2CppSystem.Decimal(10000000,0,0,false,0);
            world.DevAddMoney(player,millions);
            world.SetTeamDollars(millions);
            var state=world.PlayerUpgradeState(player);
            state.inventoryTier=99;
            foreach(var item in world.prototypeTools)state.tools.set_Item(item.Key,99);
            foreach(var node in MoonUpgradeTree.Build())state.tree.set_Item(node.id,node.maxLevel);
            foreach(var node in MoonUpgradeTree.BuildGear())state.tree.set_Item(node.id,node.maxLevel);
            world.SetPlayerUpgradeState(player,state);
            Vector3 lastRegion;
            int regions=world.DevFinishAllButOne(world.transform.position,out lastRegion);
            regions+=world.DevCompleteRegions(lastRegion,256);
            Plugin.Logger.LogInfo($"TEST SAVE: slot-5 provisioned; money target=$10,000,000; tool/tree upgrades maxed; regions={regions}; vehicle ownership left untouched");
            _=world.SaveProgress();
            Plugin.Logger.LogInfo("TEST SAVE: save requested; normal save slots were not modified.");
        }
        catch(Exception ex){Plugin.Logger.LogError("TEST SAVE failed: "+ex);}
    }
}

internal static class RadioTestVehicles
{
    static bool done;
    public static void Tick(MoonWalker walker)
    {
        if(done || !Environment.GetCommandLineArgs().Contains("--moon-radio-test-save"))return;
        var equipment=UnityEngine.Object.FindObjectsOfType<MoonPrototypeEquipment>().FirstOrDefault();
        if(equipment==null || walker==null)return;
        done=true;
        try
        {
            equipment.EnsureAssets();
            Vector3 origin=walker.transform.position;
            bool first=equipment.PlaceForTest(1,new Pose(origin+walker.transform.forward*8+walker.transform.right*4,Quaternion.identity),false);
            bool second=equipment.PlaceForTest(2,new Pose(origin+walker.transform.forward*8-walker.transform.right*4,Quaternion.identity),false);
            Plugin.Logger.LogInfo($"TEST VEHICLES: placed Ride-on sweeper={first}, Compact utility rover={second}; these are disposable runtime test vehicles");
        }
        catch(Exception ex){Plugin.Logger.LogError("TEST VEHICLES failed: "+ex);}
    }
}
