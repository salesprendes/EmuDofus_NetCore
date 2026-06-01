using Protocolo.Framework.Database;
using Game.Database.Repository;
using Game.Database.Structure;

namespace Game.Database
{
    public sealed class WorldDbMgr : DbManager<WorldDbMgr>
    {
        public void Initialize(string dbConnection = "")
        {
            SqlMapperExtensions.SetTableName(typeof(CharacterDAO), WorldConfig.AUTH_DB_NAME + ".characterinstance");
            SqlMapperExtensions.SetTableName(typeof(GameServerDAO), WorldConfig.AUTH_DB_NAME + ".gameservers");

            AddRepository(GameServerRepository.Instance);
            AddRepository(ExperienceTemplateRepository.Instance);
            AddRepository(ItemSetRepository.Instance);
            AddRepository(ItemTemplateRepository.Instance);
            AddRepository(CraftEntryRepository.Instance);
            AddRepository(InventoryItemRepository.Instance);
            AddRepository(SpellRepository.Instance);
            AddRepository(SpellBookEntryRepository.Instance);
            AddRepository(GuildRepository.Instance);
            AddRepository(TaxCollectorRepository.Instance);
            AddRepository(PaddockRepository.Instance);
            AddRepository(MountTemplateRepository.Instance);
            AddRepository(MountRepository.Instance);
            AddRepository(CharacterGuildRepository.Instance);
            AddRepository(CharacterJobRepository.Instance);
            AddRepository(CharacterRepository.Instance);
            AddRepository(CharacterQuestRepository.Instance);
            AddRepository(SocialRelationRepository.Instance);
            AddRepository(BankRepository.Instance);
            AddRepository(MapTriggerRepository.Instance);
            AddRepository(MapTemplateRepository.Instance);
            AddRepository(NpcTemplateRepository.Instance);
            AddRepository(NpcInstanceRepository.Instance);
            AddRepository(NpcQuestionRepository.Instance);
            AddRepository(NpcResponseRepository.Instance);
            AddRepository(MonsterSpawnRepository.Instance);
            AddRepository(MonsterSuperRaceRepository.Instance);
            AddRepository(MonsterRaceRepository.Instance);
            AddRepository(MonsterRepository.Instance);
            AddRepository(MonsterGradeRepository.Instance);
            AddRepository(MonsterSpellRepository.Instance);
            AddRepository(DropTemplateRepository.Instance);
            AddRepository(AuctionHouseRepository.Instance);
            AddRepository(AuctionHouseEntryRepository.Instance);
            AddRepository(AuctionHouseAllowedTypeRepository.Instance);
            AddRepository(SubAreaRepository.Instance);
            AddRepository(AreaRepository.Instance);
            AddRepository(SuperAreaRepository.Instance);
            AddRepository(FightActionRepository.Instance);
            AddRepository(QuestRepository.Instance);
            AddRepository(QuestStepRepository.Instance);
            AddRepository(QuestObjectiveRepository.Instance);
            AddRepository(ConquestTerritoryRepository.Instance);
            AddRepository(AccountGiftRepository.Instance);

            LoadAll(string.IsNullOrWhiteSpace(dbConnection) ? WorldConfig.WORLD_DB_CONNECTION : dbConnection);
        }
    }
}

