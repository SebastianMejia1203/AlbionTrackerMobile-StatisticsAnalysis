using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StatisticsAnalysisTool.MobileServer.Dtos;
using StatisticsAnalysisTool.MobileServer.Hubs;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StatisticsAnalysisTool.MobileServer;

/// <summary>
/// Manages the embedded ASP.NET Core / SignalR server lifecycle.
/// The WPF app creates an instance, provides an IDataProvider implementation,
/// and calls Start/Stop to control the server.
/// </summary>
public class MobileServerManager : IDisposable
{
    private WebApplication? _app;
    private IHubContext<AlbionMobileHub>? _hubContext;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private UdpDiscoveryService? _discoveryService;
    private readonly IDataProvider _dataProvider;

    public int Port { get; private set; }
    public bool IsRunning { get; private set; }
    public string? LocalIpAddress { get; private set; }

    /// <summary>
    /// Event raised when the server starts/stops or a client connects/disconnects.
    /// </summary>
    public event Action<bool>? ServerStateChanged;

    public MobileServerManager(IDataProvider dataProvider, int port = 7777)
    {
        _dataProvider = dataProvider;
        Port = port;
    }

    /// <summary>
    /// Start the SignalR server on the specified port.
    /// </summary>
    public async Task StartAsync()
    {
        if (IsRunning) return;

        try
        {
            _cts = new CancellationTokenSource();

            var builder = WebApplication.CreateBuilder();

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, Port);
            });

            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                options.PayloadSerializerOptions.Converters.Add(new SafeDoubleConverter());
            });

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Register the data provider as a singleton
            builder.Services.AddSingleton(_dataProvider);

            _app = builder.Build();

            _app.UseCors();
            _app.MapHub<AlbionMobileHub>("/mobilehub");

            // Health check endpoint
            _app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

            // Connection info endpoint
            _app.MapGet("/info", () => Results.Ok(_dataProvider.GetServerStatus()));

            _hubContext = _app.Services.GetRequiredService<IHubContext<AlbionMobileHub>>();

            LocalIpAddress = GetLocalIpAddress();

            _serverTask = _app.RunAsync(_cts.Token);
            IsRunning = true;

            _discoveryService = new UdpDiscoveryService(Port);
            _discoveryService.Start();

            Log.Information("MobileServer started on port {Port}. Local IP: {Ip}", Port, LocalIpAddress);
            ServerStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start MobileServer on port {Port}", Port);
            IsRunning = false;
            throw;
        }
    }

    /// <summary>
    /// Stop the SignalR server.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning || _app == null) return;

        try
        {
            _cts?.Cancel();

            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error stopping MobileServer");
        }
        finally
        {
            _discoveryService?.Stop();
            _discoveryService = null;
            _app = null;
            _hubContext = null;
            _cts?.Dispose();
            _cts = null;
            IsRunning = false;

            Log.Information("MobileServer stopped");
            ServerStateChanged?.Invoke(false);
        }
    }

    // =====================================================================
    // Broadcast methods — called by the WPF app when data changes
    // =====================================================================

    public async Task BroadcastDashboardAsync(DashboardDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("DashboardUpdate", data);
    }

    public async Task BroadcastDamageMeterAsync(DamageMeterDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("DamageMeterUpdate", data);
    }

    public async Task BroadcastDungeonsAsync(DungeonListDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("DungeonsUpdate", data);
    }

    public async Task BroadcastTradesAsync(TradeListDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("TradesUpdate", data);
    }

    public async Task BroadcastGatheringAsync(GatheringListDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("GatheringUpdate", data);
    }

    public async Task BroadcastPartyAsync(PartyDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("PartyUpdate", data);
    }

    public async Task BroadcastGuildAsync(GuildDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("GuildUpdate", data);
    }

    public async Task BroadcastPlayerInfoAsync(PlayerInfoDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("PlayerInfoUpdate", data);
    }

    public async Task BroadcastClusterChangedAsync(ClusterChangedDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("ClusterChanged", data);
    }

    public async Task BroadcastMapHistoryAsync(MapHistoryDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("MapHistoryUpdate", data);
    }

    public async Task BroadcastLoggingNotificationAsync(LoggingNotificationDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("NewLoggingNotification", data);
    }

    public async Task BroadcastServerStatusAsync(ServerStatusDto data)
    {
        if (_hubContext == null) return;
        await _hubContext.Clients.All.SendAsync("ServerStatus", data);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static string GetLocalIpAddress()
    {
        try
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;

                var properties = networkInterface.GetIPProperties();
                foreach (var address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(address.Address))
                    {
                        return address.Address.ToString();
                    }
                }
            }
        }
        catch
        {
            // ignored
        }

        return "127.0.0.1";
    }

    public string GetConnectionUrl() => $"http://{LocalIpAddress}:{Port}/mobilehub";

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
