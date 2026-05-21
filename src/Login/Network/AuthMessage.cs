using Login.Database.Structure;
using Login.Frames;
using System.Collections.Generic;
using System.Linq;

namespace Login.Network
{
    public sealed class AuthMessage
    {
        public static string HELLO_CONNECT(string key) => "HC" + key;
        public static string PROTOCOL_REQUIRED() => "AlEv" + VersionFrame.ClientVersion;
        public static string AUTH_FAILED_CREDENTIALS() => "AlEf";
        public static string AUTH_FAILED_BANNED() => "AlEb";
        public static string AUTH_FAILED_ALREADY_CONNECTED() => "AlEc";
        public static string SERVER_BUSY() => "AlEw";
        public static string AUTH_QUEUE_POSITION(int position, int totalAbo, int totalNonAbo) =>  "Af" + position + "|" + totalAbo + "|" + totalNonAbo + "|2|-1";
        public static string ACCOUNT_PSEUDO(string pseudo) => "Ad" + pseudo;
        public static string ACCOUNT_COMMUNITY => "Ac0";
        public static string ACCOUNT_RIGHT(int right) => "AlK" + (right > 0 ? 1 : 0);
        public static string ACCOUNT_SECRET_ANSWER(string answer) => "AQ" + answer;
        public static string ACCOUNT_KICK_TIMEOUT => "M01";
        public static string WORLD_HOST_LIST(IEnumerable<GameServerDAO> servers) => "AH" + string.Join("|", (servers ?? Enumerable.Empty<GameServerDAO>()).OrderBy(server => server.Id).Select(server => $"{server.Id};{server.State};{server.Sub};{server.FreePlaces}"));
        public static string WORLD_CHARACTER_LIST(IReadOnlyDictionary<int, int> countsByServer) => "AxK31536000000" + string.Concat(countsByServer.OrderBy(p => p.Key).Select(p => $"|{p.Key},{p.Value}"));
        public static string WORLD_SELECTION_FAILED() => "AXEd";
        public static string WORLD_SELECTION_SUCCESS(string ip, int port, string ticket) => "AYK" + ip + ":" + port + ";" + ticket;
    }
}
