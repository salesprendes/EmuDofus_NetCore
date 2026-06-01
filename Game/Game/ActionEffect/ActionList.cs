using Game.Spell;
using Protocolo.Framework.Generic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.ActionEffect
{
    public sealed class ActionEntry
    {
        public EffectEnum Effect { get; }
        public Dictionary<string, string> Parameters { get; }
        public ActionEntry(EffectEnum effect, Dictionary<string, string> parameters)
        {
            Effect = effect;
            Parameters = parameters;
        }
        public static ActionEntry Deserialize(string data)
        {
            var splitted = data.Split(':');
            var effect = (EffectEnum)int.Parse(splitted[0]);
            var parameters = new Dictionary<string, string>();
            foreach (var parameter in splitted[1].Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var splittedParam = parameter.Split('=');
                var key = splittedParam[0];
                var value = splittedParam[1];
                parameters.Add(key, value);
            }
            return new ActionEntry(effect, parameters);
        }
    }

    public sealed class ActionList : List<ActionEntry>
    {
        private static ILogger Logger = LogManager.GetLogger(typeof(ActionList));

        public static ActionList Deserialize(string data)
        {
            var list = new ActionList();
            try
            {
                list.AddRange(data.Split('|').Select(ActionEntry.Deserialize));
            }
            catch(Exception e)
            {
                Logger.Error("ActionList::Deserialize failed, check the script syntax, data=" + data, e);
            }
            return list;
        }
    }
}


