namespace StatisticsAnalysisTool.MobileServer.Dtos;

// ==================== Dashboard DTOs ====================

public record DashboardDto
{
    public double FamePerHour { get; init; }
    public double SilverPerHour { get; init; }
    public double ReSpecPointsPerHour { get; init; }
    public double MightPerHour { get; init; }
    public double FavorPerHour { get; init; }
    public double SilverCostForReSpecHour { get; init; }

    public double TotalGainedFameInSession { get; init; }
    public double TotalGainedSilverInSession { get; init; }
    public double TotalGainedReSpecPointsInSession { get; init; }
    public double TotalGainedMightInSession { get; init; }
    public double TotalGainedFavorInSession { get; init; }
    public double TotalGainedSilverCostForReSpecInSession { get; init; }

    public double FameInPercent { get; init; }
    public double SilverInPercent { get; init; }
    public double ReSpecPointsInPercent { get; init; }
    public double MightInPercent { get; init; }
    public double FavorInPercent { get; init; }

    public int KillsToday { get; init; }
    public int KillsThisWeek { get; init; }
    public int KillsThisMonth { get; init; }
    public int DeathsToday { get; init; }
    public int DeathsThisWeek { get; init; }
    public int DeathsThisMonth { get; init; }
    public int SoloKillsToday { get; init; }
    public int SoloKillsThisWeek { get; init; }
    public int SoloKillsThisMonth { get; init; }

    public double AverageItemPowerWhenKilling { get; init; }
    public double AverageItemPowerOfTheKilledEnemies { get; init; }
    public double AverageItemPowerWhenDying { get; init; }

    public long RepairCostsToday { get; init; }
    public long RepairCostsLast7Days { get; init; }
    public long RepairCostsLast30Days { get; init; }

    public LootedChestsDto LootedChests { get; init; } = new();
    public List<FactionPointStatDto> FactionPointStats { get; init; } = [];
}

public record LootedChestsDto
{
    public int OpenedCommon { get; init; }
    public int OpenedUncommon { get; init; }
    public int OpenedRare { get; init; }
    public int OpenedLegendary { get; init; }
}

public record FactionPointStatDto
{
    public string CityFaction { get; init; } = string.Empty;
    public double Value { get; init; }
    public double ValuePerHour { get; init; }
}

// ==================== Damage Meter DTOs ====================

public record DamageMeterDto
{
    public List<DamageMeterFragmentDto> Fragments { get; init; } = [];
    public string SortType { get; init; } = "Damage";
    public bool IsDamageMeterResetByMapChangeActive { get; init; }
    public bool IsDamageMeterResetBeforeCombatActive { get; init; }
}

public record DamageMeterFragmentDto
{
    public string CauserGuid { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public long Damage { get; init; }
    public string DamageShortString { get; init; } = string.Empty;
    public double Dps { get; init; }
    public string DpsString { get; init; } = string.Empty;
    public double DamageInPercent { get; init; }
    public double DamagePercentage { get; init; }
    public long Heal { get; init; }
    public string HealShortString { get; init; } = string.Empty;
    public double Hps { get; init; }
    public string HpsString { get; init; } = string.Empty;
    public double HealInPercent { get; init; }
    public double HealPercentage { get; init; }
    public double Overhealed { get; init; }
    public double OverhealedPercentageOfTotalHealing { get; init; }
    public long TakenDamage { get; init; }
    public string TakenDamageShortString { get; init; } = string.Empty;
    public double TakenDamageInPercent { get; init; }
    public double TakenDamagePercentage { get; init; }
    public string CombatTime { get; init; } = string.Empty;
    public ItemDto? CauserMainHand { get; init; }
    public List<UsedSpellDto> Spells { get; init; } = [];
}

public record UsedSpellDto
{
    public int SpellIndex { get; init; }
    public string UniqueName { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public long DamageHealValue { get; init; }
    public string DamageHealShortString { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
}

public record DamageMeterSnapshotDto
{
    public string Id { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;
    public List<DamageMeterFragmentDto> Fragments { get; init; } = [];
}

// ==================== Dungeon DTOs ====================

public record DungeonListDto
{
    public List<DungeonFragmentDto> Dungeons { get; init; } = [];
    public DungeonStatsDto Stats { get; init; } = new();
}

public record DungeonFragmentDto
{
    public string DungeonHash { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public string MapType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public int Level { get; init; } = -1;
    public string Faction { get; init; } = string.Empty;
    public string CityFaction { get; init; } = string.Empty;
    public string EnterDungeonFirstTime { get; init; } = string.Empty;
    public double Fame { get; init; }
    public double Silver { get; init; }
    public double ReSpec { get; init; }
    public double Might { get; init; }
    public double Favor { get; init; }
    public double FactionCoins { get; init; }
    public double FactionFlags { get; init; }
    public double FamePerHour { get; init; }
    public double SilverPerHour { get; init; }
    public double ReSpecPerHour { get; init; }
    public int TotalRunTimeInSeconds { get; init; }
    public int NumberOfFloors { get; init; }
    public double TotalValue { get; init; }
    public string MainMapIndex { get; init; } = string.Empty;
    public string MainMapName { get; init; } = string.Empty;
    public string KilledBy { get; init; } = string.Empty;
    public string DiedName { get; init; } = string.Empty;
    public string KillStatus { get; init; } = string.Empty;
    public List<DungeonLootDto> Loot { get; init; } = [];
    public List<DungeonEventDto> Events { get; init; } = [];
    public DungeonLootDto? MostValuableLoot { get; init; }
}

public record DungeonLootDto
{
    public string UniqueName { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public double EstimatedMarketValue { get; init; }
    public string LootedByName { get; init; } = string.Empty;
    public string LootedFromName { get; init; } = string.Empty;
    public bool IsTrash { get; init; }
}

public record DungeonEventDto
{
    public int Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string UniqueName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsBossChest { get; init; }
    public string Rarity { get; init; } = string.Empty;
    public string ShrineType { get; init; } = string.Empty;
    public string ShrineBuff { get; init; } = string.Empty;
}

public record DungeonStatsDto
{
    public DungeonStatCategoryDto Solo { get; init; } = new();
    public DungeonStatCategoryDto Standard { get; init; } = new();
    public DungeonStatCategoryDto Avalonian { get; init; } = new();
    public DungeonStatCategoryDto Corrupted { get; init; } = new();
    public DungeonStatCategoryDto HellGate { get; init; } = new();
    public DungeonStatCategoryDto Expedition { get; init; } = new();
    public DungeonStatCategoryDto Mists { get; init; } = new();
    public DungeonStatCategoryDto MistsDungeon { get; init; } = new();
    public DungeonStatCategoryDto AbyssalDepths { get; init; } = new();
    public DungeonStatCategoryDto Total { get; init; } = new();
}

public record DungeonStatCategoryDto
{
    public int EnteredDungeon { get; init; }
    public double Fame { get; init; }
    public double ReSpec { get; init; }
    public double Silver { get; init; }
    public double FamePerHour { get; init; }
    public double ReSpecPerHour { get; init; }
    public double SilverPerHour { get; init; }
    public int BestTime { get; init; }
    public double BestFame { get; init; }
    public double BestReSpec { get; init; }
    public double BestSilver { get; init; }
    public double BestFamePerHour { get; init; }
    public double BestReSpecPerHour { get; init; }
    public double BestSilverPerHour { get; init; }
    public double TotalValue { get; init; }
}

// ==================== Trade DTOs ====================

public record TradeListDto
{
    public List<TradeDto> Trades { get; init; } = [];
    public TradeStatsDto Stats { get; init; } = new();
}

public record TradeDto
{
    public long Id { get; init; }
    public string Timestamp { get; init; } = string.Empty;
    public string ClusterIndex { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public ItemDto? Item { get; init; }
    public int Quantity { get; init; }
    public double TotalPrice { get; init; }
    public double UnitPrice { get; init; }
    public double TaxRate { get; init; }
    public double TaxSetupRate { get; init; }
    public double TaxAmount { get; init; }
    public double TaxSetupAmount { get; init; }
    public double TotalRevenue { get; init; }
    public double DistanceFee { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record TradeStatsDto
{
    public long SoldToday { get; init; }
    public long BoughtToday { get; init; }
    public long SalesToday { get; init; }
    public long TaxesToday { get; init; }

    public long SoldThisWeek { get; init; }
    public long BoughtThisWeek { get; init; }
    public long SalesThisWeek { get; init; }
    public long TaxesThisWeek { get; init; }

    public long SoldLastWeek { get; init; }
    public long BoughtLastWeek { get; init; }
    public long SalesLastWeek { get; init; }
    public long TaxesLastWeek { get; init; }

    public long SoldMonth { get; init; }
    public long BoughtMonth { get; init; }
    public long SalesMonth { get; init; }
    public long TaxesMonth { get; init; }

    public long SoldYear { get; init; }
    public long BoughtYear { get; init; }
    public long SalesYear { get; init; }
    public long TaxesYear { get; init; }

    public long SoldTotal { get; init; }
    public long BoughtTotal { get; init; }
    public long SalesTotal { get; init; }
    public long TaxesTotal { get; init; }
}

// ==================== Gathering DTOs ====================

public record GatheringListDto
{
    public List<GatheredDto> GatheredItems { get; init; } = [];
    public GatheringStatsDto Stats { get; init; } = new();
}

public record GatheredDto
{
    public string Guid { get; init; } = string.Empty;
    public string UniqueName { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public int GainedStandardAmount { get; init; }
    public int GainedBonusAmount { get; init; }
    public int GainedPremiumBonusAmount { get; init; }
    public int GainedTotalAmount { get; init; }
    public int GainedFame { get; init; }
    public int MiningProcesses { get; init; }
    public double EstimatedMarketValue { get; init; }
    public long TotalMarketValue { get; init; }
    public string ClusterIndex { get; init; } = string.Empty;
    public string MapType { get; init; } = string.Empty;
    public string MapDisplayName { get; init; } = string.Empty;
    public string ClusterMode { get; init; } = string.Empty;
    public string TimestampUtc { get; init; } = string.Empty;
    public bool HasBeenFished { get; init; }
}

public record GatheringStatsDto
{
    public int TotalMiningProcesses { get; init; }
    public int TotalResources { get; init; }
    public long TotalGainedSilver { get; init; }
    public long GainedSilverByWood { get; init; }
    public long GainedSilverByHide { get; init; }
    public long GainedSilverByOre { get; init; }
    public long GainedSilverByRock { get; init; }
    public long GainedSilverByFiber { get; init; }
    public long GainedSilverByFish { get; init; }
    public int WoodCount { get; init; }
    public int HideCount { get; init; }
    public int OreCount { get; init; }
    public int RockCount { get; init; }
    public int FiberCount { get; init; }
    public int FishCount { get; init; }
}

// ==================== Party DTOs ====================

public record PartyDto
{
    public List<PartyPlayerDto> Players { get; init; } = [];
    public double AveragePartyIp { get; init; }
    public double AveragePartyBasicIp { get; init; }
    public double MinimalItemPower { get; init; }
    public double MaximumItemPower { get; init; }
}

public record PartyPlayerDto
{
    public string Guid { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public bool IsLocalPlayer { get; init; }
    public bool IsPlayerInspected { get; init; }
    public double AverageItemPower { get; init; }
    public double AverageBasicItemPower { get; init; }
    public string ItemPowerCondition { get; init; } = string.Empty;
    public string BasicItemPowerCondition { get; init; } = string.Empty;
    public bool IsDeathAlertActive { get; init; }
    public EquipmentDto Equipment { get; init; } = new();
}

public record EquipmentDto
{
    public ItemDto? MainHand { get; init; }
    public ItemDto? OffHand { get; init; }
    public ItemDto? Head { get; init; }
    public ItemDto? Chest { get; init; }
    public ItemDto? Shoes { get; init; }
    public ItemDto? Bag { get; init; }
    public ItemDto? Cape { get; init; }
    public ItemDto? Mount { get; init; }
    public ItemDto? Potion { get; init; }
    public ItemDto? BuffFood { get; init; }
}

// ==================== Guild DTOs ====================

public record GuildDto
{
    public List<SiphonedEnergyItemDto> SiphonedEnergyList { get; init; } = [];
    public List<SiphonedEnergyOverviewDto> SiphonedEnergyOverview { get; init; } = [];
    public long TotalSiphonedEnergyQuantity { get; init; }
    public string SiphonedEnergyLastUpdate { get; init; } = string.Empty;
}

public record SiphonedEnergyItemDto
{
    public string GuildName { get; init; } = string.Empty;
    public string CharacterName { get; init; } = string.Empty;
    public long Quantity { get; init; }
    public string Timestamp { get; init; } = string.Empty;
    public bool IsDeposit { get; init; }
}

public record SiphonedEnergyOverviewDto
{
    public string CharacterName { get; init; } = string.Empty;
    public long TotalQuantity { get; init; }
}

// ==================== Logging DTOs ====================

public record LoggingNotificationDto
{
    public string Type { get; init; } = string.Empty;
    public string DateTime { get; init; } = string.Empty;
    public string FragmentType { get; init; } = string.Empty;
    public LoggingFragmentDto Fragment { get; init; } = new();
}

public record LoggingFragmentDto
{
    // Common
    public string Description { get; init; } = string.Empty;

    // Loot fragment
    public string LootedByName { get; init; } = string.Empty;
    public string LootedFromName { get; init; } = string.Empty;
    public string LocalizedName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public double EstimatedMarketValue { get; init; }

    // Fame/Silver fragment
    public double TotalPlayerFame { get; init; }
    public double GainedFame { get; init; }
    public double TotalPlayerSilver { get; init; }
    public double GainedSilver { get; init; }

    // Kill fragment
    public string Died { get; init; } = string.Empty;
    public string KilledBy { get; init; } = string.Empty;
}

// ==================== Vault / Storage DTOs ====================

public record VaultDto
{
    public List<VaultContainerDto> Containers { get; init; } = [];
}

public record VaultContainerDto
{
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string LastUpdate { get; init; } = string.Empty;
    public List<VaultItemDto> Items { get; init; } = [];
}

public record VaultItemDto
{
    public string UniqueName { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public double EstimatedMarketValue { get; init; }
}

// ==================== Player Info DTOs ====================

public record PlayerInfoDto
{
    public string Username { get; init; } = string.Empty;
    public string Guild { get; init; } = string.Empty;
    public string Alliance { get; init; } = string.Empty;
    public string CurrentMap { get; init; } = string.Empty;
    public string CurrentMapType { get; init; } = string.Empty;
    public string MapDisplayName { get; init; } = string.Empty;
    public string ClusterMode { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public string MapTypeString { get; init; } = string.Empty;
    public string InstanceName { get; init; } = string.Empty;
}

// ==================== Common DTOs ====================

public record ItemDto
{
    public int Index { get; init; }
    public string UniqueName { get; init; } = string.Empty;
    public string LocalizedName { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public int Tier { get; init; }
    public int EnchantmentLevel { get; init; }
    public string ShopCategory { get; init; } = string.Empty;
    public string ShopSubCategory { get; init; } = string.Empty;
}

// ==================== Connection / Status DTOs ====================

public record ServerStatusDto
{
    public bool IsTrackingActive { get; init; }
    public string ServerVersion { get; init; } = string.Empty;
    public string PlayerName { get; init; } = string.Empty;
    public int ConnectedClients { get; init; }
    public string CurrentCluster { get; init; } = string.Empty;
}

public record ClusterChangedDto
{
    public string Index { get; init; } = string.Empty;
    public string MainClusterIndex { get; init; } = string.Empty;
    public string UniqueName { get; init; } = string.Empty;
    public string UniqueClusterName { get; init; } = string.Empty;
    public string MapType { get; init; } = string.Empty;
    public string MapTypeString { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public string ClusterMode { get; init; } = string.Empty;
    public string WorldJsonType { get; init; } = string.Empty;
    public string AvalonTunnelType { get; init; } = string.Empty;
    public string MistsRarity { get; init; } = string.Empty;
    public string InstanceName { get; init; } = string.Empty;
    public string MapDisplayName { get; init; } = string.Empty;
    public string ClusterHistoryString1 { get; init; } = string.Empty;
    public string ClusterHistoryString2 { get; init; } = string.Empty;
    public string ClusterHistoryString3 { get; init; } = string.Empty;
}

// ==================== Map History DTOs ====================

public record MapHistoryDto
{
    public List<MapHistoryEntryDto> Entries { get; init; } = [];
}

public record MapHistoryEntryDto
{
    public string Index { get; init; } = string.Empty;
    public string MainClusterIndex { get; init; } = string.Empty;
    public string UniqueName { get; init; } = string.Empty;
    public string UniqueClusterName { get; init; } = string.Empty;
    public string MapDisplayName { get; init; } = string.Empty;
    public string MapType { get; init; } = string.Empty;
    public string MapTypeString { get; init; } = string.Empty;
    public string ClusterMode { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public string WorldJsonType { get; init; } = string.Empty;
    public string AvalonTunnelType { get; init; } = string.Empty;
    public string MistsRarity { get; init; } = string.Empty;
    public string EnteredAt { get; init; } = string.Empty;
    public string InstanceName { get; init; } = string.Empty;
    public string ClusterHistoryString1 { get; init; } = string.Empty;
    public string ClusterHistoryString2 { get; init; } = string.Empty;
    public string ClusterHistoryString3 { get; init; } = string.Empty;
}

// ==================== Logging Settings DTO ====================

public record LoggingSettingsDto
{
    public bool IsTrackingSilver { get; init; }
    public bool IsTrackingFame { get; init; }
    public bool IsTrackingMobLoot { get; init; }
}
