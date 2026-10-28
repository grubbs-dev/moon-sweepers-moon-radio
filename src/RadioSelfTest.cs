using MoonCleaners;
using UnityEngine;

namespace MoonSweepers.MoonRadio;
// Explicit opt-in integration tests. Temporary objects never enter game save state.
internal static class RadioSelfTest
{
    static GameObject firstAnchor,secondAnchor;
    static RadioEmitter first,second;
    static float deadline;
    public static void Start(Vector3 ear)
    {
        firstAnchor=new GameObject("MoonRadioTestStation");secondAnchor=new GameObject("MoonRadioTestVehicle");
        firstAnchor.transform.position=ear+Vector3.right;secondAnchor.transform.position=ear-Vector3.right;
        first=new RadioEmitter("test:station","Test station",firstAnchor.transform);
        second=new RadioEmitter("test:vehicle","Test vehicle",secondAnchor.transform);
        first.Select("https://ice2.somafm.com/groovesalad-128-mp3","Groove Salad");
        second.Select("https://ice2.somafm.com/dronezone-128-mp3","Drone Zone");
        deadline=Time.unscaledTime+45;
        foreach(var item in MoonPrototypeCatalog.Vehicles)
        {
            bool expected=!item.name.StartsWith("Push",StringComparison.OrdinalIgnoreCase);
            if(RadioRuntime.Eligible(item.name)!=expected)
                throw new Exception("Vehicle eligibility test failed: "+item.name);
        }
        Plugin.Logger.LogInfo("SELFTEST: vehicle catalog exclusion passed; starting two independent positional streams.");
    }
    public static void Tick(Vector3 ear)
    {
        if(first==null)return;
        first.Tick(ear,0.12f);second.Tick(ear,0.12f);
        if(first.Verified&&second.Verified)
        {
            bool independent=first.Playing&&second.Playing&&first.Url!=second.Url;
            secondAnchor.transform.position+=Vector3.right*12;
            second.Tick(ear,0.12f);
            bool follows=Vector3.Distance(second.SpeakerPosition,secondAnchor.transform.position)<0.01f;
            first.Tick(firstAnchor.transform.position+Vector3.right*201,0.12f);
            bool cutoff=first.SpeakerVolume==0;
            first.Select("","");
            bool isolatedStop=!first.Playing&&second.Playing;
            Plugin.Logger.LogInfo($"SELFTEST RESULT independentStreams={independent} movingSpeaker={follows} cutoff200m={cutoff} isolatedStop={isolatedStop}");
            Stop();
            if(!independent||!follows||!cutoff||!isolatedStop)throw new Exception("Moon Radio integration self-test failed.");
        }
        else if(Time.unscaledTime>deadline){Plugin.Logger.LogError("SELFTEST: timed out waiting for non-silent Unity output from both streams.");Stop();}
    }
    public static void Stop()
    {
        first?.Dispose();second?.Dispose();first=null;second=null;
        if(firstAnchor!=null)UnityEngine.Object.Destroy(firstAnchor);
        if(secondAnchor!=null)UnityEngine.Object.Destroy(secondAnchor);
        firstAnchor=null;secondAnchor=null;
    }
}
