using System;
using System.Collections.Generic;
using System.Data;

namespace Protocolo.Framework.Database
{
    public partial class FeatureSupport
    {
        private static readonly FeatureSupport DefaultFeatures = new FeatureSupport { Arrays = false };
        private static readonly Dictionary<string, FeatureSupport> FeatureList = new Dictionary<string, FeatureSupport>(StringComparer.OrdinalIgnoreCase)
        {
            { "sqlserverconnection", DefaultFeatures },
            { "npgsqlconnection", new FeatureSupport { Arrays = true } }
        };

        public static FeatureSupport Get(IDbConnection connection)
        {
            string name = connection.GetType().Name;
            return FeatureList.TryGetValue(name, out var features) ? features : DefaultFeatures;
        }

        public bool Arrays { get; set; }
    }

}
