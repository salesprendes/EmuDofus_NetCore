using Protocolo.Framework.Generic;
using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Spell
{
    public sealed class SpellBookFactory : Singleton<SpellBookFactory>
    {
        public SpellBook Create(AbstractEntity entity)
        {
            switch (entity.Type)
            {
                case EntityTypeEnum.TYPE_CHARACTER:
                case EntityTypeEnum.TYPE_TAX_COLLECTOR:
                    return new SpellBook((int)entity.Type, entity.Id);

                case EntityTypeEnum.TYPE_MONSTER_FIGHTER:
                {
                    var grade = ((MonsterEntity)entity).Grade;
                    // Encode (MonsterId, GradeNumber) into the long so SpellBook can look up
                    // monstruos_hechizos by both columns — Grade.Id (PK) is unrelated to them.
                    long monsterKey = ((long)grade.MonsterId << 32) | (uint)grade.Grade;
                    return new SpellBook((int)entity.Type, monsterKey);
                }
            }

            return null;
        }
    }
}


