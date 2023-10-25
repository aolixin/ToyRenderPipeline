using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ToyRPLighting
{

	const string bufferName = "Lighting";

	CullingResults cullingResults;

	//ֻ֧���ĸ�ƽ�й�
	const int maxDirLightCount = 4;

	static int
		dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
		dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
		dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"), // ���� shade �Ĺ���visibleLight�е�����
		dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData"); // ���� shadow �Ĺ���visibleLight�е�����

	static Vector4[]
	dirLightColors = new Vector4[maxDirLightCount],
	dirLightDirections = new Vector4[maxDirLightCount],
	dirLightShadowData = new Vector4[maxDirLightCount];

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	public Shadows shadows = new Shadows();

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults,
		ShadowSettings shadowSettings)
	{
		this.cullingResults = cullingResults;
		buffer.BeginSample(bufferName);
		shadows.Setup(context, cullingResults, shadowSettings);

		SetupLights();
		//��ȾshadowMap
		shadows.Render();

		buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
	{
		//Debug.Log(index);
		dirLightColors[index] = visibleLight.finalColor;

		dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

		// ��ȡ���� shadow �Ĺ�������
		//shadows.ReserveDirectionalShadows(visibleLight.light, index);
		// ��ȡ���� shade �Ĺ�������
		dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
	}
	void SetupLights() 
	{
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
		
		int dirLightCount = 0;
		for (int i = 0; i < visibleLights.Length; i++)
		{
			VisibleLight visibleLight = visibleLights[i];
			if (visibleLight.lightType == LightType.Directional)
			{
				//Debug.Log("dirLightCount: "+ dirLightCount);
				SetupDirectionalLight(dirLightCount++, ref visibleLight);
				if (dirLightCount >= maxDirLightCount)
				{
					break;
				}
			}
		}
		//Debug.Log("visibleLights.Length: " + visibleLights.Length);
		buffer.SetGlobalInt(dirLightCountId, dirLightCount);
		buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
		buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
		buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
	}

	public void Cleanup()
	{
		shadows.Cleanup();
	}
}