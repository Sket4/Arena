using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace DGX.SRP.Editor.ShaderGraph
{
    enum SurfaceType
    {
        Opaque,
        Transparent
    }
    enum AlphaMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply,
    }
    
    [GenerateBlocks]
    internal struct FullScreenBlocks
    {
        // TODO: use base color and alpha blocks
        public static BlockFieldDescriptor colorBlock = new BlockFieldDescriptor(String.Empty, "Color", "Color",
                new ColorRGBAControl(UnityEngine.Color.white), ShaderStage.Fragment);
    }

    sealed class DgxTarget : Target
    {
        [SerializeField] private ZWrite ZWriteMode = ZWrite.On;
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Opaque;
        [SerializeField] private AlphaMode alphaMode = AlphaMode.Alpha;
        
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
            var renderQueue = surfaceType == SurfaceType.Opaque ? "Geometry" : "Transparent";
            var renderType = surfaceType == SurfaceType.Opaque ? "Opaque" : "Transparent";
            context.AddSubShader(Unlit(renderType, renderQueue, this));
        }
        
        public static SubShaderDescriptor Unlit(string renderType, string renderQueue, DgxTarget target)
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

            result.passes.Add(UnlitPass(target));
            result.passes.Add(Meta(target));
            //
            // if (target.mayWriteDepth)
            //     result.passes.Add(CorePasses.DepthOnly(target));
            //
            // result.passes.Add(CorePasses.ShadowCaster(target));
            // result.passes.Add(CorePasses.SceneSelection(target));
            // result.passes.Add(CorePasses.ScenePicking(target));

            return result;
        }
        
        public static PassDescriptor UnlitPass(DgxTarget target)
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
                renderStates = DefaultRenderStates(target),
                pragmas = Pragmas,
                defines = new DefineCollection(),
                keywords = new KeywordCollection(),
                includes = Includes,

                // Custom Interpolator Support
                customInterpolators = CustomInterpolators
            };
            //CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
            return result;
        }
        public static PassDescriptor Meta(DgxTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "Meta",
                referenceName = "SHADERPASS_META",
                lightMode = "Meta",

                // Template
                passTemplatePath = "Packages/com.dgx.srp/Editor/Templates/ShaderPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(
                    new [] { "Packages/com.dgx.srp/Editor/Templates" }).ToArray(),

                // Port Mask
                validVertexBlocks = Vertex,
                validPixelBlocks = FragmentMeta,

                // Fields
                structs = DefaultStructs,
                requiredFields = new FieldCollection()
                {
                    StructFields.Attributes.uv1,                            // needed for meta vertex position
                    StructFields.Attributes.uv2,                            //needed for meta vertex position
                },
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = MetaRenderState(),
                pragmas = MetaPragmas,
                defines = new DefineCollection(),
                keywords = new KeywordCollection(),
                includes = Includes,

                // Custom Interpolator Support
                customInterpolators = CustomInterpolators
            };

            //AddMetaControlsToPass(ref result, target);
            return result;
        }
        public static readonly CustomInterpSubGen.Collection CustomInterpolators = new()
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
        
        public static readonly PragmaCollection MetaPragmas = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.MultiCompileInstancing },
            //{ Pragma.MultiCompileFog },
            //{ Pragma.MultiCompileForwardBase },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("FragmentMetaCustom") },
            { Pragma.DOTSInstancing },
        };

        public static RenderStateCollection MetaRenderState()
        {
            return new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off) },
            };
        }
        
        public static RenderStateCollection DefaultRenderStates(DgxTarget target)
        {
            var result = new RenderStateCollection();
            result.Add(RenderState.ZTest("LEqual"));

            if (target.ZWriteMode == ZWrite.On)
            {
                result.Add(RenderState.ZWrite("On"));    
            }
            else
            {
                result.Add(RenderState.ZWrite("Off"));
            }
            
            result.Add(RenderState.Cull("Back"));
            
            if (target.surfaceType == SurfaceType.Opaque)
            {
                result.Add(RenderState.Blend(Blend.One, Blend.Zero));
            }
            else
            {
                if (target.alphaMode == AlphaMode.Alpha)
                    result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                else if (target.alphaMode == AlphaMode.Premultiply)
                    result.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                else if (target.alphaMode == AlphaMode.Additive)
                    result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
                else if (target.alphaMode == AlphaMode.Multiply)
                    result.Add(RenderState.Blend(Blend.DstColor, Blend.Zero));
            }
            
            
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
            BlockFields.SurfaceDescription.Emission,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
        };
        public static readonly BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Emission,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
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
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            context.AddProperty("ZWrite", new EnumField(ZWriteMode) { value = ZWriteMode }, (evt) =>
            {
                if (Equals(ZWriteMode, evt.newValue))
                    return;

                registerUndo("Change z write mode");
                ZWriteMode = (ZWrite)evt.newValue;
                onChange();
            });  
            
            context.AddProperty("Surface Type", new EnumField(SurfaceType.Opaque) { value = surfaceType }, (evt) =>
            {
                if (Equals(surfaceType, evt.newValue))
                    return;

                registerUndo("Change Surface");
                surfaceType = (SurfaceType)evt.newValue;
                onChange();
            });

            if (surfaceType == SurfaceType.Transparent)
            {
                context.AddProperty("Alpha mode", new EnumField(alphaMode) { value = alphaMode }, (evt) =>
                {
                    if (Equals(alphaMode, evt.newValue))
                        return;

                    registerUndo("Change alpha mode");
                    alphaMode = (AlphaMode)evt.newValue;
                    onChange();
                });    
            }
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
