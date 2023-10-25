using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
public partial class CameraRenderer
{
	//渲染栅格
	partial void DrawGizmos();

	//渲染不支持的shader
	partial void DrawUnsupportedShaders();

	//渲染
	partial void PrepareForSceneWindow();

	//准备每个摄像机的缓冲区
	partial void PrepareBuffer();
#if UNITY_EDITOR
	static Material errorMaterial;

	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	string SampleName
	{ get; set; }

	partial void DrawGizmos()
	{
		if (Handles.ShouldRenderGizmos())
		{
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}
	partial void PrepareForSceneWindow()
	{
		if (camera.cameraType == CameraType.SceneView)
		{
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		}
	}

	partial void PrepareBuffer()
	{
		buffer.name = SampleName = camera.name;
	}

	//绘制管线不支持的shader,
	partial void DrawUnsupportedShaders()
	{
		if (errorMaterial == null)
		{
			errorMaterial =
				new Material(Shader.Find("Hidden/InternalErrorShader"));
		}
		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(camera)
		)
		{
			overrideMaterial = errorMaterial
		};


		for (int i = 1; i < legacyShaderTagIds.Length; i++)
		{
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}

		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}
#else

	const string SampleName = bufferName;

#endif

}