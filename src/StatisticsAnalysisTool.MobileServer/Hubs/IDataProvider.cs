using StatisticsAnalysisTool.MobileServer.Dtos;

namespace StatisticsAnalysisTool.MobileServer.Hubs;

/// <summary>
/// Interface that the WPF app must implement to provide data to the mobile hub.
/// This decouples the server library from the WPF app's internal types.
/// </summary>
public interface IDataProvider
{
    // Snapshot getters (called when client requests data)
    ServerStatusDto GetServerStatus();
    DashboardDto GetDashboard();
    DamageMeterDto GetDamageMeter();
    DungeonListDto GetDungeons();
    TradeListDto GetTrades();
    GatheringListDto GetGathering();
    PartyDto GetParty();
    GuildDto GetGuild();
    PlayerInfoDto GetPlayerInfo();
    List<LoggingNotificationDto> GetLoggingNotifications(int count = 100);
    List<DamageMeterSnapshotDto> GetDamageMeterSnapshots();
    MapHistoryDto GetMapHistory();

    // Commands from mobile
    void ToggleDeathAlert(string playerGuid, bool isActive);
    void ResetDamageMeter();
    void TakeDamageMeterSnapshot();
    void ChangeDamageMeterSort(string sortType);
    Task RemoveDungeonAsync(string dungeonHash);

    // Dungeon management commands
    Task ResetDungeonTrackingAsync();
    Task DeleteDungeonsWithZeroFameAsync();
    Task DeleteDungeonsFromTodayAsync();

    // Damage meter settings
    void SetDamageMeterResetOnMapChange(bool active);

    // Logging settings
    LoggingSettingsDto GetLoggingSettings();
    void SetLoggingSettings(bool isTrackingSilver, bool isTrackingFame, bool isTrackingMobLoot);

    // Gathering commands
    bool IsGatheringActive();
    void SetGatheringActive(bool active);
    Task RemoveGatheringEntriesAsync(IEnumerable<string> guids);
}
