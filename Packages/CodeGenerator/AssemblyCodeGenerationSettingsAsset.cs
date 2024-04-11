using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace TzarGames.CodeGeneration
{
    [CreateAssetMenu(fileName = "Assembly codegen settings.asset", menuName = "Tzar Games/CodeGen/Create assembly codegen settings asset")]
    public class AssemblyCodeGenerationSettingsAsset : CodeGenerationSettingsAsset
    {
        public List<AssemblyDefinitionAsset> PrepareOnlyAssemblies = new List<AssemblyDefinitionAsset>();
        public List<AssemblyDefinitionAsset> Assemblies = new List<AssemblyDefinitionAsset>();
    }
}
