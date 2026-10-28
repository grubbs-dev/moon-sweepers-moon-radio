using MoonCleaners;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MoonSweepers.MoonRadio;
internal sealed class RadioPanel : IDisposable
{
    MoonMenuKit kit;
    GameObject root,ownedEvents;
    TMP_InputField input;
    TMP_Text header,status,now,volume;
    readonly List<Button> results=new();
    Task<StationChoice[]> search;
    RadioEmitter radio;
    bool priorVisible,priorBlocked;
    CursorLockMode priorLock;
    string message="Choose a genre, search a station, or paste an HTTPS MP3 stream.";
    public bool IsOpen=>root!=null&&root.activeSelf;
    public void Open(RadioEmitter target)
    {
        radio=target;
        if(root==null)Build();
        priorVisible=Cursor.visible;priorLock=Cursor.lockState;priorBlocked=RadioRuntime.Walker.uiBlocked;
        RadioRuntime.Walker.uiBlocked=true;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;root.SetActive(true);
        header.text="MOON RADIO  /  "+target.Name;
        Search("",false);
        Plugin.Logger.LogInfo("Radio panel opened: "+target.Key);
    }
    public void Close()
    {
        if(!IsOpen)return;
        root.SetActive(false);
        if(RadioRuntime.Walker!=null)RadioRuntime.Walker.uiBlocked=priorBlocked;
        Cursor.lockState=priorLock;Cursor.visible=priorVisible;
    }
    void Build()
    {
        kit=new MoonMenuKit("MoonRadio");
        root=MoonMenuKit.Canvas(null,"MoonRadioCanvas",160);
        var panel=kit.Panel(root.transform,"RadioPanel",0,0,880,620,new Color(0.035f,0.06f,0.085f,0.98f),12);
        var window=panel.GetComponent<RectTransform>();window.anchorMin=new Vector2(0.5f,0.5f);window.anchorMax=new Vector2(0.5f,0.5f);window.pivot=new Vector2(0.5f,0.5f);window.anchoredPosition=Vector2.zero;
        var parent=panel.transform;
        header=Label(parent,"MOON RADIO",24,24,720,34,25);
        kit.Button(parent,"CLOSE",748,22,106,34,(Action)Close);
        now=Label(parent,"Radio off",24,66,820,34,17);
        var fieldRect=kit.Rect(parent,"StationSearch",24,116,640,42);
        var bg=fieldRect.gameObject.AddComponent<Image>();bg.color=new Color(0.12f,0.17f,0.21f,1);
        var text=kit.Label(fieldRect,"",12,0,612,42,17,Color.white,false);text.richText=false;
        var placeholder=kit.Label(fieldRect,"Search station name or paste HTTPS MP3 URL",12,0,612,42,16,new Color(0.6f,0.7f,0.75f),false);
        input=fieldRect.gameObject.AddComponent<TMP_InputField>();input.textViewport=fieldRect;input.textComponent=text.TryCast<TextMeshProUGUI>();input.placeholder=placeholder;input.targetGraphic=bg;input.characterLimit=1024;
        kit.Button(parent,"SEARCH / PLAY",680,116,174,42,(Action)Submit,true);
        string[] genres={"chillout","rock","pop","jazz","classical","electronic","hip hop"};
        for(int i=0;i<genres.Length;i++)
        {
            string genre=genres[i];kit.Button(parent,genre.ToUpperInvariant(),24+i*119,174,111,32,(Action)(()=>Search(genre,true)));
        }
        for(int i=0;i<8;i++)
        {
            int row=i;
            var button=kit.Button(parent,"",24,218+i*35,830,31,(Action)(()=>PlayResult(row)));
            results.Add(button);button.gameObject.SetActive(false);
        }
        kit.Button(parent,"STOP THIS RADIO",24,514,200,36,(Action)(()=>RadioRuntime.Select(radio,"","")));
        kit.Button(parent,"VOL −",460,514,90,36,(Action)(()=>RadioRuntime.Volume=Math.Max(0,RadioRuntime.Volume-0.1f)));
        volume=Label(parent,"55%",560,514,70,36,18);
        kit.Button(parent,"VOL +",646,514,90,36,(Action)(()=>RadioRuntime.Volume=Math.Min(1,RadioRuntime.Volume+0.1f)));
        status=Label(parent,message,24,564,830,40,14);
        if(EventSystem.current==null)
        {
            ownedEvents=new GameObject("MoonRadioEventSystem");ownedEvents.AddComponent<EventSystem>();
            ownedEvents.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        root.SetActive(false);
    }
    TMP_Text Label(Transform p,string value,float x,float y,float width,float height,float size)
    {var text=kit.Label(p,value,x,y,width,height,size,Color.white,false);text.richText=false;return text;}
    StationChoice[] choices=Array.Empty<StationChoice>();
    void Search(string query,bool genre)
    {
        if(search!=null&&!search.IsCompleted)return;
        foreach(var row in results)row.gameObject.SetActive(false);
        message="Finding stations…";search=StationDirectory.Search(query,genre);
    }
    void Submit()
    {
        string query=input.text.Trim();
        if(query.StartsWith("https://",StringComparison.OrdinalIgnoreCase))
        {RadioRuntime.Select(radio,query,"Custom stream");message="Connecting to custom MP3 stream…";}
        else Search(query,false);
    }
    void PlayResult(int row)
    {
        if(row>=choices.Length)return;
        var choice=choices[row];
        RadioRuntime.Select(radio,choice.url_resolved,choice.name.Length>120?choice.name[..120]:choice.name);
        message="Selected station. Nearby players need Moon Radio installed to hear it.";
    }
    public void Tick()
    {
        if(!IsOpen)return;
        RadioRuntime.Walker.uiBlocked=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(search?.IsCompleted==true)
        {
            if(search.IsCompletedSuccessfully)
            {
                choices=search.Result;
                for(int i=0;i<results.Count;i++)
                {
                    results[i].gameObject.SetActive(i<choices.Length);
                    if(i<choices.Length){var label=results[i].GetComponentInChildren<TMP_Text>();label.richText=false;label.text=choices[i].name;}
                }
                message=choices.Length==0?"No compatible HTTPS MP3 stations found. Try another search.":"Select a station • Live broadcasts may differ slightly in timing between listeners.";
            }
            else{message="Directory unavailable. Try another search or a direct stream.";Plugin.Logger.LogWarning(search.Exception);}
            search=null;
        }
        now.text=radio.Status+(radio.Station.Length>0?"  |  "+radio.Station:"");
        status.text=message;volume.text=$"{RadioRuntime.Volume*100:0}%";
    }
    public void Dispose(){Close();if(root!=null)UnityEngine.Object.Destroy(root);if(ownedEvents!=null)UnityEngine.Object.Destroy(ownedEvents);kit?.Dispose();}
}
