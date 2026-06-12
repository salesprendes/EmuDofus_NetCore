using System;
using System.Data;

namespace Protocolo.Framework.Database
{
    public sealed partial class DbString : SqlMapper.ICustomQueryParameter
    {
        public DbString() { Length = -1; }
        public bool IsAnsi { get; set; }
        public bool IsFixedLength { get; set; }
        public int Length { get; set; }
        public string Value { get; set; }
        public void AddParameter(IDbCommand command, string name)
        {
            if (IsFixedLength && Length == -1)
            {
                throw new InvalidOperationException("If specifying IsFixedLength, a Length must also be specified");
            }

            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = (object)Value ?? DBNull.Value;
            if (Length == -1 && Value != null && Value.Length <= 4000)
            {
                param.Size = 4000;
            }
            else
            {
                param.Size = Length;
            }
            param.DbType = IsAnsi ? (IsFixedLength ? DbType.AnsiStringFixedLength : DbType.AnsiString) : (IsFixedLength ? DbType.StringFixedLength : DbType.String);
            command.Parameters.Add(param);
        }
    }

}
