using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using StatisticsAnalysisTool.Cluster;
using StatisticsAnalysisTool.Dungeon.Models;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.MobileServer.Dtos;
using StatisticsAnalysisTool.MobileServer.Hubs;

namespace StatisticsAnalysisTool.Network.Manager;

/// <summary>
/// Bridges the WPF app's internal data (ViewModels/Controllers) with the mobile server's IDataProvider interface.
/// Reads from the existing controllers and bindings to produce DTOs for the mobile client.
/// </summary>
public class MobileDataProvider : IDataProvider
{
    private readonly TrackingController _trackingController;
    private readonly ViewModels.MainWindowViewModel _mainWindowViewModel;

    public MobileDataProvider(TrackingController trackingController, ViewModels.MainWindowViewModel mainWindowViewModel)
    {
        _trackingController = trackingController;
        _mainWindowViewModel = mainWindowViewModel;
    }

    public ServerStatusDto GetServerStatus()
    {
        var localEntity = _trackingController.EntityController.GetLocalEntity();
        var cluster = ClusterController.CurrentCluster;

        return new ServerStatusDto
        {
            IsTrackingActive = _mainWindowViewModel.IsTrackingActive,
            ServerVersion = "1.0.0",
            PlayerName = localEntity?.Value?.Name ?? string.Empty,
            CurrentCluster = cluster?.MainClusterIndex ?? string.Empty,
            ConnectedClients = 0
        };
    }

    public DashboardDto GetDashboard()
    {
        var db = _mainWindowViewModel.DashboardBindings;
        if (db == null) return new DashboardDto();

        return new DashboardDto
        {
            FamePerHour = db.FamePerHour,
            SilverPerHour = db.SilverPerHour,
            ReSpecPointsPerHour = db.ReSpecPointsPerHour,
            MightPerHour = db.MightPerHour,
            FavorPerHour = db.FavorPerHour,
            SilverCostForReSpecHour = db.SilverCostForReSpecHour,

            TotalGainedFameInSession = db.TotalGainedFameInSession,
            TotalGainedSilverInSession = db.TotalGainedSilverInSession,
            TotalGainedReSpecPointsInSession = db.TotalGainedReSpecPointsInSession,
            TotalGainedMightInSession = db.TotalGainedMightInSession,
            TotalGainedFavorInSession = db.TotalGainedFavorInSession,
            TotalGainedSilverCostForReSpecInSession = db.TotalSilverCostForReSpecInSession,

            FameInPercent = db.FameInPercent,
            SilverInPercent = db.SilverInPercent,
            ReSpecPointsInPercent = db.ReSpecPointsInPercent,
            MightInPercent = db.MightInPercent,
            FavorInPercent = db.FavorInPercent,

            KillsToday = db.KillsToday,
            KillsThisWeek = db.KillsThisWeek,
            KillsThisMonth = db.KillsThisMonth,
            DeathsToday = db.DeathsToday,
            DeathsThisWeek = db.DeathsThisWeek,
            DeathsThisMonth = db.DeathsThisMonth,
            SoloKillsToday = db.SoloKillsToday,
            SoloKillsThisWeek = db.SoloKillsThisWeek,
            SoloKillsThisMonth = db.SoloKillsThisMonth,

            AverageItemPowerWhenKilling = db.AverageItemPowerWhenKilling,
            AverageItemPowerOfTheKilledEnemies = db.AverageItemPowerOfTheKilledEnemies,
            AverageItemPowerWhenDying = db.AverageItemPowerWhenDying,

            RepairCostsToday = db.RepairCostsToday,
            RepairCostsLast7Days = db.RepairCostsLast7Days,
            RepairCostsLast30Days = db.RepairCostsLast30Days,

            LootedChests = db.LootedChests != null ? new LootedChestsDto
            {
                OpenedCommon = db.LootedChests.OpenWorldCommonWeek + db.LootedChests.RandomGroupDungeonCommonWeek + db.LootedChests.RandomSoloDungeonCommonWeek,
                OpenedUncommon = db.LootedChests.OpenWorldUncommonWeek + db.LootedChests.RandomGroupDungeonUncommonWeek + db.LootedChests.RandomSoloDungeonUncommonWeek,
                OpenedRare = db.LootedChests.OpenWorldEpicWeek + db.LootedChests.RandomGroupDungeonEpicWeek + db.LootedChests.RandomSoloDungeonEpicWeek,
                OpenedLegendary = db.LootedChests.OpenWorldLegendaryWeek + db.LootedChests.RandomGroupDungeonLegendaryWeek + db.LootedChests.RandomSoloDungeonLegendaryWeek
            } : new LootedChestsDto(),

            FactionPointStats = _mainWindowViewModel.FactionPointStats?
                .Select(f => new FactionPointStatDto
                {
                    CityFaction = f.CityFaction.ToString(),
                    Value = f.Value,
                    ValuePerHour = f.ValuePerHour
                }).ToList() ?? []
        };
    }

    public DamageMeterDto GetDamageMeter()
    {
        var dmb = _mainWindowViewModel.DamageMeterBindings;
        if (dmb == null) return new DamageMeterDto();

        var fragments = dmb.DamageMeter?.Select(MapDamageMeterFragment).ToList() ?? [];

        return new DamageMeterDto
        {
            Fragments = fragments,
            SortType = dmb.DamageMeterSortSelection.DamageMeterSortType.ToString(),
            IsDamageMeterResetByMapChangeActive = dmb.IsDamageMeterResetByMapChangeActive,
            IsDamageMeterResetBeforeCombatActive = dmb.IsDamageMeterResetBeforeCombatActive
        };
    }

    public DungeonListDto GetDungeons()
    {
        var dngb = _mainWindowViewModel.DungeonBindings;
        if (dngb == null) return new DungeonListDto();

        var dungeons = dngb.Dungeons?.Select(d =>
        {
            // Extract subclass-specific properties
            var level = -1;
            double might = 0, favor = 0, factionCoins = 0, factionFlags = 0;
            var cityFaction = string.Empty;
            var numberOfFloors = d.GuidList?.Count ?? 1;

            if (d is RandomDungeonFragment rdf)
            {
                level = rdf.Level;
                might = rdf.Might;
                favor = rdf.Favor;
                factionCoins = rdf.FactionCoins;
                factionFlags = rdf.FactionFlags;
                cityFaction = rdf.CityFaction.ToString();
            }
            else if (d is CorruptedFragment cf)
            {
                might = cf.Might;
                favor = cf.Favor;
            }
            else if (d is HellGateFragment hgf)
            {
                might = hgf.Might;
                favor = hgf.Favor;
            }
            else if (d is MistsFragment mf)
            {
                might = mf.Might;
                favor = mf.Favor;
            }
            else if (d is MistsDungeonFragment mdf)
            {
                might = mdf.Might;
                favor = mdf.Favor;
            }
            else if (d is AbyssalDepthsFragment adf)
            {
                might = adf.Might;
                favor = adf.Favor;
            }

            return new DungeonFragmentDto
            {
                DungeonHash = d.DungeonHash ?? string.Empty,
                Mode = d.Mode.ToString(),
                MapType = d.MapType.ToString(),
                Status = d.Status.ToString(),
                Tier = d.Tier.ToString(),
                Level = level,
                Faction = d.Faction.ToString(),
                CityFaction = cityFaction,
                EnterDungeonFirstTime = d.EnterDungeonFirstTime.ToString("o"),
                Fame = d.Fame,
                Silver = d.Silver,
                ReSpec = d.ReSpec,
                Might = might,
                Favor = favor,
                FactionCoins = factionCoins,
                FactionFlags = factionFlags,
                FamePerHour = d.FamePerHour,
                SilverPerHour = d.SilverPerHour,
                ReSpecPerHour = d.ReSpecPerHour,
                TotalRunTimeInSeconds = d.TotalRunTimeInSeconds,
                NumberOfFloors = numberOfFloors,
                TotalValue = d.TotalValue,
                MainMapIndex = d.MainMapIndex ?? string.Empty,
                MainMapName = d.MainMapName ?? string.Empty,
                KilledBy = d.KilledBy ?? string.Empty,
                DiedName = d.DiedName ?? string.Empty,
                KillStatus = d.KillStatus.ToString(),
                Loot = d.Loot?.Select(l => new DungeonLootDto
                {
                    UniqueName = l.UniqueName ?? string.Empty,
                    Quantity = l.Quantity,
                    EstimatedMarketValue = l.EstimatedMarketValueInternal
                }).ToList() ?? [],
                Events = d.Events?.Select(e => new MobileServer.Dtos.DungeonEventDto
                {
                    Id = e.Id,
                    Type = e.Type.ToString(),
                    UniqueName = e.UniqueName ?? string.Empty,
                    Status = e.Status.ToString(),
                    IsBossChest = e.IsBossChest,
                    Rarity = e.Rarity.ToString(),
                    ShrineType = e.ShrineType.ToString(),
                    ShrineBuff = e.ShrineBuff.ToString()
                }).ToList() ?? [],
                MostValuableLoot = d.MostValuableLoot != null ? new DungeonLootDto
                {
                    UniqueName = d.MostValuableLoot.UniqueName ?? string.Empty,
                    Quantity = d.MostValuableLoot.Quantity,
                    EstimatedMarketValue = d.MostValuableLoot.EstimatedMarketValueInternal
                } : null
            };
        }).ToList() ?? [];

        return new DungeonListDto
        {
            Dungeons = dungeons,
            Stats = MapDungeonStats(dngb.Stats)
        };
    }

    public TradeListDto GetTrades()
    {
        var tmb = _mainWindowViewModel.TradeMonitoringBindings;
        if (tmb == null) return new TradeListDto();

        var trades = tmb.Trades?.Select(t =>
        {
            var mc = t.MailContent;
            var ic = t.InstantBuySellContent;

            var quantity = mc?.Quantity ?? ic?.Quantity ?? 0;
            var totalPrice = mc?.TotalPrice.DoubleValue ?? ic?.TotalPrice.DoubleValue ?? 0;
            var unitPrice = mc?.UnitPriceWithoutTax.DoubleValue ?? ic?.UnitPrice.DoubleValue ?? 0;
            var taxRate = mc?.TaxRate ?? ic?.TaxRate ?? 0;
            var taxSetupRate = mc?.TaxSetupRate ?? 0;
            var taxAmount = mc?.TaxPrice.DoubleValue ?? ic?.TaxPrice.DoubleValue ?? 0;
            var taxSetupAmount = mc?.TaxSetupPrice.DoubleValue ?? 0;
            var distanceFee = mc?.TotalDistanceFee.DoubleValue ?? ic?.TotalDistanceFee.DoubleValue ?? 0;

            // Revenue: for sales = totalPrice - taxes; for purchases = totalPrice + taxes (what buyer paid)
            double totalRevenue;
            if (t.Type is Trade.TradeType.InstantSell or Trade.TradeType.Mail)
                totalRevenue = mc?.TotalPriceWithDeductedTaxes.DoubleValue ?? ic?.TotalPriceWithDeductedTaxes.DoubleValue ?? totalPrice;
            else
                totalRevenue = totalPrice;

            return new MobileServer.Dtos.TradeDto
            {
                Id = t.Id,
                Timestamp = t.Timestamp.ToString("o"),
                ClusterIndex = t.ClusterIndex ?? string.Empty,
                Type = t.Type.ToString(),
                LocationName = t.Location.ToString(),
                Item = t.Item != null ? MapItem(t.Item) : null,
                Quantity = quantity,
                TotalPrice = totalPrice,
                UnitPrice = unitPrice,
                TaxRate = taxRate,
                TaxSetupRate = taxSetupRate,
                TaxAmount = taxAmount,
                TaxSetupAmount = taxSetupAmount,
                TotalRevenue = totalRevenue,
                DistanceFee = distanceFee,
                Description = t.Description ?? string.Empty
            };
        }).ToList() ?? [];

        return new TradeListDto
        {
            Trades = trades,
            Stats = tmb.TradeStatsObject != null ? MapTradeStats(tmb.TradeStatsObject) : new TradeStatsDto()
        };
    }

    public GatheringListDto GetGathering()
    {
        var gb = _mainWindowViewModel.GatheringBindings;
        if (gb == null) return new GatheringListDto();

        var items = gb.GatheredCollection?.Select(g =>
        {
            var idx = g.ClusterIndex ?? string.Empty;
            var mapDisplayName = !string.IsNullOrEmpty(idx)
                ? ClusterController.ComposingMapInfoString(idx, g.MapType, null)
                : g.MapType.ToString();
            var clusterMode = !string.IsNullOrEmpty(idx)
                ? WorldData.GetClusterTypeByIndex(idx).ToString()
                : string.Empty;

            return new GatheredDto
            {
                Guid = g.Guid.ToString(),
                UniqueName = g.UniqueName ?? string.Empty,
                ItemName = g.Item?.LocalizedName ?? string.Empty,
                GainedStandardAmount = g.GainedStandardAmount,
                GainedBonusAmount = g.GainedBonusAmount,
                GainedPremiumBonusAmount = g.GainedPremiumBonusAmount,
                GainedTotalAmount = g.GainedTotalAmount,
                GainedFame = g.GainedFame,
                MiningProcesses = g.MiningProcesses,
                EstimatedMarketValue = g.EstimatedMarketValue.DoubleValue,
                TotalMarketValue = g.TotalMarketValueWithCulture,
                ClusterIndex = idx,
                MapType = g.MapType.ToString(),
                MapDisplayName = mapDisplayName,
                ClusterMode = clusterMode,
                TimestampUtc = g.TimestampUtc > 0 ? new DateTime(g.TimestampUtc, DateTimeKind.Utc).ToString("o") : string.Empty,
                HasBeenFished = g.HasBeenFished
            };
        }).ToList() ?? [];

        var stats = gb.GatheringStats;

        return new GatheringListDto
        {
            GatheredItems = items,
            Stats = stats != null ? new GatheringStatsDto
            {
                TotalMiningProcesses = (int)stats.TotalMiningProcesses,
                TotalResources = (int)stats.TotalResources,
                TotalGainedSilver = stats.TotalGainedSilverString,
                GainedSilverByWood = stats.GainedSilverByWood,
                GainedSilverByHide = stats.GainedSilverByHide,
                GainedSilverByOre = stats.GainedSilverByOre,
                GainedSilverByRock = stats.GainedSilverByRock,
                GainedSilverByFiber = stats.GainedSilverByFiber,
                GainedSilverByFish = stats.GainedSilverByFish,
                WoodCount = stats.GatheredWood?.Count ?? 0,
                HideCount = stats.GatheredHide?.Count ?? 0,
                OreCount = stats.GatheredOre?.Count ?? 0,
                RockCount = stats.GatheredRock?.Count ?? 0,
                FiberCount = stats.GatheredFiber?.Count ?? 0,
                FishCount = stats.GatheredFish?.Count ?? 0
            } : new GatheringStatsDto()
        };
    }

    public PartyDto GetParty()
    {
        var pb = _mainWindowViewModel.PartyBindings;
        if (pb == null) return new PartyDto();

        var players = pb.Party?.Select(p => new PartyPlayerDto
        {
            Guid = p.Guid.ToString(),
            Username = p.Username ?? string.Empty,
            IsLocalPlayer = p.IsLocalPlayer,
            IsPlayerInspected = p.IsPlayerInspected,
            AverageItemPower = p.AverageItemPower?.ItemPower ?? 0,
            AverageBasicItemPower = p.AverageBasicItemPower?.ItemPower ?? 0,
            ItemPowerCondition = p.ItemPowerCondition.ToString(),
            BasicItemPowerCondition = p.BasicItemPowerCondition.ToString(),
            IsDeathAlertActive = p.IsDeathAlertActive,
            Equipment = new EquipmentDto
            {
                MainHand = p.MainHand != null ? MapItem(p.MainHand) : null,
                OffHand = p.OffHand != null ? MapItem(p.OffHand) : null,
                Head = p.Head != null ? MapItem(p.Head) : null,
                Chest = p.Chest != null ? MapItem(p.Chest) : null,
                Shoes = p.Shoes != null ? MapItem(p.Shoes) : null,
                Bag = p.Bag != null ? MapItem(p.Bag) : null,
                Cape = p.Cape != null ? MapItem(p.Cape) : null,
                Mount = p.Mount != null ? MapItem(p.Mount) : null,
                Potion = p.Potion != null ? MapItem(p.Potion) : null,
                BuffFood = p.BuffFood != null ? MapItem(p.BuffFood) : null
            }
        }).ToList() ?? [];

        return new PartyDto
        {
            Players = players,
            AveragePartyIp = pb.AveragePartyIp,
            AveragePartyBasicIp = pb.AveragePartyBasicIp,
            MinimalItemPower = pb.MinimalItemPower,
            MaximumItemPower = pb.MaximumItemPower
        };
    }

    public GuildDto GetGuild()
    {
        var gb = _mainWindowViewModel.GuildBindings;
        if (gb == null) return new GuildDto();

        return new GuildDto
        {
            SiphonedEnergyList = gb.SiphonedEnergyList?.Select(s => new SiphonedEnergyItemDto
            {
                GuildName = s.GuildName ?? string.Empty,
                CharacterName = s.CharacterName ?? string.Empty,
                Quantity = s.Quantity.IntegerValue,
                Timestamp = s.Timestamp.ToString("o"),
                IsDeposit = s.IsDeposit
            }).ToList() ?? [],
            SiphonedEnergyOverview = gb.SiphonedEnergyOverviewList?.Select(s => new SiphonedEnergyOverviewDto
            {
                CharacterName = s.CharacterName ?? string.Empty,
                TotalQuantity = s.Quantity.IntegerValue
            }).ToList() ?? [],
            TotalSiphonedEnergyQuantity = gb.TotalSiphonedEnergyQuantity,
            SiphonedEnergyLastUpdate = gb.SiphonedEnergyLastUpdate.ToString("o")
        };
    }

    public PlayerInfoDto GetPlayerInfo()
    {
        var localEntity = _trackingController.EntityController.GetLocalEntity();
        var cluster = ClusterController.CurrentCluster;
        var utb = _mainWindowViewModel.UserTrackingBindings;

        // Use Index (actual map) for display, not MainClusterIndex (parent cluster)
        var mapDisplayName = cluster?.UniqueClusterName ?? cluster?.UniqueName ?? string.Empty;

        return new PlayerInfoDto
        {
            Username = localEntity?.Value?.Name ?? utb?.Username ?? string.Empty,
            Guild = utb?.GuildName ?? string.Empty,
            Alliance = utb?.AllianceName ?? string.Empty,
            CurrentMap = cluster?.Index ?? string.Empty,
            CurrentMapType = cluster?.MapType.ToString() ?? string.Empty,
            MapDisplayName = mapDisplayName,
            ClusterMode = cluster?.ClusterMode.ToString() ?? string.Empty,
            Tier = cluster?.TierString ?? string.Empty,
            MapTypeString = cluster?.MapTypeString ?? string.Empty,
            InstanceName = cluster?.InstanceName ?? string.Empty
        };
    }

    public MapHistoryDto GetMapHistory()
    {
        var enteredClusters = _mainWindowViewModel.EnteredCluster;
        if (enteredClusters == null || enteredClusters.Count == 0)
            return new MapHistoryDto();

        var entries = enteredClusters.Take(200).Select(c =>
        {
            return new MapHistoryEntryDto
            {
                Index = c.Index ?? string.Empty,
                MainClusterIndex = c.MainClusterIndex ?? string.Empty,
                UniqueName = c.UniqueName ?? string.Empty,
                UniqueClusterName = c.UniqueClusterName ?? string.Empty,
                MapDisplayName = c.UniqueClusterName ?? c.UniqueName ?? c.Index ?? string.Empty,
                MapType = c.MapType.ToString(),
                MapTypeString = c.MapTypeString ?? string.Empty,
                ClusterMode = c.ClusterMode.ToString(),
                Tier = c.TierString ?? string.Empty,
                WorldJsonType = c.WorldJsonType ?? string.Empty,
                AvalonTunnelType = c.AvalonTunnelType.ToString(),
                MistsRarity = c.MistsRarity.ToString(),
                EnteredAt = c.Entered.ToString("o"),
                InstanceName = c.InstanceName ?? string.Empty,
                ClusterHistoryString1 = c.ClusterHistoryString1 ?? string.Empty,
                ClusterHistoryString2 = c.ClusterHistoryString2 ?? string.Empty,
                ClusterHistoryString3 = c.ClusterHistoryString3 ?? string.Empty
            };
        }).ToList();

        return new MapHistoryDto { Entries = entries };
    }

    public List<LoggingNotificationDto> GetLoggingNotifications(int count = 100)
    {
        var notifications = _mainWindowViewModel.LoggingBindings?.TrackingNotifications;
        if (notifications == null) return [];

        return notifications.Take(count).Select(n => new LoggingNotificationDto
        {
            Type = n.Type.ToString(),
            DateTime = n.DateTime.ToString("o"),
            FragmentType = n.Fragment?.GetType().Name ?? string.Empty,
            Fragment = MapLoggingFragment(n)
        }).ToList();
    }

    public List<DamageMeterSnapshotDto> GetDamageMeterSnapshots()
    {
        var snapshots = _mainWindowViewModel.DamageMeterBindings?.DamageMeterSnapshots;
        if (snapshots == null) return [];

        return snapshots.Select(s => new DamageMeterSnapshotDto
        {
            Id = s.Timestamp.Ticks.ToString(),
            Timestamp = s.Timestamp.ToString("o"),
            Fragments = s.DamageMeter?.Select(f => new DamageMeterFragmentDto
            {
                Name = f.Name ?? string.Empty,
                CauserGuid = f.CauserGuid.ToString(),
                Damage = f.Damage,
                DamageShortString = f.DamageShortString ?? string.Empty,
                Dps = f.Dps,
                DpsString = f.DpsString ?? string.Empty,
                DamageInPercent = f.DamageInPercent,
                Heal = f.Heal,
                HealShortString = f.HealShortString ?? string.Empty,
                Hps = f.Hps,
                HpsString = f.HpsString ?? string.Empty,
                HealInPercent = f.HealInPercent,
                CombatTime = f.CombatTime.ToString()
            }).ToList() ?? []
        }).ToList();
    }

    // ===== Actions from mobile =====

    public void ToggleDeathAlert(string playerGuid, bool isActive)
    {
        if (!Guid.TryParse(playerGuid, out var guid)) return;
        var player = _mainWindowViewModel.PartyBindings?.Party?.FirstOrDefault(p => p.Guid == guid);
        if (player != null)
        {
            player.IsDeathAlertActive = isActive;
        }
    }

    public void ResetDamageMeter()
    {
        _trackingController.CombatController.ResetDamageMeter();
    }

    public void TakeDamageMeterSnapshot()
    {
        // Invoked via the existing snapshot mechanism
        _mainWindowViewModel.DamageMeterBindings?.GetSnapshot();
    }

    public void ChangeDamageMeterSort(string sortType)
    {
        // The sort is handled by the UI bindings — we trigger a refresh
        // The mobile client will receive updated data via the next DamageMeterUpdate broadcast
    }

    public async Task RemoveDungeonAsync(string dungeonHash)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await _trackingController.DungeonController.RemoveDungeonByHashAsync(new[] { dungeonHash });
        });
    }

    public Task ResetDungeonTrackingAsync()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _trackingController.DungeonController.ResetDungeons();
        });
        return Task.CompletedTask;
    }

    public Task DeleteDungeonsWithZeroFameAsync()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _trackingController.DungeonController.DeleteDungeonsWithZeroFame();
        });
        return Task.CompletedTask;
    }

    public Task DeleteDungeonsFromTodayAsync()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _trackingController.DungeonController.ResetDungeonsByDateAscending(DateTime.UtcNow.Date);
        });
        return Task.CompletedTask;
    }

    public void SetDamageMeterResetOnMapChange(bool active)
    {
        if (_mainWindowViewModel.DamageMeterBindings != null)
        {
            _mainWindowViewModel.DamageMeterBindings.IsDamageMeterResetByMapChangeActive = active;
        }
    }

    public LoggingSettingsDto GetLoggingSettings()
    {
        var lb = _mainWindowViewModel.LoggingBindings;
        if (lb == null) return new LoggingSettingsDto();
        return new LoggingSettingsDto
        {
            IsTrackingSilver = lb.IsTrackingSilver,
            IsTrackingFame = lb.IsTrackingFame,
            IsTrackingMobLoot = lb.IsTrackingMobLoot
        };
    }

    public void SetLoggingSettings(bool isTrackingSilver, bool isTrackingFame, bool isTrackingMobLoot)
    {
        var lb = _mainWindowViewModel.LoggingBindings;
        if (lb == null) return;
        lb.IsTrackingSilver = isTrackingSilver;
        lb.IsTrackingFame = isTrackingFame;
        lb.IsTrackingMobLoot = isTrackingMobLoot;
    }

    public bool IsGatheringActive()
    {
        return _mainWindowViewModel.GatheringBindings?.IsGatheringActive ?? false;
    }

    public void SetGatheringActive(bool active)
    {
        if (_mainWindowViewModel.GatheringBindings != null)
        {
            _mainWindowViewModel.GatheringBindings.IsGatheringActive = active;
        }
    }

    public async Task RemoveGatheringEntriesAsync(IEnumerable<string> guids)
    {
        var guidList = guids.Select(g => Guid.TryParse(g, out var parsed) ? parsed : Guid.Empty).Where(g => g != Guid.Empty);
        if (_mainWindowViewModel.GatheringBindings != null)
        {
            await _mainWindowViewModel.GatheringBindings.RemoveResourcesByIdsAsync(guidList);
        }
    }

    // ===== Mapping Helpers =====

    private static ItemDto MapItem(Models.Item item) => new()
    {
        Index = item.Index,
        UniqueName = item.UniqueName ?? string.Empty,
        LocalizedName = item.LocalizedName ?? string.Empty,
        ImageUrl = $"https://render.albiononline.com/v1/item/{item.UniqueName}.png",
        Tier = item.Tier,
        EnchantmentLevel = item.Level,
        ShopCategory = item.FullItemInformation?.ShopCategory ?? string.Empty,
        ShopSubCategory = item.FullItemInformation?.ShopSubCategory1 ?? string.Empty
    };

    private static DamageMeterFragmentDto MapDamageMeterFragment(DamageMeter.DamageMeterFragment f) => new()
    {
        CauserGuid = f.CauserGuid.ToString(),
        Name = f.Name ?? string.Empty,
        Damage = f.Damage,
        DamageShortString = f.DamageShortString ?? string.Empty,
        Dps = f.Dps,
        DpsString = f.DpsString ?? string.Empty,
        DamageInPercent = f.DamageInPercent,
        DamagePercentage = f.DamagePercentage,
        Heal = f.Heal,
        HealShortString = f.HealShortString ?? string.Empty,
        Hps = f.Hps,
        HpsString = f.HpsString ?? string.Empty,
        HealInPercent = f.HealInPercent,
        HealPercentage = f.HealPercentage,
        Overhealed = f.Overhealed,
        OverhealedPercentageOfTotalHealing = f.OverhealedPercentageOfTotalHealing,
        TakenDamage = f.TakenDamage,
        TakenDamageShortString = f.TakenDamageShortString ?? string.Empty,
        TakenDamageInPercent = f.TakenDamageInPercent,
        TakenDamagePercentage = f.TakenDamagePercentage,
        CombatTime = f.CombatTime.ToString(@"hh\:mm\:ss"),
        CauserMainHand = f.CauserMainHand != null ? MapItem(f.CauserMainHand) : null,
        Spells = f.Spells?.Select(s => new UsedSpellDto
        {
            SpellIndex = s.SpellIndex,
            DamageHealValue = s.DamageHealValue,
            DamageHealShortString = s.DamageHealShortString ?? string.Empty,
            Category = s.Category.ToString()
        }).ToList() ?? []
    };

    private static TradeStatsDto MapTradeStats(Trade.TradeStatsObject ts) => new()
    {
        SoldToday = ts.SoldToday,
        BoughtToday = ts.BoughtToday,
        SalesToday = ts.SalesToday,
        TaxesToday = ts.TaxesToday,
        SoldThisWeek = ts.SoldThisWeek,
        BoughtThisWeek = ts.BoughtThisWeek,
        SalesThisWeek = ts.SalesThisWeek,
        TaxesThisWeek = ts.TaxesThisWeek,
        SoldLastWeek = ts.SoldLastWeek,
        BoughtLastWeek = ts.BoughtLastWeek,
        SalesLastWeek = ts.SalesLastWeek,
        TaxesLastWeek = ts.TaxesLastWeek,
        SoldMonth = ts.SoldMonth,
        BoughtMonth = ts.BoughtMonth,
        SalesMonth = ts.SalesMonth,
        TaxesMonth = ts.TaxesMonth,
        SoldYear = ts.SoldYear,
        BoughtYear = ts.BoughtYear,
        SalesYear = ts.SalesYear,
        TaxesYear = ts.TaxesYear,
        SoldTotal = ts.SoldTotal,
        BoughtTotal = ts.BoughtTotal,
        SalesTotal = ts.SalesTotal,
        TaxesTotal = ts.TaxesTotal
    };

    private static DungeonStatsDto MapDungeonStats(Dungeon.Models.DungeonStats? ds)
    {
        if (ds == null) return new DungeonStatsDto();
        return new DungeonStatsDto
        {
            Solo = MapDungeonStatCategory(ds.StatsSolo),
            Standard = MapDungeonStatCategory(ds.StatsStandard),
            Avalonian = MapDungeonStatCategory(ds.StatsAvalonian),
            Corrupted = MapDungeonStatCategory(ds.StatsCorrupted),
            HellGate = MapDungeonStatCategory(ds.StatsHellGate),
            Expedition = MapDungeonStatCategory(ds.StatsExpedition),
            Mists = MapDungeonStatCategory(ds.StatsMists),
            MistsDungeon = MapDungeonStatCategory(ds.StatsMistsDungeon),
            AbyssalDepths = MapDungeonStatCategory(ds.StatsAbyssalDepths),
            Total = MapDungeonStatCategory(ds.StatsTotal)
        };
    }

    private static DungeonStatCategoryDto MapDungeonStatCategory(dynamic? category)
    {
        if (category == null) return new DungeonStatCategoryDto();
        try
        {
            return new DungeonStatCategoryDto
            {
                EnteredDungeon = (int)(category.Entered),
                Fame = (double)(category.Fame),
                ReSpec = (double)(category.ReSpec),
                Silver = (double)(category.Silver),
                FamePerHour = (double)(category.FamePerHour),
                ReSpecPerHour = (double)(category.ReSpecPerHour),
                SilverPerHour = (double)(category.SilverPerHour),
                TotalValue = (double)(category.LootInSilver)
            };
        }
        catch
        {
            return new DungeonStatCategoryDto();
        }
    }

    internal static LoggingFragmentDto MapLoggingFragment(EventLogging.Notification.TrackingNotification notification)
    {
        var dto = new LoggingFragmentDto();

        try
        {
            var fragment = notification.Fragment;
            if (fragment == null) return dto;

            // Use reflection-safe approach for the various fragment types
            var type = fragment.GetType();

            // Try common properties via reflection
            var descProp = type.GetProperty("Description");
            if (descProp != null) dto = dto with { Description = descProp.GetValue(fragment)?.ToString() ?? string.Empty };

            var lootedByProp = type.GetProperty("LootedByName");
            if (lootedByProp != null) dto = dto with { LootedByName = lootedByProp.GetValue(fragment)?.ToString() ?? string.Empty };

            var lootedFromProp = type.GetProperty("LootedFromName");
            if (lootedFromProp != null) dto = dto with { LootedFromName = lootedFromProp.GetValue(fragment)?.ToString() ?? string.Empty };

            var localizedNameProp = type.GetProperty("LocalizedName");
            if (localizedNameProp != null) dto = dto with { LocalizedName = localizedNameProp.GetValue(fragment)?.ToString() ?? string.Empty };

            var quantityProp = type.GetProperty("Quantity");
            if (quantityProp != null && quantityProp.PropertyType == typeof(int))
                dto = dto with { Quantity = (int)(quantityProp.GetValue(fragment) ?? 0) };

            var diedProp = type.GetProperty("Died");
            if (diedProp != null) dto = dto with { Died = diedProp.GetValue(fragment)?.ToString() ?? string.Empty };

            var killedByProp = type.GetProperty("KilledBy");
            if (killedByProp != null) dto = dto with { KilledBy = killedByProp.GetValue(fragment)?.ToString() ?? string.Empty };
        }
        catch
        {
            // Ignore mapping errors
        }

        return dto;
    }
}
