using System.Net;
using System.Text;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;

namespace ServerPlayerCountApi
{
    public class Main : Plugin
    {
        public override string Name => "ServerPlayerCountApi";
        public override string Description => "Player count HTTP API";
        public override string Author => "Raccoon";

        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;

        private HttpListener _listener;
        private CancellationTokenSource _cts;

        private int PlayerCount { get; set; }

        public override void Enable()
        {
            PlayerEvents.Joined += OnJoined;
            PlayerEvents.Left += OnLeft;

            _cts = new CancellationTokenSource();

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:8080/");
            _listener.Start();

            Task.Run(() => RunApiAsync(_cts.Token));
        }

        public override void Disable()
        {
            PlayerEvents.Joined -= OnJoined;
            PlayerEvents.Left -= OnLeft;

            _cts?.Cancel();

            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
                _listener = null;
            }

            _cts?.Dispose();
            _cts = null;
        }

        private void OnJoined(PlayerJoinedEventArgs ev)
        {
            PlayerCount++;
        }

        private void OnLeft(PlayerLeftEventArgs ev)
        {
            if (PlayerCount > 0)
                PlayerCount--;
        }

        private async Task RunApiAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context =
                        await _listener.GetContextAsync();

                    if (context.Request.Url != null &&
                        context.Request.Url.AbsolutePath == "/playercount")
                    {
                        string json =
                            "{\"playerCount\":" + PlayerCount + "}";

                        byte[] data = Encoding.UTF8.GetBytes(json);

                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        context.Response.ContentLength64 = data.Length;

                        await context.Response.OutputStream.WriteAsync(
                            data,
                            0,
                            data.Length);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                    }

                    context.Response.Close();
                }
                catch (HttpListenerException)
                {
                    if (token.IsCancellationRequested)
                        break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    if (token.IsCancellationRequested)
                        break;
                }
            }
        }
    }
}