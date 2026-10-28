using MoonCleaners;
using Steamworks;
using System.Text.Json;
using UnityEngine;

namespace MoonSweepers.MoonRadio;
// Lobby metadata isolates mod state from the game's packet protocol and supports late joins.
internal static class RadioSync
{
    const string Prefix="msradio1:",RequestKey="msradio1.request";
    static readonly Dictionary<ulong,string> seenRequests=new();
    static readonly Dictionary<string,string> versions=new();
    static readonly HashSet<string> acknowledged=new();
    static ulong lobby;
    public sealed class State
    {
        public string Key {get;set;}="";
        public string Url {get;set;}="";
        public string Name {get;set;}="";
        public string Version {get;set;}="";
    }
    public static void Select(RadioEmitter radio,string url,string title)
    {
        Plugin.Logger.LogInfo($"Radio selection transport: inLobby={MoonSteamLobby.InLobby}, host={MoonSteamLobby.IsHost}");
        var state=new State{Key=radio.Key,Url=url,Name=title,Version=Guid.NewGuid().ToString("N")};
        if(!MoonSteamLobby.InLobby){radio.Select(url,title);return;}
        if(MoonSteamLobby.IsHost)Publish(state);
        else
        {
            SteamMatchmaking.SetLobbyMemberData(new CSteamID(MoonSteamLobby.LobbyId),RequestKey,JsonSerializer.Serialize(state));
            Plugin.Logger.LogInfo("Requested shared radio change: "+radio.Key);
        }
    }
    static void Publish(State state)
    {
        if(!Valid(state)||!RadioRuntime.Emitters.ContainsKey(state.Key))return;
        if(!SteamMatchmaking.SetLobbyData(new CSteamID(MoonSteamLobby.LobbyId),Prefix+state.Key,JsonSerializer.Serialize(state)))
        {Plugin.Logger.LogWarning("Steam could not publish radio selection.");return;}
        Apply(state);
    }
    static bool Valid(State s)=>s!=null && !string.IsNullOrEmpty(s.Key) && s.Key.Length<=160 && s.Name!=null && s.Name.Length<=120 && s.Version!=null && s.Version.Length==32 && s.Url!=null && s.Url.Length<=1024 &&
        (s.Url.Length==0||(Uri.TryCreate(s.Url,UriKind.Absolute,out var u)&&u.Scheme=="https"&&string.IsNullOrEmpty(u.UserInfo)));
    static void Apply(State state)
    {
        if(!Valid(state)||!RadioRuntime.Emitters.TryGetValue(state.Key,out var emitter))return;
        if(versions.TryGetValue(state.Key,out var version)&&version==state.Version)return;
        versions[state.Key]=state.Version;emitter.Select(state.Url,state.Name);
    }
    public static void Tick()
    {
        if(!MoonSteamLobby.InLobby){if(lobby!=0)Reset();return;}
        if(lobby!=MoonSteamLobby.LobbyId){Reset();lobby=MoonSteamLobby.LobbyId;}
        var id=new CSteamID(lobby);
        if(MoonSteamLobby.IsHost)
        {
            for(int m=0;m<MoonSteamLobby.members.Count;m++)
            {
                var member=MoonSteamLobby.members.get_Item(m);
                if(member.local)continue;
                string value=SteamMatchmaking.GetLobbyMemberData(id,new CSteamID(member.id),RequestKey);
                if(value.Length==0||value.Length>2048 || (seenRequests.TryGetValue(member.id,out var old)&&old==value))continue;
                try
                {
                    var request=JsonSerializer.Deserialize<State>(value);
                    if(!Valid(request)||!RadioRuntime.Emitters.TryGetValue(request.Key,out var emitter)||emitter.Anchor==null)continue;
                    var crew=MoonCrewSession.Instance;
                    if(crew==null||!crew.remotes.TryGetValue(member.id,out var player)||Vector3.Distance(player.transform.position,emitter.Anchor.position)>45)continue;
                    seenRequests[member.id]=value;
                    Publish(request);
                }
                catch(JsonException){}
            }
        }
        int n=Math.Min(SteamMatchmaking.GetLobbyDataCount(id),512);
        for(int i=0;i<n;i++)
        {
            string key="",value="";
            if(!SteamMatchmaking.GetLobbyDataByIndex(id,i,out key,256,out value,4096)||!key.StartsWith(Prefix)||value.Length>2048)continue;
            try
            {
                var state=JsonSerializer.Deserialize<State>(value);Apply(state);
                if(Valid(state)&&MoonSteamLobby.IsHost&&acknowledged.Add(state.Version))
                    Plugin.Logger.LogInfo("Steam radio metadata roundtrip verified: "+state.Key);
            }
            catch(JsonException){}
        }
    }
    public static void Reset(){lobby=0;seenRequests.Clear();versions.Clear();acknowledged.Clear();}
}
