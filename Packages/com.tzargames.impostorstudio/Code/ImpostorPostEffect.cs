using UnityEngine;
using UnityEngine.Rendering;

namespace TzarGames.ImpostorStudio
{
    [ExecuteInEditMode]
    public class ImpostorPostEffect : MonoBehaviour 
    {
	    [SerializeField] private RenderPipelineAsset renderPipelineAsset;
        public RenderPipelineAsset RenderPipelineAsset => renderPipelineAsset;

        [SerializeField]
        int blurIterations = 20;

        [SerializeField]
        private float blur = 1.0f;

        [SerializeField]
        float blurAlphaMultiply = 1.05f;
        
        [SerializeField]
        Material testMaterial = null;

        Material mat = null;
        RenderTexture resultTexture = null;

	    public bool DepthNormalRender {
		    get;
		    set;
	    }

        public float Blur
        {
            get { return blur; }
            set { blur = value; }
        }

        public int BlurIterations
        {
            get { return blurIterations; }
            set { blurIterations = value; }
        }

        public float BlurAlphaMultiply
        {
            get { return blurAlphaMultiply; }
            set { blurAlphaMultiply = value; }
        }

	    void OnEnable()
	    {
		    GetComponent<Camera> ().depthTextureMode = DepthTextureMode.DepthNormals;
	    }

        private void OnDisable()
        {
            RenderTexture.ReleaseTemporary(resultTexture);
        }

        Material createMaterial()
	    {
		    return new Material (Shader.Find ("Impostor Studio/Render"));
	    }

        RenderTexture getTempRenderTextureFrom(RenderTexture src)
        {
            return RenderTexture.GetTemporary(src.width, src.height, src.depth, src.format, RenderTextureReadWrite.Default, src.antiAliasing);
        }

        public void Clear()
        {
            // clear first
            var camera = GetComponent<Camera>();
            var clearFlags = camera.clearFlags;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.Render();
            camera.clearFlags = clearFlags;
        }

        public void Setup(bool enablePostProcessing)
        {
            var camera = GetComponent<Camera>();

            throw new System.NotImplementedException();
            //var urpData = GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            //urpData.renderPostProcessing = enablePostProcessing;
            camera.depthTextureMode = DepthTextureMode.DepthNormals;
        }

        public void RestoreAlpha(RenderTexture destination)
        {
            if(resultTexture != null)
            {
                var temp = getTempRenderTextureFrom(destination);
                mat.SetTexture("_FirstTex", destination);
                mat.SetTexture("_SecondTex", resultTexture);
                Graphics.Blit(destination, temp);
                Graphics.Blit(temp, destination, mat, 5);
                RenderTexture.ReleaseTemporary(temp);
            }
        }
		    
        // legacy code for BIRP
	    public void OnRenderImage(RenderTexture src, RenderTexture dest) 
	    {
            Debug.Log("On render image");
            
		    if (mat == null) 
		    {
			    mat = createMaterial ();
		    }

            if(testMaterial != null && testMaterial.mainTexture == null)
            {
                testMaterial.mainTexture = resultTexture;
            }
            
            var prevGlState = GL.sRGBWrite;
            
            if (DepthNormalRender) 
		    {
                GL.sRGBWrite = false;
			    Graphics.Blit (src, dest, mat, 1);
		    } 
		    else 
		    {
                GL.sRGBWrite = true;
                Graphics.Blit (src, dest, mat, 0);
		    }

            if (blur != 0.0f)
            {
                mat.SetFloat("_Blur", blur);
                mat.SetFloat("_BlurAlphaMultiply", blurAlphaMultiply);

                var orig = RenderTexture.GetTemporary(src.width, src.height);
                var temp = RenderTexture.GetTemporary(src.width, src.height);

                Graphics.Blit(dest, orig);

                for (int i=0; i< blurIterations; i++)
                {
                    Graphics.Blit(dest, temp, mat, 2);
                    Graphics.Blit(temp, dest, mat, 3);
                }

                Graphics.Blit(dest, temp);
                mat.SetTexture("_SecondTex", orig);
                Graphics.Blit(temp, dest, mat, 4);

                RenderTexture.ReleaseTemporary(temp);
                RenderTexture.ReleaseTemporary(orig);
            }

            if(resultTexture != null)
            {
                RenderTexture.ReleaseTemporary(resultTexture);
            }
            resultTexture = getTempRenderTextureFrom(src);
            Graphics.Blit(dest, resultTexture);
            GL.sRGBWrite = prevGlState;
        }
    }
}
