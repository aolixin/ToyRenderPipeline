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

	// ��׶���޳����
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

		buffer.BeginSample(SampleName);// ����sample, frame debugger��profiler���ܿ���
		ExecuteBuffer(); // ִ��buffer
	}
	void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
	{
		//����˳��
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

		//���˻��ƶ�����͸������͸����
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		//��Ⱦ��͸������
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
		//��պ�
		context.DrawSkybox(camera);

		// Ϊ����Ⱦ͸�����壬����Ⱦ��͸�����壬Ȼ������պУ�Ȼ����͸������
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