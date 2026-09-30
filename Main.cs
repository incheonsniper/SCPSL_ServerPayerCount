using System;
using System.Net;
using System.Text;
using System.Threading;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;

namespace ServerInfoApi
{
    public class Main : Plugin
    {
        public override string Name => "Server Info";
        public override string Description => "Server information HTTP API";
        public override string Author => "Raccoon";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;

        private HttpListener? _listener;
        private Thread? _listenerThread;
        private volatile bool _running;

        public override void Enable()
        {
            StartApp();
        }

        public override void Disable()
        {
            StopApp();
        }

        private void StartApp()
        {
            if (_running)
                return;

            try
            {
                HttpListener listener = new HttpListener();

                listener.Prefixes.Add("http://+:8080/");
                listener.Start();

                _listener = listener;
                _running = true;

                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "ServerInfo HTTP Listener"
                };

                _listenerThread.Start();

                Console.WriteLine(
                    "[Server Info] HTTP server started on port 8080.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[Server Info] Failed to start HTTP server: " + ex);

                _running = false;

                try
                {
                    _listener?.Close();
                }
                catch
                {
                    // ignored
                }

                _listener = null;
                _listenerThread = null;
            }
        }

        private void StopApp()
        {
            _running = false;

            HttpListener? listener = _listener;
            Thread? listenerThread = _listenerThread;

            _listener = null;
            _listenerThread = null;

            try
            {
                listener?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[Server Info] Failed to stop listener: " + ex);
            }

            try
            {
                listener?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[Server Info] Failed to close listener: " + ex);
            }

            try
            {
                if (listenerThread is { IsAlive: true } && listenerThread != Thread.CurrentThread)
                {
                    listenerThread.Join(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[Server Info] Failed to stop listener thread: " + ex);
            }

            Console.WriteLine("[Server Info] HTTP server stopped.");
        }

        private void ListenLoop()
        {
            while (_running)
            {
                HttpListener? listener = _listener;

                if (listener == null)
                    break;

                try
                {
                    HttpListenerContext context = listener.GetContext();

                    HandleRequest(context);
                }
                catch (HttpListenerException ex)
                {
                    if (!_running)
                        break;

                    Console.WriteLine(
                        "[Server Info] HTTP listener error: " +
                        ex.Message);
                }
                catch (ObjectDisposedException)
                {
                    if (!_running)
                        break;
                }
                catch (Exception ex)
                {
                    if (!_running)
                        break;

                    Console.WriteLine(
                        "[Server Info] Listener error: " + ex);
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                string path =
                    context.Request.Url?.AbsolutePath ?? "/";

                if (context.Request.HttpMethod.Equals(
                        "GET",
                        StringComparison.OrdinalIgnoreCase) &&
                    path.Equals(
                        "/playercount",
                        StringComparison.OrdinalIgnoreCase))
                {
                    SendPlayerCount(context);
                    return;
                }

                SendJson(
                    context,
                    404,
                    "{\"error\":\"Not Found\"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[Server Info] Request handling error: " + ex);

                try
                {
                    SendJson(
                        context,
                        500,
                        "{\"error\":\"Internal Server Error\"}");
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static void SendPlayerCount(
            HttpListenerContext context)
        {
            int playerCount = Server.PlayerCount;
            int maxPlayers = Server.MaxPlayers;

            string json =
                "{"
                + "\"Player\":" + playerCount + ","
                + "\"MaxPlayer\":" + maxPlayers
                + "}";

            SendJson(context, 200, json);
        }

        private static void SendJson(
            HttpListenerContext context,
            int statusCode,
            string json)
        {
            byte[] responseBytes =
                Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = responseBytes.Length;

            context.Response.Headers.Add(
                "Access-Control-Allow-Origin",
                "*");

            try
            {
                context.Response.OutputStream.Write(
                    responseBytes,
                    0,
                    responseBytes.Length);
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }
    }
}