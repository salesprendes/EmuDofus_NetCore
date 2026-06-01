using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Protocolo.Framework.Database
{
    public partial class FeatureSupport
    {
        /// <summary>
        /// Dictionary of supported features indexed by connection type name
        /// </summary>
        private static readonly Dictionary<string, FeatureSupport> FeatureList = new Dictionary<string, FeatureSupport>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "sqlserverconnection", new FeatureSupport { Arrays = false } },
            { "npgsqlconnection", new FeatureSupport { Arrays = true } }
        };

        /// <summary>
        /// Gets the featureset based on the passed connection
        /// </summary>
        public static FeatureSupport Get(IDbConnection connection)
        {
            string name = connection.GetType().Name;
            FeatureSupport features;
            return FeatureList.TryGetValue(name, out features) ? features : FeatureList.Values.First();
        }

        /// <summary>
        /// True if the db supports array columns e.g. Postgresql
        /// </summary>
        public bool Arrays { get; set; }
    }

    /// <summary>
    /// Represents simple member map for one of target parameter or property or field to source DataReader column
    /// </summary>
}
