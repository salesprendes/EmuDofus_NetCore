using Game.Database.Structure;

namespace Game.Fight.AI
{
    public enum BreedCombatStyle
    {
        Melee,
        Mixed,
        Ranged,
    }
    
    public static class AIBreedProfile
    {
        public static BreedCombatStyle GetCombatStyle(CharacterBreedEnum breed)
        {
            switch (breed)
            {
                case CharacterBreedEnum.BREED_IOP:
                case CharacterBreedEnum.BREED_SACRIEUR:
                case CharacterBreedEnum.BREED_PANDAWA:
                    return BreedCombatStyle.Melee;

                case CharacterBreedEnum.BREED_FECA:
                case CharacterBreedEnum.BREED_SRAM:
                case CharacterBreedEnum.BREED_ECAFLIP:
                    return BreedCombatStyle.Mixed;

                default: //OSAMODAS, ENUTROF, XELOR, ENIRIPSA, CRA, SADIDAS
                return BreedCombatStyle.Ranged;
            }
        }

        public static bool IsRanged(CharacterBreedEnum breed)
            => GetCombatStyle(breed) == BreedCombatStyle.Ranged;

        public static bool IsMelee(CharacterBreedEnum breed)
            => GetCombatStyle(breed) == BreedCombatStyle.Melee;
    }
}
