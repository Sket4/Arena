using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace TzarGames.ImpostorStudio.Editor
{
	public class CaptureWindow : EditorWindow
	{
		Camera targetCamera = null;
		int renderSize = 2048;
	    int fileSize = 1024;
		string filePrefix = "Impostor";
	    float blur = 1.0f;
	    float blurAlphaMultiply = 1.05f;
	    int blurIterations = 16;

	    string lastDir = null;

		[MenuItem("Tzar Games/Impostor studio")]
		static void show()
		{
			var window = CaptureWindow.GetWindow<CaptureWindow> ();
			window.Show ();
		}

		void OnGUI()
		{
			targetCamera = EditorGUILayout.ObjectField ("Target camera", targetCamera, typeof(Camera)) as Camera;

			if (targetCamera == null) 
			{
				return;
			}

			renderSize = EditorGUILayout.IntField("Render resolution", renderSize);
	        fileSize = EditorGUILayout.IntField("File resolution", fileSize);
	        blur = EditorGUILayout.FloatField("Diffuse blur", blur);
	        blurIterations = EditorGUILayout.IntField("Blur iterations", blurIterations);
	        blurAlphaMultiply = EditorGUILayout.FloatField("Blur alpha multiply", blurAlphaMultiply);
	        filePrefix = EditorGUILayout.TextField ("File prefix", filePrefix);

			if (GUILayout.Button ("Capture")) 
			{
	            var path = EditorUtility.SaveFolderPanel("Folder", "Assets", "");

	            if (string.IsNullOrEmpty(path) == false)
	            {
		            setupAndCapture(path);
	                lastDir = path;
	            }
			}

	        if (string.IsNullOrEmpty(lastDir) == false && GUILayout.Button("Capture and save to last directory"))
	        {
		        setupAndCapture(lastDir);
	        }
	    }

		void setupAndCapture(string path)
		{
			var captureSettings = targetCamera.GetComponent<ImpostorPostEffect>();

			if (captureSettings == null)
			{
				Debug.LogError($"Please add {nameof(ImpostorPostEffect)} script to the camera");
				return;
			}

			var originalRPA = GraphicsSettings.defaultRenderPipeline;
			try
			{
				GraphicsSettings.defaultRenderPipeline = captureSettings.RenderPipelineAsset;
				capture(path, captureSettings);
			}
			finally
			{
				GraphicsSettings.defaultRenderPipeline = originalRPA;
			}
		}

		void capture(string path, ImpostorPostEffect captureSettings)
		{
	        var prevActiveTexture = RenderTexture.active;
			var targetTexture = RenderTexture.GetTemporary (renderSize, renderSize, 32, GraphicsFormat.R8G8B8A8_UNorm);
	        targetTexture.antiAliasing = 1;

	        var secondTexture = RenderTexture.GetTemporary(renderSize, renderSize, 32, GraphicsFormat.R8G8B8A8_UNorm);
	        
	        targetCamera.targetTexture = targetTexture;
	        targetCamera.depthTextureMode = DepthTextureMode.MotionVectors & DepthTextureMode.DepthNormals;
	        
			captureSettings.DepthNormalRender = false;
	        captureSettings.Blur = blur;
	        captureSettings.BlurIterations = blurIterations;
	        captureSettings.BlurAlphaMultiply = blurAlphaMultiply;
	        captureSettings.Setup(false);
	        captureSettings.Clear();
	        targetCamera.Render ();
	        
	        //Graphics.Blit(targetTexture, secondTexture);
			//impost.OnRenderImage(secondTexture, targetTexture);
	        
	        //impost.RestoreAlpha(targetTexture);

			var diffuse = covertRenderTexTo2D (targetTexture, fileSize, false);
	        
			captureSettings.DepthNormalRender = true;
	        captureSettings.Blur = 0;
	        captureSettings.Setup(true);
	        targetCamera.Render ();
	        
	        //Graphics.Blit(targetTexture, secondTexture);
	        //impost.OnRenderImage(secondTexture, targetTexture);
	        
			var normal = covertRenderTexTo2D (targetTexture, fileSize, false);
	        targetCamera.targetTexture = prevActiveTexture;
			RenderTexture.ReleaseTemporary (targetTexture);
			RenderTexture.ReleaseTemporary(secondTexture);
			captureSettings.DepthNormalRender = false;
	        captureSettings.Blur = blur;
	        captureSettings.Setup(true);

	        string diffusePath = path + "/" + filePrefix + "_diffuse.png";
	        System.IO.File.WriteAllBytes (diffusePath, diffuse.EncodeToPNG());
	        
	        DestroyImmediate(diffuse);

	        string normalPath = path + "/" + filePrefix + "_normal.png";
	        System.IO.File.WriteAllBytes (normalPath, normal.EncodeToPNG());
	        DestroyImmediate(normal);

	        AssetDatabase.Refresh();
	    }

		private static Texture2D covertRenderTexTo2D(RenderTexture renderTexture, int size, bool hdr)
		{
			var prev = RenderTexture.active;
	        var tempRt = RenderTexture.GetTemporary(size, size, renderTexture.depth, renderTexture.format, RenderTextureReadWrite.Default, renderTexture.antiAliasing);
	        var renderMat = new Material(Shader.Find("Impostor Studio/Render"));
	        var prevGlState = GL.sRGBWrite;
	        GL.sRGBWrite = true;
	        Graphics.Blit(renderTexture, tempRt, renderMat, 0);
	        RenderTexture.active = tempRt;
	        var format = hdr ? TextureFormat.RGBAFloat : TextureFormat.RGBA32;
			var result = new Texture2D (size, size, format, false);
			result.ReadPixels (new Rect (0, 0, size, size), 0, 0);
			result.Apply();
	        GL.sRGBWrite = prevGlState;
	        RenderTexture.active = prev;
	        RenderTexture.ReleaseTemporary(tempRt);
	        DestroyImmediate(renderMat);
			return result;
		}
	}
}