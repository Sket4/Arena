using TzarGames.GameCore;
using TzarGames.GameFramework;
using TzarGames.MatchFramework;
using UnityEngine;

namespace Arena.Client
{
    [CreateAssetMenu]
    public class ClientGameSettings : ScriptableObject
    {
        public GameObject CameraPrefab;
        public GameObject GameUiPrefab;
        public SurfaceSoundsLibrary SurfaceSoundsLibrary;
        public Instantiator InstantiatorPrefab;
        public bool UseNetConnectionSimulator;
        public SimParameters NetSimulatorSettings;
        public bool EnableDebugJournaling;
        public uint MaxDebugJournalRecordCount = 100000;

        public static ClientGameSettings Get
        {
            get
            {
                return Resources.FindObjectsOfTypeAll<ClientGameSettings>()[0];
            }
        }
    }
}
