using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{

	const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;
	static string[] directionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};
	//绘制shadowMap的shader
	public static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
		dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
		cascadeCountId = Shader.PropertyToID("_CascadeCount"),
		cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
		cascadeDataId = Shader.PropertyToID("_CascadeData"),
		shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
		shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

	// shadows 的旋转矩阵
	static Matrix4x4[] 
		dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
	// 联级阴影的 culling sphere 信息
	static Vector4[] 
		cascadeCullingSpheres = new Vector4[maxCascades],
		cascadeData = new Vector4[maxCascades];


	int ShadowedDirectionalLightCount;

	struct ShadowedDirectionalLight
	{
		// 每个 light 的索引
		public int visibleLightIndex;
		// 每个 light 的偏移
		public float slopeScaleBias;
		public float nearPlaneOffset;
	}

	ShadowedDirectionalLight[] ShadowedDirectionalLights =
		new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

	const string bufferName = "Shadows";
	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	ScriptableRenderContext context;

	CullingResults cullingResults;

	ShadowSettings settings;

	public void Setup(
		ScriptableRenderContext context, CullingResults cullingResults,
		ShadowSettings settings
	)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;


		ShadowedDirectionalLightCount = 0;
	}

	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
	public void Render()
	{
		if (ShadowedDirectionalLightCount > 0)
		{
			RenderDirectionalShadows();
		}
		else
		{
			buffer.GetTemporaryRT(
				dirShadowAtlasId, 1, 1,
				32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
			);
		}
	}
	// 获取投射阴影的 light 信息 vec2( lighrStrength , index in _DirectionalShadowMatrices )
	public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
	{
		if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
			light.shadows != LightShadows.None && light.shadowStrength > 0f &&
			cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)
			)
		{
			//Debug.Log(light.shadowStrength);
			ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
				new ShadowedDirectionalLight
				{
					visibleLightIndex = visibleLightIndex,
					slopeScaleBias = light.shadowBias,
					nearPlaneOffset = light.shadowNearPlane
				};
			//Debug.Log(visibleLightIndex);
			return new Vector3(
				light.shadowStrength, 
				settings.directional.cascadeCount * ShadowedDirectionalLightCount++,
				light.shadowNormalBias
			);
		}
		return Vector3.zero;
	}

	

	void RenderDirectionalShadows()
	{
		int atlasSize = (int)settings.directional.atlasSize;
		buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
			32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

		buffer.SetRenderTarget(
			dirShadowAtlasId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		// 清除缓冲
		buffer.ClearRenderTarget(true, false, Color.clear);
		buffer.BeginSample(bufferName);
		ExecuteBuffer();
		//Debug.Log(ShadowedDirectionalLightCount);
		int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		//Debug.Log(split);
		int tileSize = atlasSize / split;

		for (int i = 0; i < ShadowedDirectionalLightCount; i++)
		{
			RenderDirectionalShadows(i, split, tileSize);
		}

		buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);

		float f = 1f - settings.directional.cascadeFade;
		buffer.SetGlobalVector(
			shadowDistanceFadeId,
			new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade,
				1f / (1f - f * f))
		);

		SetKeywords();
		buffer.SetGlobalVector(
			shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize)
		);
		buffer.EndSample(bufferName);
		ExecuteBuffer();
	}
	void SetKeywords()
	{
		int enabledIndex = (int)settings.directional.filter - 1;
		for (int i = 0; i < directionalFilterKeywords.Length; i++)
		{
			if (i == enabledIndex)
			{
				buffer.EnableShaderKeyword(directionalFilterKeywords[i]);
			}
			else
			{
				buffer.DisableShaderKeyword(directionalFilterKeywords[i]);
			}
		}
	}

	void RenderDirectionalShadows(int index, int split, int tileSize)
	{
		ShadowedDirectionalLight light = ShadowedDirectionalLights[index];

		var shadowSettings =new ShadowDrawingSettings(cullingResults, light.visibleLightIndex,BatchCullingProjectionType.Orthographic);


		int cascadeCount = settings.directional.cascadeCount;
		int tileOffset = index * cascadeCount;
		Vector3 ratios = settings.directional.CascadeRatios;
		//Debug.Log(cascadeCount);
		for (int i = 0; i < cascadeCount; i++)
		{
			
			cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
			light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
			out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
			out ShadowSplitData splitData
		);

			shadowSettings.splitData = splitData;
			if (index == 0)
			{
				SetCascadeData(i, splitData.cullingSphere, tileSize);
				
				//Vector4 cullingSphere = splitData.cullingSphere;
				//cullingSphere.w *= cullingSphere.w;
				//cascadeCullingSpheres[i] = cullingSphere;
			}

			int tileIndex = tileOffset + i;
			// v , p 矩阵
			//Debug.Log(index);
			dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
				projectionMatrix * viewMatrix,
				SetTileViewport(tileIndex, split, tileSize), split
			);

			buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
			buffer.SetGlobalVectorArray(
				cascadeCullingSpheresId, cascadeCullingSpheres
			);

			buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);

			buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

			//buffer.SetGlobalDepthBias(0, 3f);
			buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
			ExecuteBuffer();
			context.DrawShadows(ref shadowSettings);
			buffer.SetGlobalDepthBias(0f, 0f);
		}
	}
	void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
	{
		float texelSize = 2f * cullingSphere.w / tileSize;
		float filterSize = texelSize * ((float)settings.directional.filter + 1f);
		// cullingSphere -- x,y,z,r
		//cascadeData[index].x = 1f / cullingSphere.w;
		cascadeData[index] = new Vector4(
			1f / cullingSphere.w,
			filterSize * 1.4142136f
		);
		cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[index] = cullingSphere;
	}


	Vector2 SetTileViewport(int index, int split, float tileSize)
	{
		Vector2 offset = new Vector2(index % split, index / split);
		buffer.SetViewport(new Rect(
			offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
		));
		//Debug.Log(offset);
		return offset;
	}

	// 计算经过分割texture的矩阵
	Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
	{
		//Debug.Log("split: "+split);
		//Debug.Log("offset: " + offset);
		if (SystemInfo.usesReversedZBuffer)
		{
			//Debug.Log("fd");
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}
		// [-1,1]映射到[0,1]

		// 坐标映射到四个子 map
		float scale = 1f / split;
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);
     
        return m;
	}

	//释放临时纹理
	public void Cleanup()
	{
		//Debug.Log("你好");
		buffer.ReleaseTemporaryRT(dirShadowAtlasId);
		ExecuteBuffer();
	}
}