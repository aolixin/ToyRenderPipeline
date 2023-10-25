using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
	litShaderTagId = new ShaderTagId("ToyRPLit");

	ToyRPLighting lighting = new ToyRPLighting();
	ScriptableRenderContext context;

	Camera camera;

	const string bufferName = "Render Camera";

	// 视锥体剔除结果
	CullingResults cullingResults;

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	public void Render(ScriptableRenderContext context, Camera camera,
		bool useDynamicBatching, bool useGPUInstancing,
		ShadowSettings shadowSettings)
	{
		this.context = context;
		this.camera = camera;

		PrepareBuffer();
		PrepareForSceneWindow();
		if (!Cull(shadowSettings.maxDistance))
		{
			return;
		}

		buffer.BeginSample(SampleName);
		ExecuteBuffer();
		lighting.Setup(context, cullingResults, shadowSettings);
		buffer.EndSample(SampleName);

		Setup();
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();

		DrawGizmos();

		//Material mat = new Material(Shader.Find("TorRP/DrawTexture"));
		//buffer.Blit(Shadows.dirShadowAtlasId,camera.targetTexture ,mat);

		lighting.Cleanup();
		Submit();
	}

	bool Cull(float maxShadowDistance)
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{
			p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}

	void Setup()
	{
		context.SetupCameraProperties(camera);
		CameraClearFlags flags = camera.clearFlags;

		buffer.ClearRenderTarget(
			flags <= CameraClearFlags.Depth,
			flags == CameraClearFlags.Color,
			flags == CameraClearFlags.Color ?
				camera.backgroundColor.linear : Color.clear
		);

		buffer.BeginSample(SampleName);// 创建sample, frame debugger和profiler才能看到
		ExecuteBuffer(); // 执行buffer
	}
	void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
	{
		//绘制顺序
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.CommonOpaque
		};


		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		)
		{
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.SetShaderPassName(1, litShaderTagId);

		//过滤绘制对象，如透明，不透明等
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		//渲染不透明物体
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
		//天空盒
		context.DrawSkybox(camera);

		// 为了渲染透明物体，先渲染不透明物体，然后是天空盒，然后是透明物体
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}


	void Submit()
	{
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
	}

	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
}