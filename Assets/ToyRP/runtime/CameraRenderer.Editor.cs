using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
public partial class CameraRenderer
{
	//��Ⱦդ��
	partial void DrawGizmos();

	//��Ⱦ��֧�ֵ�shader
	partial void DrawUnsupportedShaders();

	//��Ⱦ
	partial void PrepareForSceneWindow();

	//׼��ÿ��������Ļ�����
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

	//���ƹ��߲�֧�ֵ�shader,
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