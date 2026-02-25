using Serilog;
using StatisticsAnalysisTool.Cluster;
using StatisticsAnalysisTool.DamageMeter;
using StatisticsAnalysisTool.Models.NetworkModel;
using StatisticsAnalysisTool.MobileServer;
using StatisticsAnalysisTool.MobileServer.Dtos;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace StatisticsAnalysisTool.Network.Manager;

/// <summary>
/// Subscribes to internal events/changes in the WPF app and broadcasts data to mobile clients.
/// Uses a periodic polling approach combined with event-driven updates for real-time data.
/// </summary>
public class MobileBroadcastService : IDisposable
{
    private readonly MobileServerManager _server;
    private readonly MobileDataProvider _dataProvider;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly TrackingController _trackingController;
    private Timer _periodicTimer;
    private bool _isDisposed;

    // Throttle: avoid spamming updates more often than this interval
    private static readonly TimeSpan DamageMeterThrottle = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan DashboardInterval = TimeSpan.FromSeconds(2);
    private DateTime _lastDamageMeterBroadcast = DateTime.MinValue;
    private int _periodicTickCount;

    public MobileBroadcastService(
        MobileServerManager server,
        MobileDataProvider dataProvider,
        MainWindowViewModel mainWindowViewModel,
        TrackingController trackingController)
    {
        _server = server;
        _dataProvider = dataProvider;
        _mainWindowViewModel = mainWindowViewModel;
        _trackingController = trackingController;
    }

    public void Start()
    {
        // Subscribe to CombatController damage updates for real-time DamageMeter
        _trackingController.CombatController.OnDamageUpdate += OnDamageUpdate;

        // Subscribe to cluster changes
        _trackingController.ClusterController.OnChangeCluster += OnClusterChanged;

        // Periodic timer for dashboard/stats that change continuously
        _periodicTimer = new Timer(PeriodicBroadcast, null, TimeSpan.FromSeconds(1), DashboardInterval);

        Log.Information("MobileBroadcastService started");
    }

    public void Stop()
    {
        _trackingController.CombatController.OnDamageUpdate -= OnDamageUpdate;
        _trackingController.ClusterController.OnChangeCluster -= OnClusterChanged;
        _periodicTimer?.Dispose();
        _periodicTimer = null;

        Log.Information("MobileBroadcastService stopped");
    }

    private void OnDamageUpdate(ObservableCollection<DamageMeterFragment> fragments, List<KeyValuePair<Guid, PlayerGameObject>> entities)
    {
        if ((DateTime.UtcNow - _lastDamageMeterBroadcast) < DamageMeterThrottle)
            return;

        _lastDamageMeterBroadcast = DateTime.UtcNow;

        Task.Run(async () =>
        {
            try
            {
                var data = _dataProvider.GetDamageMeter();
                await _server.BroadcastDamageMeterAsync(data);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to broadcast DamageMeter");
            }
        });
    }

    private void OnClusterChanged(ClusterInfo cluster)
    {
        // IMPORTANT: Snapshot all values immediately on the calling thread.
        // CurrentCluster is a mutable singleton — if we read it inside Task.Run,
        // a subsequent ChangeClusterInformation call may have already mutated it.
        ClusterChangedDto? snapshotDto = null;

        if (cluster != null)
        {
            snapshotDto = new ClusterChangedDto
            {
                Index = cluster.Index ?? string.Empty,
                MainClusterIndex = cluster.MainClusterIndex ?? string.Empty,
                UniqueName = cluster.UniqueName ?? string.Empty,
                UniqueClusterName = cluster.UniqueClusterName ?? string.Empty,
                MapType = cluster.MapType.ToString(),
                MapTypeString = cluster.MapTypeString ?? string.Empty,
                Tier = cluster.TierString ?? string.Empty,
                ClusterMode = cluster.ClusterMode.ToString(),
                WorldJsonType = cluster.WorldJsonType ?? string.Empty,
                AvalonTunnelType = cluster.AvalonTunnelType.ToString(),
                MistsRarity = cluster.MistsRarity.ToString(),
                InstanceName = cluster.InstanceName ?? string.Empty,
                MapDisplayName = cluster.UniqueClusterName ?? cluster.UniqueName ?? cluster.Index ?? string.Empty,
                ClusterHistoryString1 = cluster.ClusterHistoryString1 ?? string.Empty,
                ClusterHistoryString2 = cluster.ClusterHistoryString2 ?? string.Empty,
                ClusterHistoryString3 = cluster.ClusterHistoryString3 ?? string.Empty
            };

            Log.Information(
                "ClusterChanged broadcast: {UniqueName} (index={Index}, mainIndex={MainIndex}, type={MapType}, zone={ClusterMode}, tier={Tier})",
                snapshotDto.UniqueName, snapshotDto.Index, snapshotDto.MainClusterIndex, snapshotDto.MapType, snapshotDto.ClusterMode, snapshotDto.Tier);
        }

        Task.Run(async () =>
        {
            try
            {
                if (snapshotDto != null)
                {
                    await _server.BroadcastClusterChangedAsync(snapshotDto);
                }

                // Also broadcast dashboard and player info on cluster change
                var playerInfo = _dataProvider.GetPlayerInfo();

                await _server.BroadcastDashboardAsync(_dataProvider.GetDashboard());
                await _server.BroadcastPlayerInfoAsync(playerInfo);

                // Broadcast dungeons (new dungeon might have been created)
                await _server.BroadcastDungeonsAsync(_dataProvider.GetDungeons());

                // Broadcast map history (new cluster entry added)
                await _server.BroadcastMapHistoryAsync(_dataProvider.GetMapHistory());
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to broadcast ClusterChanged");
            }
        });
    }

    private void PeriodicBroadcast(object? state)
    {
        if (!_server.IsRunning) return;

        _periodicTickCount++;

        Task.Run(async () =>
        {
            try
            {
                // Dashboard (fame/h, silver/h, etc. — changes continuously)
                await _server.BroadcastDashboardAsync(_dataProvider.GetDashboard());

                // Server status
                await _server.BroadcastServerStatusAsync(_dataProvider.GetServerStatus());

                // Dungeons & Player info & Gathering every ~6 seconds (every 3rd tick at 2s interval)
                if (_periodicTickCount % 3 == 0)
                {
                    await _server.BroadcastDungeonsAsync(_dataProvider.GetDungeons());
                    await _server.BroadcastPlayerInfoAsync(_dataProvider.GetPlayerInfo());
                    await _server.BroadcastGatheringAsync(_dataProvider.GetGathering());
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed periodic broadcast");
            }
        });
    }

    /// <summary>
    /// Call this from controllers when trades are added/updated.
    /// </summary>
    public async Task BroadcastTradesAsync()
    {
        if (!_server.IsRunning) return;
        try
        {
            await _server.BroadcastTradesAsync(_dataProvider.GetTrades());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to broadcast Trades");
        }
    }

    /// <summary>
    /// Call this from controllers when gathering data changes.
    /// </summary>
    public async Task BroadcastGatheringAsync()
    {
        if (!_server.IsRunning) return;
        try
        {
            await _server.BroadcastGatheringAsync(_dataProvider.GetGathering());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to broadcast Gathering");
        }
    }

    /// <summary>
    /// Call this from controllers when party data changes.
    /// </summary>
    public async Task BroadcastPartyAsync()
    {
        if (!_server.IsRunning) return;
        try
        {
            await _server.BroadcastPartyAsync(_dataProvider.GetParty());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to broadcast Party");
        }
    }

    /// <summary>
    /// Call this from controllers when guild data changes.
    /// </summary>
    public async Task BroadcastGuildAsync()
    {
        if (!_server.IsRunning) return;
        try
        {
            await _server.BroadcastGuildAsync(_dataProvider.GetGuild());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to broadcast Guild");
        }
    }

    /// <summary>
    /// Call this when a new logging notification is added.
    /// </summary>
    public async Task BroadcastLoggingNotificationAsync(LoggingNotificationDto notification)
    {
        if (!_server.IsRunning) return;
        try
        {
            await _server.BroadcastLoggingNotificationAsync(notification);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to broadcast LoggingNotification");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}
