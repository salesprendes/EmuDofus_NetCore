using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Entity;
using Game.Fight;
using System.Collections.Generic;
using System.Linq;

namespace Game.Manager
{
    public sealed class DropManager : Singleton<DropManager>
    {
        public List<ItemDAO> GetDrops(long prospection, MonsterEntity monster, double rate)
        {
            List<ItemDAO> drops = new List<ItemDAO>();
            foreach (var drop in monster.Grade.Template.Drops)
            {
                for (var i = 0; i < drop.Max; i++)
                {
                    if (TryDrop(prospection, drop, rate))
                    {
                        if (drop.ItemTemplate != null)
                        {
                            drops.Add(drop.ItemTemplate.Create(-1, 0));
                        }
                    }
                }
            }
            return drops;
        }

        public bool TryDrop(long prospection, DropTemplateDAO drop, double rate)
        {
            if (drop.PPThreshold > prospection)
                return false;

            var realRate = drop.Rate * rate;
            var chance = Util.Next(0, 100);

            return chance <= realRate;
        }

        public Dictionary<AbstractFighter, List<ItemDAO>> Distribute(IEnumerable<AbstractFighter> fighters, long totalProspection, List<ItemDAO> drops)
        {
            var abstractFighters = fighters as AbstractFighter[] ?? fighters.ToArray();
            var distributed = abstractFighters.ToDictionary(player => player, player => new List<ItemDAO>());

            if (abstractFighters.Length == 0)
                return distributed;

            if (totalProspection > 0)
            {
                var assignedInPass = true;
                while (drops.Count > 0 && assignedInPass)
                {
                    assignedInPass = false;
                    foreach (var player in abstractFighters)
                    {
                        for (int i = drops.Count - 1; i > -1; i--)
                        {
                            var rand = Util.Next(0, 100);
                            var rate = (player.Prospection / (double)totalProspection) * 100;
                            if (rand < rate)
                            {
                                distributed[player].Add(drops[i]);
                                drops.RemoveAt(i);
                                assignedInPass = true;
                            }
                        }
                    }
                }
            }

            // Reparto determinista de lo que quede (prospecciones a cero o pasada sin
            // asignaciones): garantiza que el bucle termina y no se pierde botín.
            if (drops.Count > 0)
            {
                var byProspection = abstractFighters.OrderByDescending(player => player.Prospection).ToArray();
                var index = 0;
                for (int i = drops.Count - 1; i > -1; i--)
                {
                    distributed[byProspection[index % byProspection.Length]].Add(drops[i]);
                    drops.RemoveAt(i);
                    index++;
                }
            }

            return distributed;
        }
    }
}


