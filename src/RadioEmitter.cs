using MoonSweepers.MoonRadio.Audio;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace MoonSweepers.MoonRadio;
internal sealed class RadioEmitter : IDisposable
{
    public string Key,Name;
    public Transform Anchor;
    public string Url="",Station="";
    LiveStream stream;
    AudioSource source;
    AudioClip clip;
    GameObject speaker;
    AudioClip.PCMReaderCallback callback;
    float[] buffer=Array.Empty<float>();
    bool measured;
    float retryAt;
    long consumed;
    internal bool Verified=>measured;
    internal bool Playing=>source!=null&&source.isPlaying;
    internal Vector3 SpeakerPosition=>speaker!=null?speaker.transform.position:Vector3.zero;
    internal float SpeakerVolume=>source!=null?source.volume:0;
    bool IsVehicle=>Key.StartsWith("v:",StringComparison.OrdinalIgnoreCase);
    public string Status=>string.IsNullOrEmpty(Url)?"Radio off":stream?.Error!=null?"Stream unavailable — retrying":source!=null&&source.isPlaying?"Playing": "Buffering…";
    public RadioEmitter(string key,string name,Transform anchor){Key=key;Name=name;Anchor=anchor;}
    public void Select(string url,string station)
    {
        if(Url==url){Station=station;return;}
        StopAudio();Url=url;Station=station;retryAt=0;
        Plugin.Logger.LogInfo($"Radio {Key}: {(url.Length==0?"off":station)}");
    }
    public void Tick(Vector3 listener,float volume)
    {
        if(Anchor==null||string.IsNullOrEmpty(Url)||Vector3.Distance(listener,Anchor.position)>205f){StopAudio();return;}
        if(stream==null && Time.unscaledTime>=retryAt){stream=new LiveStream(Url);measured=false;consumed=0;}
        if(stream==null)return;
        if(stream.Error!=null || (stream.Finished&&stream.Buffered==0))
        {
            Plugin.Logger.LogWarning($"Radio {Key}: stream disconnected; retrying in 10s. {stream.Error}");
            StopAudio();retryAt=Time.unscaledTime+10;return;
        }
        if(source==null && stream.SampleRate>0 && stream.Buffered>=stream.SampleRate)
        {
            speaker=new GameObject("MoonRadioSpeaker");
            source=speaker.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=true;source.spatialBlend=1;source.dopplerLevel=0;
            source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=IsVehicle?3:8;source.maxDistance=IsVehicle?15:200;
            callback=(Action<Il2CppStructArray<float>>)Fill;
            clip=AudioClip.Create("MoonRadioLive",stream.SampleRate*4,1,stream.SampleRate,true,callback);
            source.clip=clip;source.volume=volume;speaker.transform.position=Anchor.position;
            source.Play();
            Plugin.Logger.LogInfo($"Speaker started {Key}: {stream.SampleRate}Hz mono, spatialBlend=1, range={(IsVehicle?15:200)}m");
        }
        if(source!=null)
        {
            speaker.transform.position=Anchor.position;
            float range=IsVehicle?15f:200f;
            source.volume=Vector3.Distance(listener,Anchor.position)>=range?0:volume;
            if(!measured && consumed>stream.SampleRate*2)
            {
                var output=new Il2CppStructArray<float>(1024);source.GetOutputData(output,0);
                double energy=0;for(int i=0;i<output.Length;i++)energy+=output[i]*output[i];
                double rms=Math.Sqrt(energy/output.Length);
                if(rms>0.000001){measured=true;Plugin.Logger.LogInfo($"Unity audio verified {Key}: isPlaying={source.isPlaying}, RMS={rms:F6}, decoded={stream.DecodedSamples}, consumed={consumed}");}
            }
        }
    }
    void Fill(Il2CppStructArray<float> data)
    {
        if(buffer.Length!=data.Length)buffer=new float[data.Length];
        var active=stream;
        if(active!=null){active.Read(buffer);System.Threading.Interlocked.Add(ref consumed,data.Length);}
        else Array.Clear(buffer,0,buffer.Length);
        for(int i=0;i<data.Length;i++)data[i]=buffer[i];
    }
    void StopAudio()
    {
        if(source!=null)source.Stop();
        stream?.Dispose();stream=null;
        if(speaker!=null)UnityEngine.Object.Destroy(speaker);
        if(clip!=null)UnityEngine.Object.Destroy(clip);
        source=null;speaker=null;clip=null;
    }
    public void Dispose(){StopAudio();}
}
