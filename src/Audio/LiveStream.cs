using System.Net.Http;
using NLayer;

namespace MoonSweepers.MoonRadio.Audio;

// No Unity calls on the decoder thread. Each emitter owns a bounded PCM buffer.
public sealed class LiveStream : IDisposable
{
    readonly CancellationTokenSource cancel = new();
    readonly object gate = new();
    readonly Uri endpoint;
    float[] ring = Array.Empty<float>();
    int read, write, count;
    public int SampleRate { get; private set; }
    public string Error { get; private set; }
    public bool Finished { get; private set; }
    public long DecodedSamples { get; private set; }
    public int Buffered { get { lock(gate) return count; } }
    public Task Worker { get; }
    public LiveStream(string url)
    {
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri) || uri.Scheme!="https" || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Enter an HTTPS MP3 audio stream URL.");
        endpoint=uri;
        Worker=Task.Run(Decode);
    }
    async Task Decode()
    {
        try
        {
            using var client=new HttpClient(){Timeout=Timeout.InfiniteTimeSpan};
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MoonSweepers-MoonRadio/0.1.1");
            using var headersTimeout=CancellationTokenSource.CreateLinkedTokenSource(cancel.Token);
            headersTimeout.CancelAfter(TimeSpan.FromSeconds(20));
            using var response=await client.GetAsync(endpoint,HttpCompletionOption.ResponseHeadersRead,headersTimeout.Token);
            response.EnsureSuccessStatusCode();
            if(response.Headers.TryGetValues("icy-metaint",out _))throw new InvalidDataException("Stream unexpectedly includes ICY metadata.");
            using var network=await response.Content.ReadAsStreamAsync(cancel.Token);
            using var registration=cancel.Token.Register(()=>network.Dispose());
            using var counted=new PositionStream(network);
            using var decoder=new MpegFile(counted);
            int channels=decoder.Channels;
            int rate=decoder.SampleRate;
            if(rate<8000 || rate>192000 || channels<1 || channels>2)throw new InvalidDataException("Unsupported MP3 audio format.");
            lock(gate){ring=new float[rate*8];SampleRate=rate;}
            var block=new float[4096*channels];
            while(!cancel.IsCancellationRequested)
            {
                int received=decoder.ReadSamples(block,0,block.Length);
                if(received==0)break;
                int frames=received/channels;
                // Backpressure bounds memory without dropping live audio during normal buffering.
                while(!cancel.IsCancellationRequested)
                {
                    bool added=false;
                    lock(gate)
                    {
                        if(ring.Length-count>=frames)
                        {
                            for(int i=0;i<frames;i++)
                            {
                                float sample=channels==1?block[i]:(block[i*2]+block[i*2+1])*0.5f;
                                ring[write]=float.IsFinite(sample)?Math.Clamp(sample,-1f,1f):0;
                                write=(write+1)%ring.Length;
                            }
                            count+=frames;DecodedSamples+=frames;added=true;
                        }
                    }
                    if(added)break;
                    await Task.Delay(20,cancel.Token);
                }
            }
        }
        catch(Exception ex){if(!cancel.IsCancellationRequested)Error=ex.ToString();}
        finally{Finished=true;}
    }
    // Mono output permits positional attenuation in Unity.
    public int Read(float[] destination)
    {
        lock(gate)
        {
            int n=Math.Min(destination.Length,count);
            for(int i=0;i<n;i++){destination[i]=ring[read];read=(read+1)%ring.Length;}
            count-=n;
            Array.Clear(destination,n,destination.Length-n);
            return n;
        }
    }
    public void Dispose(){cancel.Cancel();}

    // NLayer asks Position even for forward-only inputs; HttpClient streams omit it.
    sealed class PositionStream : Stream
    {
        readonly Stream inner;long position;
        public PositionStream(Stream inner){this.inner=inner;}
        public override bool CanRead=>true;
        public override bool CanSeek=>false;
        public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();
        public override long Position {get=>position;set=>throw new NotSupportedException();}
        public override int Read(byte[] buffer,int offset,int count){int n=inner.Read(buffer,offset,count);position+=n;return n;}
        public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
        public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
        public override void Flush(){}
    }
}
