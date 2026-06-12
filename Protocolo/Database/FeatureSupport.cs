using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Protocolo.Framework.Database
{
    public partial class FeatureSupport
    {
        private static readonly Dictionary<string, FeatureSupport> FeatureList = new Dictionary<string, FeatureSupport>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "sqlserverconnection", new FeatureSupport { Arrays = false } },
            { "npgsqlconnection", new FeatureSupport { Arrays = true } }
        };

        public static FeatureSupport Get(IDbConnection connection)
        {
            string name = connection.GetType().Name;
            FeatureSupport features;
            return FeatureList.TryGetValue(name, out features) ? features : FeatureList.Values.First();
        }

        public bool Arrays { get; set; }
    }

}
