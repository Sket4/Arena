using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace DGX.SRP.Editor.ShaderGraph
{
    [GenerateBlocks]
    internal struct FullScreenBlocks
    {
        // TODO: use base color and alpha blocks
        public static BlockFieldDescriptor colorBlock = new BlockFieldDescriptor(String.Empty, "Color", "Color",
                new ColorRGBAControl(UnityEngine.Color.white), ShaderStage.Fragment);
    }

    sealed class DgxTarget : Target
    {
        public DgxTarget()
        {
            displayName = "DGX";
            isHidden = false;
        }
        
        public override bool IsActive()
        {
            return GraphicsSettings.currentRenderPipeline is RenderPipelineAsset;
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddSubShader(Unlit("Opaque", "Geometry"));
        }
        
        public static SubShaderDescriptor Unlit(string renderType, string renderQueue)
        {
            var result = new SubShaderDescriptor()
            {
                //pipelineTag = UniversalTarget.kPipelineTag,
                customTags = "",
                renderType = renderType,
                renderQueue = renderQueue,
                generatesPreview = true,
                passes = new PassCollection()
            };

            result.passes.Add(UnlitPass());
            //
            // if (target.mayWriteDepth)
            //     result.passes.Add(CorePasses.DepthOnly(target));
            //
            // result.passes.Add(CorePasses.ShadowCaster(target));
            // result.passes.Add(CorePasses.SceneSelection(target));
            // result.passes.Add(CorePasses.ScenePicking(target));

            return result;
        }
        
        public static PassDescriptor UnlitPass()
        {
            var result = new PassDescriptor
            {
                // Definition
                displayName = "Pass",
                referenceName = "SHADERPASS_UNLIT",
                lightMode = "SRPDefaultUnlit",
                useInPreview = true,

                // Template
                passTemplatePath = "Packages/com.dgx.srp/Editor/Templates/ShaderPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(
                    new [] { "Packages/com.dgx.srp/Editor/Templates" }).ToArray(),

                // Port Mask
                validVertexBlocks = Vertex,
                validPixelBlocks = FragmentColorAlpha,

                // Fields
                structs = DefaultStructs,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = DefaultRenderStates(),
                pragmas = Pragmas,
                defines = new DefineCollection(),
                keywords = new KeywordCollection(),
                includes = Includes,

                // Custom Interpolator Support
                customInterpolators = CusromInterpolators
            };
            //CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
            return result;
        }
        public static readonly CustomInterpSubGen.Collection CusromInterpolators = new()
        {
            // Custom interpolators are not explicitly defined in the SurfaceDescriptionInputs template.
            // This entry point will let us generate a block of pass-through assignments for each field.
            //CustomInterpSubGen.Descriptor.MakeBlock(CustomInterpSubGen.Splice.k_spliceCopyToSDI, "output", "input"),

            // sgci_PassThroughFunc is called from BuildVaryings in Varyings.hlsl to copy custom interpolators from vertex descriptions.
            // this entry point allows for the function to be defined before it is used.
            //CustomInterpSubGen.Descriptor.MakeFunc(CustomInterpSubGen.Splice.k_splicePreSurface, "CustomInterpolatorPassThroughFunc", "Varyings", "VertexDescription", "CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC", "FEATURES_GRAPH_VERTEX")
        };
        public static IncludeCollection Includes = new()
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.dgx.srp/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.dgx.srp/ShaderLibrary/Graph/GraphInput.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.dgx.srp/ShaderLibrary/Graph/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.dgx.srp/ShaderLibrary/Graph/GraphPass.hlsl", IncludeLocation.Postgraph },
            
        };
        public static readonly PragmaCollection Pragmas = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.MultiCompileInstancing },
            //{ Pragma.MultiCompileFog },
            //{ Pragma.MultiCompileForwardBase },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
            { Pragma.DOTSInstancing },
        };
        public static RenderStateCollection DefaultRenderStates()
        {
            var result = new RenderStateCollection();
            result.Add(RenderState.ZTest("LEqual"));
            result.Add(RenderState.ZWrite("On"));
            result.Add(RenderState.Cull("Back"));
            
            // AddUberSwitchedBlend(target, result);
            // if (target.surfaceType != SurfaceType.Opaque)
            //     result.Add(RenderState.ColorMask("ColorMask RGB"));
            
            return result;
        }
        public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
        {
            BlockFields.VertexDescription.Position,
            BlockFields.VertexDescription.Normal,
            BlockFields.VertexDescription.Tangent,
        };
        public static readonly BlockFieldDescriptor[] FragmentColorAlpha = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Alpha,
            //BlockFields.SurfaceDescription.AlphaClipThreshold,
        };
        public static readonly StructCollection DefaultStructs = new StructCollection
        {
            { Structs.Attributes },
            //{ BuiltInStructs.Varyings },
            { Structs.SurfaceDescriptionInputs },
            { Structs.VertexDescriptionInputs },
        };

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            
            // Core fields
            context.AddField(Fields.GraphVertex, descs.Contains(BlockFields.VertexDescription.Position) ||
                                                 descs.Contains(BlockFields.VertexDescription.Normal) ||
                                                 descs.Contains(BlockFields.VertexDescription.Tangent));
            context.AddField(Fields.GraphPixel);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);
            
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            //
        }

        public override bool WorksWithSRP(UnityEngine.Rendering.RenderPipelineAsset scriptableRenderPipeline)
        {
            if (scriptableRenderPipeline is RenderPipelineAsset)
            {
                return true;
            }
            return false;
        }
    }
}
