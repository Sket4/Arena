using UnityEngine;

namespace TzarGames.CodeGeneration
{
    //[CreateAssetMenu(fileName = "Codegen settings.asset", menuName = "Tzar Games/CodeGen/Create codegen settings asset")]
    public class CodeGenerationSettingsAsset : ScriptableObject
    {
        [SerializeField] string relativeSavePath;

        public string RelativeSavePath { get => relativeSavePath; set => relativeSavePath = value; }
        public string FullSavePath => Application.dataPath + RelativeSavePath;
    }
}
