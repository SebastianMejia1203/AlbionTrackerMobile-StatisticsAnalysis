using Serilog;
using StatisticsAnalysisTool.Cluster;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Dungeon.Models;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Exceptions;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Models.NetworkModel;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.Properties;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using StatisticsAnalysisTool.Diagnostics;
using Loot = StatisticsAnalysisTool.Dungeon.Models.Loot;
using ValueType = StatisticsAnalysisTool.Enumerations.ValueType;
// ReSharper disable PossibleMultipleEnumeration

namespace StatisticsAnalysisTool.Dungeon;

public sealed class DungeonController
{
    private const int MaxDungeons = 9999;
    private const int NumberOfDungeonsUntilSaved = 1;

    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly TrackingController _trackingController;
    private Guid? _currentGuid;
    private Guid? _lastMapGuid;
    private int _addDungeonCounter;
    private readonly List<DiscoveredItem> _discoveredLoot = new();
    private ObservableCollection<Guid> _lastGuidWithRecognizedLevel = new();

    public DungeonController(TrackingController trackingController, MainWindowViewModel mainWindowViewModel)
    {
        _trackingController = trackingController;
        _mainWindowViewModel = mainWindowViewModel;

        if (_mainWindowViewModel?.DungeonBindings?.Dungeons != null)
        {
            _mainWindowViewModel.DungeonBindings.Dungeons.CollectionChanged += OnCollectionChanged;
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _mainWindowViewModel?.DungeonBindings?.Stats.Set(_mainWindowViewModel?.DungeonBindings?.Dungeons);
    }

    public async Task AddDungeonAsync(MapType mapType, Guid? mapGuid, string mainMapIndex = null)
    {
        // Use mainMapIndex from JoinResponse directly to avoid race condition with ChangeClusterResponse
        var effectiveMainMapIndex = mainMapIndex ?? ClusterController.CurrentCluster.MainClusterIndex;

        Log.Information("[DUNGEON-TRACE] === STEP 1: AddDungeonAsync START ===");
        Log.Information("[DUNGEON-TRACE] Input: mapType={MapType}, mapGuid={MapGuid}, mainMapIndex='{MainMapIndex}'", mapType, mapGuid, mainMapIndex);
        Log.Information("[DUNGEON-TRACE] EffectiveMainMapIndex='{EffectiveMainMapIndex}' (from {Source})",
            effectiveMainMapIndex, mainMapIndex != null ? "JoinResponse" : "CurrentCluster");
        Log.Information("[DUNGEON-TRACE] State: _currentGuid={CurrentGuid}, _lastMapGuid={LastMapGuid}", _currentGuid, _lastMapGuid);
        Log.Information("[DUNGEON-TRACE] CurrentCluster: Index='{Index}', MainClusterIndex='{MainClusterIndex}', MapType={ClusterMapType}, Guid={ClusterGuid}",
            ClusterController.CurrentCluster.Index, ClusterController.CurrentCluster.MainClusterIndex, ClusterController.CurrentCluster.MapType, ClusterController.CurrentCluster.Guid);

        if (!_trackingController.IsTrackingAllowedByMainCharacter())
        {
            Log.Information("[DUNGEON-TRACE] SKIPPED: Tracking not allowed by main character");
            return;
        }

        UpdateDungeonSaveTimerUi();

        _currentGuid = mapGuid;

        var isDungeonCluster = IsDungeonCluster(mapType, mapGuid);
        var existLastDungeon = ExistDungeon(_lastMapGuid);
        var existCurrentDungeon = ExistDungeon(_currentGuid);
        Log.Information("[DUNGEON-TRACE] Evaluation: IsDungeonCluster={IsDungeon}, ExistDungeon(_lastMapGuid)={ExistLast}, ExistDungeon(_currentGuid)={ExistCurrent}",
            isDungeonCluster, existLastDungeon, existCurrentDungeon);

        // Last map is a dungeon, add new map
        if (IsDungeonCluster(mapType, mapGuid)
            && ExistDungeon(_lastMapGuid)
            && mapType is not MapType.CorruptedDungeon
            && mapType is not MapType.HellGate
            && mapType is not MapType.Mists
            && mapType is not MapType.MistsDungeon)
        {
            Log.Information("[DUNGEON-TRACE] BRANCH: ADD FLOOR to existing dungeon (lastMapGuid={LastGuid})", _lastMapGuid);
            if (AddClusterToExistDungeon(mapGuid, _lastMapGuid, out var currentDungeon))
            {
                Log.Information("[DUNGEON-TRACE] Floor added. Dungeon GuidList count: {Count}, MainMapIndex='{MainMapIndex}', MainMapName='{MainMapName}'",
                    currentDungeon?.GuidList?.Count, currentDungeon?.MainMapIndex, currentDungeon?.MainMapName);
                currentDungeon.AddTimer(DateTime.UtcNow);
            }
        }
        // Add new dungeon
        else if (IsDungeonCluster(mapType, mapGuid)
                 && !ExistDungeon(_lastMapGuid)
                 && !ExistDungeon(_currentGuid)
                 || (IsDungeonCluster(mapType, mapGuid)
                 && mapType is MapType.CorruptedDungeon or MapType.HellGate or MapType.Mists or MapType.MistsDungeon or MapType.AbyssalDepths))
        {
            Log.Information("[DUNGEON-TRACE] BRANCH: CREATE NEW dungeon. mapType={MapType}, effectiveMainMapIndex='{MainMapIndex}'",
                mapType, effectiveMainMapIndex);

            UpdateDungeonSaveTimerUi(mapType);

            if (mapType is MapType.CorruptedDungeon or MapType.HellGate or MapType.Mists or MapType.MistsDungeon or MapType.AbyssalDepths)
            {
                var lastDungeon = GetDungeon(_lastMapGuid);
                lastDungeon?.EndTimer();
            }

            _mainWindowViewModel.DungeonBindings.Dungeons.Where(x => x.Status != DungeonStatus.Done).ToList().ForEach(x => x.Status = DungeonStatus.Done);

            var newDungeon = CreateNewDungeon(mapType, effectiveMainMapIndex, mapGuid);
            Log.Information("[DUNGEON-TRACE] New dungeon created: Type={DunType}, Mode={Mode}, MainMapIndex='{MainMapIndex}', MainMapName='{MainMapName}', Tier={Tier}, Faction={Faction}",
                newDungeon?.GetType().Name, newDungeon?.Mode, newDungeon?.MainMapIndex, newDungeon?.MainMapName, newDungeon?.Tier, newDungeon?.Faction);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _mainWindowViewModel.DungeonBindings.Dungeons.Insert(0, newDungeon);
            });
        }
        // Activate exist dungeon again
        else if (IsDungeonCluster(mapType, mapGuid)
                 && !ExistDungeon(_lastMapGuid)
                 && ExistDungeon(_currentGuid)
                 || IsDungeonCluster(mapType, mapGuid)
                 && mapType is MapType.CorruptedDungeon or MapType.HellGate or MapType.Mists or MapType.MistsDungeon or MapType.AbyssalDepths)
        {
            Log.Information("[DUNGEON-TRACE] BRANCH: REACTIVATE existing dungeon (currentGuid={CurrentGuid})", _currentGuid);
            UpdateDungeonSaveTimerUi(mapType);

            var currentDungeon = GetDungeon(_currentGuid);
            Log.Information("[DUNGEON-TRACE] Reactivated: Type={DunType}, Mode={Mode}, MainMapName='{MainMapName}', Tier={Tier}",
                currentDungeon?.GetType().Name, currentDungeon?.Mode, currentDungeon?.MainMapName, currentDungeon?.Tier);
            currentDungeon.Status = DungeonStatus.Active;
            currentDungeon.AddTimer(DateTime.UtcNow);
        }
        // Make last dungeon done
        else if (mapGuid == null && ExistDungeon(_lastMapGuid))
        {
            Log.Information("[DUNGEON-TRACE] BRANCH: CLOSE dungeon (lastMapGuid={LastGuid}) - exited to open world", _lastMapGuid);
            var lastDungeon = GetDungeon(_lastMapGuid);
            Log.Information("[DUNGEON-TRACE] Closed: Type={DunType}, Mode={Mode}, MainMapName='{MainMapName}', Tier={Tier}, Fame={Fame}, Silver={Silver}",
                lastDungeon?.GetType().Name, lastDungeon?.Mode, lastDungeon?.MainMapName, lastDungeon?.Tier, lastDungeon?.Fame, lastDungeon?.Silver);
            lastDungeon.EndTimer();
            lastDungeon.Status = DungeonStatus.Done;
            await SaveInFileAfterExceedingLimit(NumberOfDungeonsUntilSaved);
            _lastGuidWithRecognizedLevel = [];
        }
        else
        {
            Log.Information("[DUNGEON-TRACE] BRANCH: NO MATCH - not a dungeon transition or no action taken");
        }

        _lastMapGuid = mapGuid;
        Log.Information("[DUNGEON-TRACE] === STEP 1 END: _lastMapGuid updated to {LastMapGuid} ===", _lastMapGuid);

        await RemoveDungeonsAfterCertainNumberAsync(_mainWindowViewModel.DungeonBindings.Dungeons, MaxDungeons);
        await Application.Current.Dispatcher.InvokeAsync(_mainWindowViewModel.DungeonBindings.UpdateFilteredDungeonsAsync);
    }

    private static DungeonBaseFragment CreateNewDungeon(MapType mapType, string mainMapIndex, Guid? guid)
    {
        Log.Information("[DUNGEON-TRACE] === STEP 2: CreateNewDungeon ===");
        Log.Information("[DUNGEON-TRACE] Input: mapType={MapType}, mainMapIndex='{MainMapIndex}', guid={Guid}", mapType, mainMapIndex, guid);

        if (guid == null)
        {
            Log.Information("[DUNGEON-TRACE] ABORT: guid is null");
            return null;
        }

        DungeonBaseFragment newDungeon;
        switch (mapType)
        {
            case MapType.RandomDungeon:
                var dungeonMode = DungeonData.GetDungeonMode(mainMapIndex);
                Log.Information("[DUNGEON-TRACE] RandomDungeon: DungeonData.GetDungeonMode('{MainMapIndex}') = {DungeonMode}", mainMapIndex, dungeonMode);
                newDungeon = new RandomDungeonFragment((Guid) guid, mapType, dungeonMode, mainMapIndex);
                break;
            case MapType.CorruptedDungeon:
                Log.Information("[DUNGEON-TRACE] CorruptedDungeon: mode=Corrupted");
                newDungeon = new CorruptedFragment((Guid) guid, mapType, DungeonMode.Corrupted, mainMapIndex);
                break;
            case MapType.HellGate:
                Log.Information("[DUNGEON-TRACE] HellGate: mode=HellGate");
                newDungeon = new HellGateFragment((Guid) guid, mapType, DungeonMode.HellGate, mainMapIndex);
                break;
            case MapType.Expedition:
                Log.Information("[DUNGEON-TRACE] Expedition: mode=Expedition");
                newDungeon = new ExpeditionFragment((Guid) guid, mapType, DungeonMode.Expedition, mainMapIndex);
                break;
            case MapType.Mists:
                var tier = (Tier) Enum.ToObject(typeof(Tier), MistsData.GetTier(ClusterController.CurrentCluster.WorldMapDataType));
                Log.Information("[DUNGEON-TRACE] Mists: WorldMapDataType='{WorldMapDataType}', MistsRarity={MistsRarity}, tier={Tier}",
                    ClusterController.CurrentCluster.WorldMapDataType, ClusterController.CurrentCluster.MistsRarity, tier);
                newDungeon = new MistsFragment((Guid) guid, mapType, DungeonMode.Mists, mainMapIndex, ClusterController.CurrentCluster.MistsRarity, tier);
                break;
            case MapType.MistsDungeon:
                Log.Information("[DUNGEON-TRACE] MistsDungeon: MistsDungeonTier={MistsDungeonTier}", ClusterController.CurrentCluster.MistsDungeonTier);
                newDungeon = new MistsDungeonFragment((Guid) guid, mapType, DungeonMode.MistsDungeon, mainMapIndex, ClusterController.CurrentCluster.MistsDungeonTier);
                break;
            case MapType.AbyssalDepths:
                Log.Information("[DUNGEON-TRACE] AbyssalDepths: mode=AbyssalDepths");
                newDungeon = new AbyssalDepthsFragment((Guid) guid, mapType, DungeonMode.AbyssalDepths, mainMapIndex);
                break;
            default:
                Log.Information("[DUNGEON-TRACE] UNKNOWN mapType={MapType} - returning null", mapType);
                newDungeon = null;
                break;
        }

        Log.Information("[DUNGEON-TRACE] === STEP 2 RESULT: Type={DunType}, MainMapIndex='{MainMapIndex}', MainMapName='{MainMapName}', ClusterType={ClusterType} ===",
            newDungeon?.GetType().Name, newDungeon?.MainMapIndex, newDungeon?.MainMapName, newDungeon?.ClusterType);
        return newDungeon;
    }

    public void ResetDungeons()
    {
        _mainWindowViewModel.DungeonBindings.Dungeons.Clear();
        Application.Current.Dispatcher.Invoke(() => { _mainWindowViewModel?.DungeonBindings?.Dungeons?.Clear(); });
    }

    public void ResetDungeonsByDateAscending(DateTime date)
    {
        var dungeonsToDelete = _mainWindowViewModel.DungeonBindings.Dungeons?.Where(x => x.EnterDungeonFirstTime >= date).ToList();
        foreach (var dungeonObject in dungeonsToDelete ?? [])
        {
            _mainWindowViewModel.DungeonBindings.Dungeons?.Remove(dungeonObject);
        }

        var trackingDungeonsToDelete = _mainWindowViewModel?.DungeonBindings?.Dungeons?.Where(x => x.EnterDungeonFirstTime >= date).ToList();
        foreach (var dungeonObject in trackingDungeonsToDelete ?? [])
        {
            _mainWindowViewModel?.DungeonBindings?.Dungeons?.Remove(dungeonObject);
        }
    }

    public void DeleteDungeonsWithZeroFame()
    {
        var dungeonsToDelete = _mainWindowViewModel.DungeonBindings.Dungeons?.Where(x => x.Fame <= 0 && x.Status != DungeonStatus.Active).ToList();
        foreach (var dungeonObject in dungeonsToDelete ?? [])
        {
            _mainWindowViewModel.DungeonBindings.Dungeons?.Remove(dungeonObject);
        }
    }

    public void RemoveDungeon(string dungeonHash)
    {
        var dungeon = _mainWindowViewModel.DungeonBindings.Dungeons.FirstOrDefault(x => x.DungeonHash.Contains(dungeonHash));

        if (dungeon == null)
        {
            return;
        }

        var dialog = new DialogWindow(LocalizationController.Translation("REMOVE_DUNGEON"), LocalizationController.Translation("SURE_YOU_WANT_TO_REMOVE_DUNGEON"));
        var dialogResult = dialog.ShowDialog();

        if (dialogResult is not true)
        {
            return;
        }

        _ = _mainWindowViewModel.DungeonBindings.Dungeons.Remove(dungeon);
    }

    private async Task RemoveDungeonsAfterCertainNumberAsync(ICollection<DungeonBaseFragment> dungeons, int dungeonLimit)
    {
        try
        {
            var toDelete = dungeons?.Count - dungeonLimit;

            if (toDelete <= 0)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                for (var i = toDelete; i <= 0; i--)
                {
                    var dateTime = GetLowestDate(dungeons);
                    if (dateTime == null)
                    {
                        continue;
                    }

                    var removableItem = dungeons?.FirstOrDefault(x => x.EnterDungeonFirstTime == dateTime);
                    dungeons?.Remove(removableItem);
                }

                await _mainWindowViewModel.DungeonBindings.UpdateFilteredDungeonsAsync();
            });
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
    }

    public async Task RemoveDungeonByHashAsync(IEnumerable<string> dungeonHash)
    {
        await foreach (var dungeons in _mainWindowViewModel.DungeonBindings.Dungeons.ToList().ToAsyncEnumerable())
        {
            if (dungeonHash.Contains(dungeons.DungeonHash))
            {
                _mainWindowViewModel.DungeonBindings.Dungeons.Remove(dungeons);
            }
        }

        await SaveInFileAsync();
    }

    private bool AddClusterToExistDungeon(Guid? currentGuid, Guid? lastGuid, out DungeonBaseFragment dungeon)
    {
        if (currentGuid != null && lastGuid != null && _mainWindowViewModel.DungeonBindings.Dungeons?.Any(x => x.GuidList.Contains((Guid) currentGuid)) != true)
        {
            var dun = _mainWindowViewModel.DungeonBindings.Dungeons?.FirstOrDefault(x => x.GuidList.Contains((Guid) lastGuid));
            dun?.GuidList.Add((Guid) currentGuid);

            dungeon = dun;

            return _mainWindowViewModel.DungeonBindings.Dungeons?.Any(x => x.GuidList.Contains((Guid) currentGuid)) ?? false;
        }

        dungeon = null;
        return false;
    }

    public static DateTime? GetLowestDate(IEnumerable<DungeonBaseFragment> items)
    {
        if (items?.Count() <= 0)
        {
            return null;
        }

        try
        {
            return items?.Select(x => x.EnterDungeonFirstTime).Min();
        }
        catch (ArgumentNullException e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            return null;
        }
    }

    #region Dungeon object

    public void SetDungeonChestOpen(int id, List<Guid> allowedToOpen)
    {
        if (!_trackingController.EntityController.IsAnyEntityInParty(allowedToOpen))
        {
            return;
        }

        if (_currentGuid != null)
        {
            try
            {
                var dun = GetDungeon((Guid) _currentGuid);
                var chest = dun?.Events?.FirstOrDefault(x => x?.Id == id);
                if (chest != null)
                {
                    chest.Status = ChestStatus.Open;
                    chest.Opened = DateTime.UtcNow;
                }
            }
            catch (Exception e)
            {
                DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
                Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            }
        }
    }

    private DungeonBaseFragment GetDungeon(Guid? guid)
    {
        return guid == null ? null : _mainWindowViewModel.DungeonBindings.Dungeons.FirstOrDefault(x => x.GuidList.Contains((Guid) guid));
    }

    public async Task SetDungeonEventInformationAsync(int id, string uniqueName)
    {
        if (_currentGuid == null || uniqueName == null)
        {
            return;
        }

        try
        {
            var dun = GetDungeon((Guid) _currentGuid);
            if (dun == null || dun.Events?.Any(x => x.Id == id) == true)
            {
                return;
            }

            Log.Information("[DUNGEON-TRACE] === STEP 3: SetDungeonEventInformationAsync ===");
            Log.Information("[DUNGEON-TRACE] Event id={Id}, uniqueName='{UniqueName}'", id, uniqueName);

            var eventObject = new PointOfInterest(id, uniqueName);
            await Application.Current.Dispatcher.InvokeAsync(() => { dun.Events?.Add(eventObject); });

            if (dun.Faction == Faction.Unknown)
            {
                var detectedFaction = DungeonData.GetFaction(uniqueName);
                Log.Information("[DUNGEON-TRACE] Faction detection: DungeonData.GetFaction('{UniqueName}') = {Faction}", uniqueName, detectedFaction);
                dun.Faction = detectedFaction;
            }

            if (dun.Mode == DungeonMode.Unknown)
            {
                var detectedMode = DungeonData.GetDungeonMode(uniqueName);
                Log.Information("[DUNGEON-TRACE] Mode detection: DungeonData.GetDungeonMode('{UniqueName}') = {Mode}", uniqueName, detectedMode);
                dun.Mode = detectedMode;
            }

            Log.Information("[DUNGEON-TRACE] Dungeon after event: Faction={Faction}, Mode={Mode}, Events count={EventCount}",
                dun.Faction, dun.Mode, dun.Events?.Count);
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
    }

    public void AddValueToDungeon(double value, ValueType valueType, CityFaction cityFaction = CityFaction.Unknown)
    {
        try
        {
            lock (_mainWindowViewModel.DungeonBindings.Dungeons)
            {
                var dun = _mainWindowViewModel.DungeonBindings.Dungeons?.FirstOrDefault(x => _currentGuid != null && x.GuidList.Contains((Guid) _currentGuid) && x.Status == DungeonStatus.Active);

                switch (dun)
                {
                    case RandomDungeonFragment standardDun:
                        standardDun.Add(value, valueType, cityFaction);
                        break;
                    case HellGateFragment hellGate:
                        hellGate.Add(value, valueType);
                        break;
                    case CorruptedFragment corrupted:
                        corrupted.Add(value, valueType);
                        break;
                    case ExpeditionFragment expedition:
                        expedition.Add(value, valueType);
                        break;
                    case MistsFragment mists:
                        mists.Add(value, valueType);
                        break;
                    case MistsDungeonFragment mistsDungeon:
                        mistsDungeon.Add(value, valueType);
                        break;
                    case AbyssalDepthsFragment abyssalDepths:
                        abyssalDepths.Add(value, valueType);
                        break;
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    public void SetDiedIfInDungeon(DiedObject dieObject)
    {
        if (_currentGuid == null || _trackingController.EntityController.LocalUserData.Username == null)
        {
            return;
        }

        var dungeon = _mainWindowViewModel.DungeonBindings.Dungeons.FirstOrDefault(x => x.GuidList.Contains((Guid) _currentGuid));

        if (dungeon is null)
        {
            return;
        }

        if (dieObject.DiedName == _trackingController.EntityController.LocalUserData.Username)
        {
            dungeon.KillStatus = KillStatus.LocalPlayerDead;
        }
        else if (dieObject.KilledBy == _trackingController.EntityController.LocalUserData.Username)
        {
            dungeon.KillStatus = KillStatus.OpponentDead;
        }

        dungeon.DiedName = dieObject.DiedName;
        dungeon.KilledBy = dieObject.KilledBy;
    }

    #endregion

    #region Tier / Level recognize

    public void AddLevelToCurrentDungeon(int? mobIndex, double hitPointsMax)
    {
        if (_currentGuid is not { } currentGuid)
        {
            return;
        }

        if (_lastGuidWithRecognizedLevel.Contains(currentGuid))
        {
            return;
        }

        if (mobIndex is null || ClusterController.CurrentCluster.Guid != currentGuid)
        {
            return;
        }

        //if (ClusterController.CurrentCluster.MapType != MapType.Expedition
        //    && ClusterController.CurrentCluster.MapType != MapType.CorruptedDungeon
        //    && ClusterController.CurrentCluster.MapType != MapType.HellGate
        //    && ClusterController.CurrentCluster.MapType != MapType.RandomDungeon
        //    && ClusterController.CurrentCluster.MapType != MapType.Mists
        //    && ClusterController.CurrentCluster.MapType != MapType.MistsDungeon)
        //{
        //    return;
        //}

        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dun = _mainWindowViewModel.DungeonBindings.Dungeons?.FirstOrDefault(x => x.GuidList.Contains(currentGuid) && x.Status == DungeonStatus.Active);
                if (dun is not RandomDungeonFragment randomDungeon)
                {
                    return;
                }

                var recognizedLevel = MobsData.GetMobLevelByIndex((int) mobIndex, hitPointsMax);
                // [DUNGEON-TRACE] Step 4 commented - too noisy (fires per mob)
                // Log.Information("[DUNGEON-TRACE] === STEP 4: AddLevelToCurrentDungeon ===");
                // Log.Information("[DUNGEON-TRACE] mobIndex={MobIndex}, hitPointsMax={HP}, recognizedLevel={RecognizedLevel}, currentLevel={CurrentLevel}", mobIndex, hitPointsMax, recognizedLevel, randomDungeon.Level);

                randomDungeon.Level = randomDungeon.Level < 0 ? recognizedLevel : randomDungeon.Level;

                // Log.Information("[DUNGEON-TRACE] After update: Level={Level}, LevelString='{LevelString}'", randomDungeon.Level, randomDungeon.LevelString);

                if (randomDungeon.Level > 0)
                {
                    _lastGuidWithRecognizedLevel = dun.GuidList;
                }
            });
        }
        catch
        {
            // ignored
        }
    }

    public async Task AddTierToCurrentDungeonAsync(int? mobIndex)
    {
        if (_currentGuid is not { } currentGuid)
        {
            return;
        }

        if (mobIndex is null || ClusterController.CurrentCluster.Guid != currentGuid)
        {
            return;
        }

        //if (ClusterController.CurrentCluster.MapType != MapType.Expedition
        //    && ClusterController.CurrentCluster.MapType != MapType.CorruptedDungeon
        //    && ClusterController.CurrentCluster.MapType != MapType.HellGate
        //    && ClusterController.CurrentCluster.MapType != MapType.RandomDungeon)
        //{
        //    return;
        //}

        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var mobTier = (Tier) MobsData.GetMobTierByIndex((int) mobIndex);
                var dun = _mainWindowViewModel.DungeonBindings.Dungeons?.FirstOrDefault(x => x.GuidList.Contains(currentGuid) && x.Status == DungeonStatus.Active);

                // [DUNGEON-TRACE] Step 5 commented - too noisy (fires per mob)
                // Log.Information("[DUNGEON-TRACE] === STEP 5: AddTierToCurrentDungeonAsync ===");
                // Log.Information("[DUNGEON-TRACE] mobIndex={MobIndex}, mobTier={MobTier}, dunFound={Found}, currentTier={CurrentTier}", mobIndex, mobTier, dun != null, dun?.Tier);

                if (dun == null || dun.Tier >= mobTier)
                {
                    // Log.Information("[DUNGEON-TRACE] Tier NOT updated");
                    return;
                }

                dun.SetTier(mobTier);
                Log.Information("[DUNGEON-TRACE] Tier UPDATED to {NewTier}", dun.Tier);
            });
        }
        catch
        {
            // ignored
        }
    }

    #endregion

    #region Dungeon loot tracking

    private ItemContainerObject _currentItemContainer;

    public void SetCurrentItemContainer(ItemContainerObject itemContainerObject)
    {
        _currentItemContainer = itemContainerObject;
    }

    public void AddDiscoveredItem(DiscoveredItem discoveredItem)
    {
        if (_discoveredLoot.Any(x => x?.ObjectId == discoveredItem?.ObjectId))
        {
            return;
        }

        if (_currentGuid == null)
        {
            return;
        }

        _discoveredLoot.Add(discoveredItem);
    }

    public async Task AddNewLocalPlayerLootOnCurrentDungeonAsync(int containerSlot, Guid containerGuid, Guid userInteractGuid)
    {
        if (_trackingController.EntityController.LocalUserData.InteractGuid != userInteractGuid)
        {
            return;
        }

        if (_currentItemContainer?.ContainerGuid != containerGuid)
        {
            return;
        }

        var itemObjectId = GetItemObjectIdFromContainer(containerSlot);
        var lootedItem = _discoveredLoot.FirstOrDefault(x => x.ObjectId == itemObjectId);

        if (lootedItem == null)
        {
            return;
        }

        await AddLocalPlayerLootedItemToCurrentDungeonAsync(lootedItem);
    }

    private long GetItemObjectIdFromContainer(int containerSlot)
    {
        if (_currentItemContainer == null || _currentItemContainer?.SlotItemIds?.Count is null or <= 0 || _currentItemContainer?.SlotItemIds?.Count <= containerSlot)
        {
            return 0;
        }

        return _currentItemContainer!.SlotItemIds![containerSlot];
    }

    public async Task AddLocalPlayerLootedItemToCurrentDungeonAsync(DiscoveredItem discoveredItem)
    {
        if (_currentGuid == null)
        {
            return;
        }

        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dun = GetDungeon((Guid) _currentGuid);
                if (dun == null)
                {
                    return;
                }

                var uniqueItemName = ItemController.GetUniqueNameByIndex(discoveredItem.ItemIndex);
                if (uniqueItemName.Contains("SILVERBAG"))
                {
                    return;
                }

                dun.Loot.Add(new Loot()
                {
                    EstimatedMarketValueInternal = discoveredItem.EstimatedMarketValueInternal,
                    Quantity = discoveredItem.Quantity,
                    UniqueName = uniqueItemName,
                    UtcDiscoveryTime = discoveredItem.UtcDiscoveryTime
                });
                dun.UpdateTotalSilverValue();
                dun.UpdateMostValuableLoot();
                dun.UpdateMostValuableLootVisibility();
            });
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
    }

    public void ResetLocalPlayerDiscoveredLoot()
    {
        _discoveredLoot.Clear();
    }

    #endregion

    #region Dungeon timer

    private void UpdateDungeonSaveTimerUi(MapType mapType = MapType.Unknown)
    {
        _mainWindowViewModel.DungeonBindings.DungeonCloseTimer.Visibility = mapType == MapType.RandomDungeon ? Visibility.Visible : Visibility.Collapsed;
    }

    #endregion

    #region Expedition

    public async Task UpdateCheckPointAsync(CheckPoint checkPoint)
    {
        if (_currentGuid is not { } currentGuid)
        {
            return;
        }


        if (ClusterController.CurrentCluster.MapType != MapType.Expedition)
        {
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dun = _mainWindowViewModel.DungeonBindings.Dungeons?.FirstOrDefault(x => x.GuidList.Contains(currentGuid) && x.Status == DungeonStatus.Active);
            if (dun is not ExpeditionFragment expedition)
            {
                return;
            }

            var foundCheckPoint = expedition.CheckPoints?.FirstOrDefault(x => x.Id == checkPoint.Id);
            if (foundCheckPoint is null)
            {
                expedition.CheckPoints?.Add(checkPoint);
            }
            else
            {
                foundCheckPoint.Status = checkPoint.Status;
            }

        });
    }

    #endregion

    #region Helper methods

    private bool ExistDungeon(Guid? mapGuid)
    {
        return mapGuid != null && _mainWindowViewModel.DungeonBindings.Dungeons.Any(x => x.GuidList.Contains((Guid) mapGuid));
    }

    private static bool IsDungeonCluster(MapType mapType, Guid? mapGuid)
    {
        return mapGuid != null && mapType is MapType.RandomDungeon or MapType.CorruptedDungeon or MapType.HellGate or MapType.Expedition or MapType.Mists or MapType.MistsDungeon or MapType.AbyssalDepths;
    }

    #endregion

    #region Load / Save file data

    public async Task LoadDungeonFromFileAsync()
    {
        var dungeons = await FileController.LoadAsync<List<DungeonDto>>(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.DungeonRunsFileName));

        var dungeonsToAdd = new List<DungeonBaseFragment>();
        foreach (DungeonDto dungeonDto in dungeons)
        {
            try
            {
                dungeonsToAdd.Add(DungeonMapping.Mapping(dungeonDto));
            }
            catch (MappingException e)
            {
                DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
                Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            }
        }

        _mainWindowViewModel.DungeonBindings.Dungeons.AddRange(dungeonsToAdd.OrderBy(x => x?.EnterDungeonFirstTime).ToList());
        _mainWindowViewModel.DungeonBindings.InitListCollectionView();
    }

    public async Task SaveInFileAsync()
    {
        DirectoryController.CreateDirectoryWhenNotExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName));
        var toSaveDungeons = _mainWindowViewModel.DungeonBindings.Dungeons.Select(DungeonMapping.Mapping).ToList();
        await FileController.SaveAsync(toSaveDungeons, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.DungeonRunsFileName));
        Log.Information("Dungeons saved");
    }

    private async Task SaveInFileAfterExceedingLimit(int limit)
    {
        if (++_addDungeonCounter < limit)
        {
            return;
        }

        await SaveInFileAsync();
        _addDungeonCounter = 0;
    }

    #endregion
}