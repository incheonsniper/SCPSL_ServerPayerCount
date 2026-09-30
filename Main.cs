using System.Net;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using Microsoft.AspNetCore.Builder;

namespace ServerInfoApi;

public class Main : Plugin
{
    public override string Name => "Server Info";
    public override string Description => "None";
    public override string Author => "Raccoon";
    public override Version Version => new Version(1, 0, 0);
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;

    private WebApplication? _app;

    public override void Enable()
    {
        PlayerEvents.Joined += OnPlayerJoined;
        PlayerEvents.Left += OnPlayerLeft;

        App();
    }

    public override void Disable()
    {
        PlayerEvents.Joined -= OnPlayerJoined;
        PlayerEvents.Left -= OnPlayerLeft;

        _app?.StopAsync().GetAwaiter().GetResult();
        _app?.DisposeAsync().GetAwaiter().GetResult();
        _app = null;
    }

    private static int CurrentPlayerCount { get; set; }

    private static void OnPlayerJoined(PlayerJoinedEventArgs eventArgs)
    {
        CurrentPlayerCount++;
    }

    private static void OnPlayerLeft(PlayerLeftEventArgs eventArgs)
    {
        CurrentPlayerCount--;
    }

    private void App()
    {
        var builder = WebApplication.CreateBuilder();

        _app = builder.Build();

        _app.MapGet("/playercount", () => new
        {
            Player = CurrentPlayerCount,
            MaxPlayer = Server.MaxPlayers
        });

        _ = _app.RunAsync("http://0.0.0.0:8080");
    }
}