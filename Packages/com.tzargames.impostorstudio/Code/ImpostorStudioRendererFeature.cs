// using UnityEngine;
// using UnityEngine.Experimental.Rendering;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.Universal;
//
// namespace TzarGames.ImpostorStudio
// {
//     public class ImpostorStudioRendererFeature : ScriptableRendererFeature
//     {
//         [SerializeField] private Material material;
//         private static readonly int DestTexID = Shader.PropertyToID("TG_DestTex");
//         
//         class Pass : ScriptableRenderPass
//         {
//             private Material mat;
//             
//             public Pass(Material material)
//             {
//                 mat = material;
//             }
//
//             private RenderTargetIdentifier destID;
//             private RTHandle dest; 
//
//             public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
//             {
//                 dest = RTHandles.Alloc("TG_DestTex", name: "TG_DestTex");
//                 
//                 cmd.GetTemporaryRT(DestTexID, cameraTextureDescriptor);
//              
//                 base.Configure(cmd, cameraTextureDescriptor);
//             }
//
//             public override void FrameCleanup(CommandBuffer cmd)
//             {
//                 cmd.ReleaseTemporaryRT(DestTexID);
//                 base.FrameCleanup(cmd);
//             }
//
//             public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//             {
//                 if (mat == null)
//                 {
//                     return;
//                 }
//
//                 CommandBuffer cmd = CommandBufferPool.Get("Impostor Studio Pass");
//                 
//
//                 var settings = renderingData.cameraData.camera.GetComponent<ImpostorPostEffect>();
//
//                 var prevGlState = GL.sRGBWrite;
//                 var src = renderingData.cameraData.renderer.cameraColorTargetHandle;
//                 
//                 if (settings.DepthNormalRender) 
//                 {
//                     cmd.Blit (src, dest.nameID, mat, 1);
//                 } 
//                 else 
//                 {
//                     cmd.Blit (src, dest.nameID, mat, 0);
//                 }
//
//                 if (settings.Blur != 0.0f)
//                 {
//                     int orig = Shader.PropertyToID("TG_BlurTex1");
//                     int temp = Shader.PropertyToID("TG_BlurTex2");
//                     
//                     mat.SetFloat("_Blur", settings.Blur);
//                     mat.SetFloat("_BlurAlphaMultiply", settings.BlurAlphaMultiply);
//                 
//                     var camDesc = renderingData.cameraData.cameraTargetDescriptor;
//                     cmd.GetTemporaryRT(orig,  camDesc.width, camDesc.height);
//                     cmd.GetTemporaryRT(temp, camDesc.width, camDesc.height);
//                 
//                     cmd.Blit(dest.nameID, orig);
//                 
//                     for (int i=0; i < settings.BlurIterations; i++)
//                     {
//                         cmd.Blit(dest.nameID, temp, mat, 2);
//                         cmd.Blit(temp, dest.nameID, mat, 3);
//                     }
//                 
//                     cmd.Blit(dest.nameID, temp);
//                     cmd.SetGlobalTexture("_SecondTex", orig);
//                     //mat.SetTexture("_SecondTex", orig);
//                     cmd.Blit(temp, dest.nameID, mat, 4);
//                 
//                     cmd.ReleaseTemporaryRT(temp);
//                     cmd.ReleaseTemporaryRT(orig);
//                 }
//                 
//                 cmd.Blit(dest.nameID, src);
//                 
//                 GL.sRGBWrite = prevGlState;
//                 
//                 context.ExecuteCommandBuffer(cmd);
//
//                 cmd.Clear();
//                 CommandBufferPool.Release(cmd);
//             }
//         }
//
//         private Pass pass;
//         
//         public override void Create()
//         {
//             pass = new Pass(material);
//         }
//
//         public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
//         {
//             if (material == null)
//             {
//                 return;
//             }
//
//             pass.ConfigureInput(ScriptableRenderPassInput.Normal);
//             renderer.EnqueuePass(pass);
//         }
//     }
// }