using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using UnityEngine;

namespace Arena
{
    [CreateAssetMenu]
    public class ServerGameSettings : ScriptableObject, IServerGameSettings
    {
        [SerializeField]
        int maxConnections = 16;

        [SerializeField] string serverName = "Arena Server";
        [SerializeField] bool useSimulatorPipeline = false;
        [SerializeField] bool useDebugDisconnectTimeout = false;
        [SerializeField] private bool enableDebugJournaling = false;
        [SerializeField] protected uint maxDebugJournalRecordCount = 100000;
        [SerializeField] SimParameters simParameters = new SimParameters();

        public int MaxConnections
        {
            get
            {
                return maxConnections;
            }
        }

        public string ServerName => serverName;
        public bool UseSimulatorPipeline => useSimulatorPipeline;
        public bool UseDebugDisconnectTimeout => useDebugDisconnectTimeout;
        public bool EnableDebugJournaling => enableDebugJournaling;
        public uint MaxDebugJournalRecordCount => maxDebugJournalRecordCount;
        public SimParameters SimParameters => simParameters;
    }
}
