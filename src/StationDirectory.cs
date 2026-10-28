using System.Net.Http;
using System.Text.Json;

namespace MoonSweepers.MoonRadio;
internal sealed class StationChoice
{
    public string name {get;set;}="";
    public string url_resolved {get;set;}="";
    public string codec {get;set;}="";
    public string stationuuid {get;set;}="";
    public int hls {get;set;}
}
internal static class StationDirectory
{
    public static async Task<StationChoice[]> Search(string query,bool genre)
    {
        using var http=new HttpClient(){Timeout=TimeSpan.FromSeconds(15)};
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MoonSweepers-MoonRadio/0.1.0");
        string filter=string.IsNullOrWhiteSpace(query)?"":$"&{(genre?"tag":"name")}={Uri.EscapeDataString(query.Trim())}";
        Exception failure=null;
        foreach(var host in new[]{"de1.api.radio-browser.info","nl1.api.radio-browser.info"})
        {
            try
            {
                string json=await http.GetStringAsync($"https://{host}/json/stations/search?codec=MP3&hidebroken=true&order=clickcount&reverse=true&limit=32{filter}");
                return (JsonSerializer.Deserialize<StationChoice[]>(json)??Array.Empty<StationChoice>())
                    .Where(s=>s.codec.Equals("MP3",StringComparison.OrdinalIgnoreCase)&&s.hls==0&&Uri.TryCreate(s.url_resolved,UriKind.Absolute,out var u)&&u.Scheme=="https")
                    .GroupBy(s=>s.url_resolved).Select(g=>g.First()).Take(8).ToArray();
            }
            catch(Exception ex){failure=ex;}
        }
        throw new HttpRequestException("Radio directory unavailable. Try again or enter a direct HTTPS MP3 URL.",failure);
    }
}
