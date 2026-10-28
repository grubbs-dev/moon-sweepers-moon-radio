using MoonCleaners;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Runtime.InteropServices;

namespace MoonSweepers.MoonRadio;
internal static class RadioRuntime
{
    public static MoonWalker Walker;
    public static readonly Dictionary<string,RadioEmitter> Emitters=new();
    public static RadioEmitter Context;
    public static float Volume=0.55f;
    static MoonMenuKit hintKit;
    static GameObject hintRoot;
    static GameObject hintLabelObject;
    static GameObject hintKeyObject;
    static TMP_Text hintLabel;
    static TMP_Text hintKey;
    static RadioPanel panel;
    static float nextScan;
    static bool described;
    static bool tested;
    static string lastContextDiagnostic;
    public static void Tick()
    {
        if(Walker==null)
        {
            Walker=UnityEngine.Object.FindObjectsOfType<MoonWalker>().FirstOrDefault();
            if(Walker==null)return;
        }
        RadioTestVehicles.Tick(Walker);
        RadioTestSave.Tick(Walker);
        if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+1;Scan();RadioSync.Tick();}
        Context=FindContext();
        var contextDiagnostic=Context?.Key??"<none>";
        if(contextDiagnostic!=lastContextDiagnostic)
        {
            lastContextDiagnostic=contextDiagnostic;
            Plugin.Logger.LogInfo($"Radio context changed: {contextDiagnostic}; player={Walker.transform.position}");
        }
        if(hintRoot==null)
        {
            // The HUD action list is created after the player scene loads. Do
            // not permanently fall back during that short window: doing so
            // produces the mismatched radio row users see before the native
            // controls become discoverable.
            if(!TryCreateNativeHint())return;
        }
        RehomeHintToLiveCanvas();
        if(Context!=null && panel?.IsOpen!=true)
        {
            SetHintActive(true);
        }
        else SetHintActive(false);
        var keyboard=Keyboard.current;
        bool radioPressed=(GetAsyncKeyState(0x52)&1)!=0 || keyboard!=null&&keyboard.rKey.wasPressedThisFrame;
        if(Application.isFocused && keyboard!=null)
        {
            if(panel?.IsOpen==true)
            {
                if(keyboard.escapeKey.wasPressedThisFrame)panel.Close();
            }
            else if(Context!=null && !MoonInput.Typing && radioPressed)
            {
                panel??=new RadioPanel();panel.Open(Context);
            }
        }
        panel?.Tick();
        Vector3 ear=Walker.viewCamera!=null?Walker.viewCamera.transform.position:Walker.transform.position;
        foreach(var radio in Emitters.Values)radio.Tick(ear,Volume);
        if(Environment.GetCommandLineArgs().Contains("--moon-radio-self-test"))
        {
            if(!tested){tested=true;RadioSelfTest.Start(ear);}
            RadioSelfTest.Tick(ear);
        }
    }
    static void RehomeHintToLiveCanvas()
    {
        if(hintRoot==null)return;
        TMP_Text field=null;
        foreach(var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
            if(string.Equals(text.text,"FIELD MANUAL",StringComparison.OrdinalIgnoreCase)){field=text;break;}
        var canvas=field?.GetComponentInParent<Canvas>();
        if(canvas==null)
            canvas=UnityEngine.Object.FindObjectsOfType<Canvas>()
                .Where(c=>c.isActiveAndEnabled)
                .OrderByDescending(c=>c.sortingOrder)
                .FirstOrDefault();
        if(canvas!=null && hintRoot.transform.parent!=canvas.transform)
            hintRoot.transform.SetParent(canvas.transform,true);
    }
    static bool TryCreateNativeHint()
    {
        TMP_Text field=null,key=null;
        foreach(var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
        {
            if(field==null && string.Equals(text.text,"FIELD MANUAL",StringComparison.OrdinalIgnoreCase))field=text;
            else if(key==null && string.Equals(text.text,"H",StringComparison.OrdinalIgnoreCase))key=text;
        }
        if(field==null||key==null||key.transform.parent==null)return false;
        var sourceRow=key.transform.parent.gameObject;
        hintRoot=UnityEngine.Object.Instantiate(sourceRow,sourceRow.transform.parent);
        hintRoot.name="MoonRadioNativeHint";
        hintRoot.transform.SetSiblingIndex(sourceRow.transform.GetSiblingIndex());
        var sourceRect=sourceRow.GetComponent<RectTransform>();
        var hintRect=hintRoot.GetComponent<RectTransform>();
        if(sourceRect!=null&&hintRect!=null)
            hintRect.anchoredPosition=sourceRect.anchoredPosition+new Vector2(0f,sourceRect.rect.height+8f);
        var nativeCanvas=sourceRow.GetComponentInParent<Canvas>();
        if(nativeCanvas!=null)
            hintRoot.transform.SetParent(nativeCanvas.transform,true);
        hintRoot.SetActive(true);
        hintLabel=null;hintKey=null;
        foreach(var text in hintRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if(string.Equals(text.text,"FIELD MANUAL",StringComparison.OrdinalIgnoreCase))
            {
                text.text="RADIO";hintLabel=text;text.raycastTarget=false;
            }
            else if(string.Equals(text.text,"H",StringComparison.OrdinalIgnoreCase))
            {
                text.text="R";hintKey=text;text.raycastTarget=false;
            }
        }
        if(hintLabel==null||hintKey==null)
        {
            UnityEngine.Object.Destroy(hintRoot);hintRoot=null;return false;
        }
        hintLabelObject=hintRoot;hintKeyObject=hintRoot;
        Plugin.Logger.LogInfo("Radio hint cloned the complete native shortcut row");
        return true;
    }
    static void CreateFallbackHint()
    {
        hintKit=new MoonMenuKit("MoonRadioHint");
        hintRoot=MoonMenuKit.Canvas(null,"MoonRadioHintCanvas",92);
        hintLabel=hintKit.Label(hintRoot.transform,"RADIO",1625,372,155,34,16,Color.white,false);hintLabel.alignment=TextAlignmentOptions.MidlineRight;hintLabel.raycastTarget=false;
        var keyRect=hintKit.Rect(hintRoot.transform,"MoonRadioKey",1792,369,38,38);var keyBg=keyRect.gameObject.AddComponent<Image>();keyBg.color=new Color(0.035f,0.025f,0.025f,0.98f);
        hintKey=hintKit.Label(keyRect,"R",0,0,38,38,18,Color.white,true);hintKey.alignment=TextAlignmentOptions.Center;hintKey.raycastTarget=false;CopyNativeHudStyle(keyBg);
        hintLabelObject=hintLabel.gameObject;hintKeyObject=keyRect.gameObject;
        Plugin.Logger.LogWarning("Native HUD controls not found; used fallback radio hint");
    }
    static void SetHintActive(bool active)
    {
        if(hintLabelObject!=null)hintLabelObject.SetActive(active);
        if(hintKeyObject!=null)hintKeyObject.SetActive(active);
        if(hintRoot!=null && hintRoot.transform.childCount>0)hintRoot.SetActive(active);
    }
    static void Add(string key,string name,Transform anchor)
    {
        if(string.IsNullOrEmpty(key)||key.EndsWith(":"))return;
        if(Emitters.TryGetValue(key,out var old)){old.Anchor=anchor;return;}
        if(Emitters.Count>=128)return;
        Emitters.Add(key,new RadioEmitter(key,name,anchor));
        Plugin.Logger.LogInfo($"Found radio emitter {key}: {name}");
    }
    public static bool Eligible(string name)
    {
        string compact=new string((name??"").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return !compact.StartsWith("push");
    }
    static void Scan()
    {
        foreach(var s in UnityEngine.Object.FindObjectsOfType<MoonCollector>())
        {
            var installed=s.GetComponentInChildren<MoonRadioStation>();
            string stationKey="s:"+s.site.key;
            Add(stationKey,s.site.name,installed!=null&&installed.Speaker!=null?installed.Speaker:s.transform);
        }
        foreach(var v in UnityEngine.Object.FindObjectsOfType<MoonPrototypeVehicle>())
            if(Eligible(v.Spec?.name))Add("v:"+v.StorageId,v.Spec?.name??"Vehicle",v.transform);
        foreach(var v in UnityEngine.Object.FindObjectsOfType<MoonRemoteVehicle>())
            if(Eligible(v.Spec?.name))Add("v:"+v.Storage,v.Spec?.name??"Vehicle",v.transform);
        foreach(var v in UnityEngine.Object.FindObjectsOfType<MoonRobotVacuum>())
            Add("r:"+v.storageId,"Robot vacuum",v.transform);
        if(!described)
        {
            described=true;
            foreach(var item in MoonPrototypeCatalog.Vehicles)Plugin.Logger.LogInfo("Vehicle catalog: "+item.name+" eligible="+Eligible(item.name));
            Plugin.Logger.LogInfo("Interior volumes: "+UnityEngine.Object.FindObjectsOfType<MoonCollectorInteriorGrade>().Length);
        }
    }
    static void CopyNativeHudStyle(Image keyBg)
    {
        // Reuse the actual HUD font/material and the existing H key-cap sprite
        // when the native action list is present. This keeps the mod in lockstep
        // with the game's typography and rounded key treatment.
        TMP_Text label=null,key=null;
        foreach(var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
        {
            if(label==null && string.Equals(text.text,"FIELD MANUAL",StringComparison.OrdinalIgnoreCase))label=text;
            if(key==null && string.Equals(text.text,"H",StringComparison.OrdinalIgnoreCase))key=text;
        }
        if(label!=null)
        {
            hintLabel.font=label.font;hintLabel.fontSize=label.fontSize;hintLabel.fontStyle=label.fontStyle;
            hintLabel.color=label.color;hintLabel.fontSharedMaterial=label.fontSharedMaterial;
        }
        if(key!=null)
        {
            var hierarchy=new List<string>();
            for(var t=key.transform;t!=null&&hierarchy.Count<8;t=t.parent)
            {
                var rt=t.GetComponent<RectTransform>();
                hierarchy.Add($"{t.name}[{(rt!=null?rt.rect.width.ToString("0.##"):"?")},{(rt!=null?rt.rect.height.ToString("0.##"):"?")}]");
            }
            Plugin.Logger.LogInfo("Native H hierarchy: "+string.Join(" <- ",hierarchy));
            hintKey.font=key.font;hintKey.fontSize=key.fontSize;hintKey.fontStyle=key.fontStyle;
            hintKey.color=key.color;hintKey.fontSharedMaterial=key.fontSharedMaterial;
            for(var t=key.transform; t!=null && t!=hintRoot.transform; t=t.parent)
            {
                var source=t.GetComponent<Image>();
                if(source!=null && source.sprite!=null)
                {
                    keyBg.sprite=source.sprite;keyBg.type=source.type;keyBg.pixelsPerUnitMultiplier=source.pixelsPerUnitMultiplier;
                    keyBg.color=source.color;break;
                }
            }
        }
    }
    static RadioEmitter FindContext()
    {
        string key=null;
        if(Walker.Mounted && Walker.mountedVehicle!=null)
        {
            if(!Eligible(Walker.mountedVehicle.Spec?.name))return null;
            key="v:"+Walker.mountedVehicle.StorageId;
        }
        else if(Walker.Riding!=null)key="r:"+Walker.Riding.storageId;
        else if(Walker.Passenger)
        {
            var seat=Walker.GetComponent<MoonVehiclePassenger>();
            if(seat?.Vehicle!=null)
            {
                var remote=seat.Vehicle.GetComponent<MoonRemoteVehicle>();
                var local=seat.Vehicle.GetComponent<MoonPrototypeVehicle>();
                if(remote!=null && Eligible(remote.Spec?.name))key="v:"+remote.Storage;
                else if(local!=null && Eligible(local.Spec?.name))key="v:"+local.StorageId;
            }
        }
        else
        {
            var pos=Walker.transform.position;
            bool inside=MoonCollectorInteriorGrade.Inside(pos)>0.5f;
            if(inside)
                key=Emitters.Values.Where(e=>e.Key.StartsWith("s:")&&e.Anchor!=null)
                    .OrderBy(e=>Vector3.SqrMagnitude(e.Anchor.position-pos)).FirstOrDefault()?.Key;
        }
        return key!=null&&Emitters.TryGetValue(key,out var radio)?radio:null;
    }
    public static void Select(RadioEmitter radio,string url,string title)
    {
        if(!string.IsNullOrEmpty(url) && (!Uri.TryCreate(url,UriKind.Absolute,out var u)||u.Scheme!="https"||!string.IsNullOrEmpty(u.UserInfo)))return;
        RadioSync.Select(radio,url,title);
    }
    public static void Reset()
    {
        panel?.Dispose();panel=null;
        if(hintLabelObject!=null)UnityEngine.Object.Destroy(hintLabelObject);
        if(hintKeyObject!=null)UnityEngine.Object.Destroy(hintKeyObject);
        if(hintRoot!=null)UnityEngine.Object.Destroy(hintRoot);
        hintRoot=null;hintLabelObject=null;hintKeyObject=null;hintLabel=null;hintKey=null;hintKit?.Dispose();hintKit=null;
        foreach(var radio in Emitters.Values)radio.Dispose();
        Emitters.Clear();Context=null;Walker=null;nextScan=0;described=false;lastContextDiagnostic=null;RadioSync.Reset();RadioSelfTest.Stop();
    }
    [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
}
