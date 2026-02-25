using Microsoft.AspNetCore.SignalR;
using StatisticsAnalysisTool.MobileServer.Dtos;

namespace StatisticsAnalysisTool.MobileServer.Hubs;

/// <summary>
/// SignalR Hub for mobile clients. 
/// Clients can call these methods to request data snapshots.
/// The server also pushes real-time updates to clients via the broadcast service.
/// </summary>
public class AlbionMobileHub : Hub
{
    private readonly IDataProvider _dataProvider;

    public AlbionMobileHub(IDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        await Clients.Caller.SendAsync("ServerStatus", _dataProvider.GetServerStatus());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    // ===== Client-callable methods (Request/Response) =====

    /// <summary>Request current dashboard data snapshot</summary>
    public async Task RequestDashboard()
    {
        var data = _dataProvider.GetDashboard();
        await Clients.Caller.SendAsync("DashboardUpdate", data);
    }

    /// <summary>Request current damage meter data</summary>
    public async Task RequestDamageMeter()
    {
        var data = _dataProvider.GetDamageMeter();
        await Clients.Caller.SendAsync("DamageMeterUpdate", data);
    }

    /// <summary>Request dungeon list with stats</summary>
    public async Task RequestDungeons()
    {
        var data = _dataProvider.GetDungeons();
        await Clients.Caller.SendAsync("DungeonsUpdate", data);
    }

    /// <summary>Request trade list with stats</summary>
    public async Task RequestTrades()
    {
        var data = _dataProvider.GetTrades();
        await Clients.Caller.SendAsync("TradesUpdate", data);
    }

    /// <summary>Request gathering data</summary>
    public async Task RequestGathering()
    {
        var data = _dataProvider.GetGathering();
        await Clients.Caller.SendAsync("GatheringUpdate", data);
    }

    /// <summary>Request party data</summary>
    public async Task RequestParty()
    {
        var data = _dataProvider.GetParty();
        await Clients.Caller.SendAsync("PartyUpdate", data);
    }

    /// <summary>Request guild data</summary>
    public async Task RequestGuild()
    {
        var data = _dataProvider.GetGuild();
        await Clients.Caller.SendAsync("GuildUpdate", data);
    }

    /// <summary>Request player info</summary>
    public async Task RequestPlayerInfo()
    {
        var data = _dataProvider.GetPlayerInfo();
        await Clients.Caller.SendAsync("PlayerInfoUpdate", data);
    }

    /// <summary>Request recent logging notifications</summary>
    public async Task RequestLogging(int count = 100)
    {
        var data = _dataProvider.GetLoggingNotifications(count);
        await Clients.Caller.SendAsync("LoggingUpdate", data);
    }

    /// <summary>Request server status</summary>
    public async Task RequestServerStatus()
    {
        var status = _dataProvider.GetServerStatus();
        await Clients.Caller.SendAsync("ServerStatus", status);
    }

    /// <summary>Request damage meter snapshots list</summary>
    public async Task RequestDamageMeterSnapshots()
    {
        var data = _dataProvider.GetDamageMeterSnapshots();
        await Clients.Caller.SendAsync("DamageMeterSnapshotsUpdate", data);
    }

    /// <summary>Request map history</summary>
    public async Task RequestMapHistory()
    {
        var data = _dataProvider.GetMapHistory();
        await Clients.Caller.SendAsync("MapHistoryUpdate", data);
    }

    // ===== Client actions (commands sent from mobile) =====

    /// <summary>Toggle death alert for a party member</summary>
    public async Task ToggleDeathAlert(string playerGuid, bool isActive)
    {
        _dataProvider.ToggleDeathAlert(playerGuid, isActive);
        await Clients.All.SendAsync("PartyUpdate", _dataProvider.GetParty());
    }

    /// <summary>Reset damage meter</summary>
    public async Task ResetDamageMeter()
    {
        _dataProvider.ResetDamageMeter();
        await Clients.All.SendAsync("DamageMeterUpdate", _dataProvider.GetDamageMeter());
    }

    /// <summary>Take a damage meter snapshot</summary>
    public async Task TakeDamageMeterSnapshot()
    {
        _dataProvider.TakeDamageMeterSnapshot();
        await Clients.Caller.SendAsync("DamageMeterSnapshotsUpdate", _dataProvider.GetDamageMeterSnapshots());
    }

    /// <summary>Change damage meter sort type</summary>
    public async Task ChangeDamageMeterSort(string sortType)
    {
        _dataProvider.ChangeDamageMeterSort(sortType);
        await Clients.Caller.SendAsync("DamageMeterUpdate", _dataProvider.GetDamageMeter());
    }

    /// <summary>Remove a dungeon by its hash</summary>
    public async Task RemoveDungeon(string dungeonHash)
    {
        await _dataProvider.RemoveDungeonAsync(dungeonHash);
        await Clients.Caller.SendAsync("DungeonsUpdate", _dataProvider.GetDungeons());
    }

    /// <summary>Reset all dungeon tracking (clear all dungeons)</summary>
    public async Task ResetDungeonTracking()
    {
        await _dataProvider.ResetDungeonTrackingAsync();
        await Clients.Caller.SendAsync("DungeonsUpdate", _dataProvider.GetDungeons());
    }

    /// <summary>Delete all dungeons with zero fame</summary>
    public async Task DeleteDungeonsWithZeroFame()
    {
        await _dataProvider.DeleteDungeonsWithZeroFameAsync();
        await Clients.Caller.SendAsync("DungeonsUpdate", _dataProvider.GetDungeons());
    }

    /// <summary>Delete all dungeons entered today</summary>
    public async Task DeleteDungeonsFromToday()
    {
        await _dataProvider.DeleteDungeonsFromTodayAsync();
        await Clients.Caller.SendAsync("DungeonsUpdate", _dataProvider.GetDungeons());
    }

    /// <summary>Set whether damage meter resets on map change</summary>
    public async Task SetDamageMeterResetOnMapChange(bool active)
    {
        _dataProvider.SetDamageMeterResetOnMapChange(active);
        await Clients.Caller.SendAsync("DamageMeterUpdate", _dataProvider.GetDamageMeter());
    }

    /// <summary>Request current logging settings</summary>
    public async Task RequestLoggingSettings()
    {
        var settings = _dataProvider.GetLoggingSettings();
        await Clients.Caller.SendAsync("LoggingSettingsUpdate", settings);
    }

    /// <summary>Set logging tracking settings</summary>
    public async Task SetLoggingSettings(bool isTrackingSilver, bool isTrackingFame, bool isTrackingMobLoot)
    {
        _dataProvider.SetLoggingSettings(isTrackingSilver, isTrackingFame, isTrackingMobLoot);
        await Clients.Caller.SendAsync("LoggingSettingsUpdate", _dataProvider.GetLoggingSettings());
    }

    // ===== Gathering commands =====

    /// <summary>Get current gathering tracking status</summary>
    public async Task RequestGatheringStatus()
    {
        var isActive = _dataProvider.IsGatheringActive();
        await Clients.Caller.SendAsync("GatheringStatusUpdate", isActive);
    }

    /// <summary>Toggle gathering tracking on/off</summary>
    public async Task SetGatheringActive(bool active)
    {
        _dataProvider.SetGatheringActive(active);
        await Clients.Caller.SendAsync("GatheringStatusUpdate", active);
    }

    /// <summary>Remove gathering entries by their GUIDs</summary>
    public async Task RemoveGatheringEntries(List<string> guids)
    {
        await _dataProvider.RemoveGatheringEntriesAsync(guids);
        await Clients.Caller.SendAsync("GatheringUpdate", _dataProvider.GetGathering());
    }
}
