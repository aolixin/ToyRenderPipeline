# unity SRP

-- 敖立鑫

2023.10.18开始

[Custom Render Pipeline (catlikecoding.com)](https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/)  -- 基于这个教程



## srp渲染流程

+ 渲染流程
  + 创建commandBuffer 
  + 用buffer执行命令（配置buffer）如:buffer.ClearRenderTarget(true, true, Color.clear);
  + 用context（上下文）执行buffer
  + 清除buffer


+ context

  + 可以直接执行一些命令如context.DrawSkybox(camera);

  + 也可以执行commandBuffer



## 基本结构

-- todo



## 配置

### drawingSettings

+ 里面有一些渲染设置
  + **渲染排序设置（SortingSettings）**：
    - 可以设置渲染对象的排序方式，以控制绘制顺序，包括前向渲染（Forward Rendering）和透明对象的排序。
    - 你可以定义绘制对象的排序层级、渲染队列、渲染模式等。
  + **着色器通道设置（ShaderPassName）**：
    - 可以指定要使用的着色器通道，以定义如何渲染对象。
    - 这包括了渲染效果、材质属性、着色器功能等。
  + **Override Material（覆盖材质）**：
    - 可以指定一个材质来覆盖对象的原始材质，从而实现特定的渲染效果或外观变化。
  + **光照设置（Lighting Settings）**：
    - 可以设置是否启用光照，以及如何应用光照。
    - 这包括了是否启用实时阴影、光照模式等。
  + **剔除（Culling）设置**：
    - 可以定义剔除设置，包括视锥体剔除、遮挡剔除等，以减少渲染的开销。
  + **渲染队列（Rendering Layer）**：
    + 可以指定要渲染的渲染队列，用于将对象分类并在不同阶段渲染
+ 比如：drawingSettings.SetShaderPassName(1, litShaderTagId);   
  + 修改着色器通道
  + 感觉类似于 opengl 中的shader.use()





### ScriptableCullingParameters

`ScriptableCullingParameters` 是Unity中的一个类，用于表示和配置裁剪（culling）过程的参数。裁剪是渲染管线中的一个关键步骤，用于确定哪些对象在摄像机的视锥体内可见，以减少不必要的渲染工作。`ScriptableCullingParameters` 允许你配置裁剪参数，以满足渲染需求。

这个类通常用于与自定义渲染管线（如SRP，Scriptable Render Pipeline）一起使用，以更好地控制渲染过程。以下是一些 `ScriptableCullingParameters` 类的常见属性和用途：

- **`layerFarCullDistances` 和 `layerCullDistances`**：这些属性允许你为不同的图层配置远裁剪距离。这是一个优化技巧，可以根据图层将视锥体外的对象排除在渲染之外。
- **`cullingPlane` 和 `cullingPlaneCount`**：你可以配置自定义裁剪平面，以进一步调整视锥体的形状。这对于创建非标准的裁剪区域非常有用。
- **`isOrthographic` 和 `projectionMatrix`**：这些属性用于指定摄像机是正交投影还是透视投影，并设置投影矩阵。这对于不同类型的摄像机配置非常重要。
- **`layerMask` 和 `sceneMask`**：你可以配置要裁剪的图层和场景，以过滤视锥体内的对象。
- **`maximumVisibleLights`**：这个属性用于指定在裁剪期间能够处理的最大可见光源数量。
- **`shadowDistance` 和 `screenSpaceShadowRes`**：这些属性用于配置阴影渲染的参数，以便在裁剪期间生成阴影。
- **`lodBias`**：这个属性用于指定层次渐进细节（LOD）偏差，以在裁剪期间控制物体的细节级别。

`ScriptableCullingParameters` 类的实例通常会在渲染管线的裁剪阶段使用，以确定可见对象并将其传递给渲染阶段。这个类允许开发人员在自定义渲染管线中更好地控制裁剪过程，以优化性能和满足特定需求。









## srp 中ugui绘制

![image-20231018201531240](https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281452180.png)

![image-20231018201545395](https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281452429.png)

当render mode是overlay时，ugui渲染不归srp管，把render mode调成camera，并把render target调成 自己的camera，就归管线管了 -- 会被归到透明几何体的绘制

![image-20231018202044570](https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281452941.png)





## unity 批处理 [Unity渲染优化的4种批处理](https://zhuanlan.zhihu.com/p/432223843)

+ 静态批处理
  + 将物体设置为static，静态批处理不一定减少DrawCall，但是会让CPU在“设置渲染状态-提交Draw Call”上更高效
  + 为什么不手动合并mesh，因为假如手动合并，在视锥体剔除时，只要出现一个三角形，那么视锥体剔除就不会剔除掉
  + 条件
    +  使用相同材质引用的静态物体
    +  物体需为Mesh，具有MeshFilter和MeshRenderer组件
    +  Mesh 需要在ImportSettings面板勾选【read/write enabled】
+ 动态批处理
  + 在运行时Unity自动把每一帧画面里符合条件的多个模型网格合并为一个，再传递给GPU
  + 条件十分苛刻
    + [unity静动态批处理的触发条件以及无效的问题解决办法](https://blog.csdn.net/lengyoumo/article/details/109328193)
+ SRP batcher（相同shader，不同mat）
  + 对于结构相同的shader，可以将数据直接全部存到GPU（CBUFFER），减少状态转换，不能减少draw call
  + 得看RP支不支持，shader需要支持SRP Batcher（HDRP和URP项目的Lit和Unlit shader都支持）
+ GPU Instancing（相同mesh，相同mat）
  + 将实例数据存储到 GPU（UNITY_INSTANCING_BUFFER）
  + GPU Instancing适用于处理大量相同物体，比如建筑物/树/草等重复出现的物体。




### SRP batcher

+ cbuffer -- hlsl中常量缓冲区 ： 常量数据可以包括变换矩阵、材质属性、光照信息等。

+ 通过 cbuffers 实现srp batcher，将materials各种参数提前存入GPU实现

+ 在shader中定义cbuffers

  + ```
    //材质cbuffer
    CBUFFER_START(UnityPerMaterial)
    	float4 _BaseColor;
    CBUFFER_END
    ```

  + ```
    //矩阵buffer
    CBUFFER_START(UnityPerDraw)
    	float4x4 unity_ObjectToWorld;
    	float4x4 unity_WorldToObject;
    	real4 unity_WorldTransformParams;
    	float4 unity_LODFade;
    CBUFFER_END
    ```

+ GraphicsSettings.useScriptableRenderPipelineBatching = true;实现绘制统一shader但是不同mat的多个物体

  ![image-20231020095318248](https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281452751.png)

  右边frame debugger可以看出drawSRPBatcher，

  但是为每个obj添加一个新颜色只能添加一个mat，很不方便，通过脚本修改属性（renderer.SetPropertyBlock(props);）的话又退化成普通draw call

  

### **GPU Instancing**

[GPU Instancing原理)](https://zhuanlan.zhihu.com/p/523765931)

####　unity 开启 GPU Instancing：

+ build-in管线：standard shader开启GPU Instance选项
+ URP默认不支持
+ 有时候GPU Instancing还是分批渲染，每批有上限 -- cbuffer有上限
+ [创建支持GPU Instancing 的 shader](https://zhuanlan.zhihu.com/p/524195324)
+ 让每个 instance 属性独立 -- MaterialPropertyBlock

#### srp 实现

+ shader部分和上面大差不差
+ 不过srp batcher 和 GPU Instancing不兼容，所以得先删除srp batcher部分代码

```
Graphics.DrawMeshInstanced(mesh, 0, material, matrices, 1023, block);
```





### srp batcher和 GPU Instancing区别

+ srp batcher（相同shader，不同mat）：传多个mesh ，传一个 block（CBUFFER） ，block里面有每个mat，每个mat用的shader必须是同一个，保证数据格式相同，状态转换不会出错
+ GPU Instancing（相同mesh，相同mat）： 传一个mesh ,传一个block（UNITY_INSTANCING_BUFFER），block有每个不同数据，相同mat，但是不同的

+ 作用范围
  + 多个 shader 可以共用一个 CBUFFER，因为里面存储的光照信息等是全局相同的
  + 但是一般不共用一个 UNITY_INSTANCING_BUFFE，每个 shader 主要用于存储示例数据



### Dynamic Batching

srp实现 Dynamic Batching，优先级在srp batcher之下

+ ```
  //改变drawingSettings
  var drawingSettings = new DrawingSettings(
  			unlitShaderTagId, sortingSettings
  		) {
  			enableDynamicBatching = true,
  			enableInstancing = false
  		};
  ```





## SRP 实现Lit shader

```
// 获取光源方向的反方向
dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
```



```
// 一个 Instancing 缓冲区
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)

	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
```



### 实现（一个实现一个文件，有点面向对象的意思）

+ 创建自己的 lightMode

+ 修改 Render 函数

+ 修改 shaderID -- drawingSettings.SetShaderPassName

+ 创建light -- 存入cbuffer

  + ```
    struct Light {
    	float3 color;
    	float3 direction;
    };
    ```

+ 创建 surface

  + ```
    struct Surface {
    	float3 normal;
    	float3 viewDirection;
    	float3 color;
    	float alpha;
    
    	float metallic;
    	float smoothness;
    
    };
    ```

+ 创建 brdf

  + ```
    struct BRDF {
    	float3 diffuse;
    	float3 specular;
    	float roughness;
    };
    ```

+ 将光照信息存在UNITY_INSTANCING_BUFFER

+ 通过surface获取光照

  + float3 GetLighting (Surface surface,BRDF brdf)

+ brdf + litShader + direct light + GPU Instancing（还挺好看的）

<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281452684.png" alt="image-20231021210237424" style="zoom:25%;" />



+ 加上alpha裁剪
  + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281452764.png" alt="image-20231022101123043" style="zoom:25%;" />













## Shadows

### 类图

![image-20231022110239840](C:\Users\aolixin\AppData\Roaming\Typora\typora-user-images\image-20231022110239840.png)



### 生成shadowMap

+ ![img](https://pic4.zhimg.com/80/v2-c7108831a33744083819d7615c5bfe57_720w.webp)

+ ```
  // 创建一张临时纹理
  buffer.GetTemporaryRT()
  // 设置渲染目标
  buffer.SetRenderTarget()
  ```

+ shadowSettings -- 渲染shadowMap的配置

+ ```
  // 获取渲染阴影时的变换矩阵，阴影分割数据等
  cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
  			light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
  			out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
  			out ShadowSplitData splitData
  		);
  ```

+ ```
  // 渲染阴影
  // tips：context.DrawShadows只渲染包含ShadowCaster Pass的材质
  context.DrawShadows(ref shadowSettings);
  ```
  
+ 当投射阴影的光数量大于1时候，将shadowMap分割成四分，通过 `buffer.SetViewport()` 渲染纹理的一部分（）

+ 因为上述情况，所以光照的vp矩阵得做一个映射

  + ```
    // 坐标映射，这段代码害我debug了六个小时！！！
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //Debug.Log("split: "+split);
        //Debug.Log("offset: " + offset);
        if (SystemInfo.usesReversedZBuffer)
        {
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
    ```

    shadowMap 的采样有说法，详情看代码

+ 将 surface 世界坐标转换到shaowMap坐标下（通过第三步获取的转换矩阵）

+ ```
  // unity内置宏，对shadowmap采样
  TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
  #define SHADOW_SAMPLER sampler_linear_clamp_compare
  SAMPLER_CMP(SHADOW_SAMPLER);
  ```

+ 获取阴影 strength 和原 color 相乘

+ 最终结果，有严重的摩尔纹

  + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281453415.png" alt="image-20231023183854132" style="zoom: 50%;" />

### 级联shadowMap

+ 原理

  + ![img](https://pic1.zhimg.com/80/v2-b592f37c6cae42255ef3b3ac99a17000_1440w.webp)

+ ```
  // 通过修改参数获取不同的 v,p 矩阵
  cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
  			light.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f,
  			out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
  			out ShadowSplitData splitData
  		);
  ```

  + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281453673.png" alt="image-20231024091425837" style="zoom: 33%;" />

+ culling sphere

  + 在确定每个级联图要渲染的实际区域时，Unity会为根据级联的阴影裁剪长方体创建一个球型空间，该球形空间会包裹整个阴影裁剪长方体，因此球形的空间会比原长方体多包裹住一些空间，这可能会导致有时在裁剪长方体区域外也会绘制一些阴影。下图为Culling Spheres的可视化图。

  + Culling Spheres的作用是让Shader确定相机渲染的每个片元需要采样哪个级联图。原理很简单，对于相机要渲染的一个片元，计算出其光源空间下的坐标，通过它计算片元与每个Culling Sphere球心的距离，最后确定属于哪个球空间内，采样对应级联图。 -- 
  + <img src="https://catlikecoding.com/unity/tutorials/custom-srp/directional-shadows/cascaded-shadow-maps/culling-spheres.png" alt="img" style="zoom: 67%;" />

+ ```
  // 向shader传递 cullingSphere 参数
  // cullingSphere -- x,y,z,r^2
  Vector4 cullingSphere = splitData.cullingSphere;
  cullingSphere.w *= cullingSphere.w;
  cascadeCullingSpheres[i] = cullingSphere;
  ```

+ ```
  // 每个片元对应的 联级的信息
  struct ShadowData {
  	int cascadeIndex; // 联级索引
  	float cascadeBlend; // 和下一联级混合的比例
  	float strength;	// 阴影强度
  };
  
  // 对阴影信息的抽象
  struct DirectionalShadowData {
  	float strength;
  	int tileIndex;
  };
  ```
  
+ shader 中对应关系

  + light -- DirectionalShadowData

  + frag -- shadowData

+ ```
  // 获取具体 DirectionalShadowData -- shadowMap 索引+联级偏移量
  data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
  ```
  
+ ```
  //通过距离判断在那个culling sphere
  int i;
  for (i = 0; i < _CascadeCount; i++) {
      float4 sphere = _CascadeCullingSpheres[i];
      float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
      if (distanceSqr < sphere.w) {
      	break;
      }
      }
  data.cascadeIndex = i;
  ```

+ 联级可视化
  + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281453105.png" alt="image-20231024104421040" style="zoom:33%;" />

+ 超过 maxDistence 不采样
  + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281453161.png" alt="image-20231024105330375" style="zoom: 33%;" />

+ 阴影随着片元在 camera view space 深度增加而变浅 + 阴影在 max cascade 逐渐变浅
  + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281453096.png" alt="image-20231024140333611" style="zoom:33%;" />

### 提升阴影质量

+ shadow acne（毛刺 & 阴影暗斑）

  + 原理 [关于ShadowMap中Shadow acne现象的解释](https://zhuanlan.zhihu.com/p/366555785)
    + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281453260.png" alt="image-20231024141014255" style="zoom:50%;" />
  + 样例
    + ​	<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281454585.png" alt="image-20231024141002382" style="zoom: 50%;" />

  + Depth Bias -- 添加偏移buffer.SetGlobalDepthBias(50000f, 0f); -- 但是会产生阴影偏移

  + Slope Bias -- buffer.SetGlobalDepthBias(0,3f); -- 根据斜度进行偏移

    + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281454961.png" alt="image-20231024141915498" style="zoom: 67%;" />

  + 由于使用联级阴影，使用全局统一的bias不合理，所以需要根据联级不同，使用不同的 culling sphere 半径做法线偏移

    + ```
      float3 normalBias = surfaceWS.normal * _CascadeData[global.cascadeIndex].y;
      ```

    + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281454069.png" alt="image-20231024151624724" style="zoom:55%;" />

+ shadow pancaking （阴影平坠 ）-- 理解的不是很好，有时间再看看

  + 原理：[Unity 阴影——阴影平坠（Shadow pancaking](https://blog.csdn.net/ithot/article/details/125473479)
  + 给进平面添加一个偏移

### 添加 PCF

+ 原理：[CSM, PCSS与SDF Soft Shadow](https://zhuanlan.zhihu.com/p/478472753)

+ float4 _ShadowAtlasSize;  //（像素大小，map尺寸，0，0）

+ SampleShadow_ComputeSamples_Tent_7x7 (size, positionSTS.xy, weights, positions);计算 PCF权重weights ，采样点positions

  + ```
    // 增加采样
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) {
    			shadow += weights[i] * SampleDirectionalShadowAtlas(
    				float3(positions[i].xy, positionSTS.z)
    			);
    		}
    ```

    

+ buffer.EnableShaderKeyword( string ) // 向shader添加关键字，常用于定义宏 

+ 联级过渡出现问题

  + <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281454840.png" alt="image-20231024214954538" style="zoom: 33%;" />
  + 添加联级之间混合，效果好了，但是增加了采样次数

    + ```
      	if (global.cascadeBlend < 1.0) {
      		normalBias = surfaceWS.normal *
      			(directional.normalBias * _CascadeData[global.cascadeIndex + 1].y);
      		positionSTS = mul(
      			_DirectionalShadowMatrices[directional.tileIndex + 1],
      			float4(surfaceWS.position + normalBias, 1.0)
      		).xyz;
      		// 联级之间根据
      		shadow = lerp(
      			FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend
      		);
      	}
      ```

      

+ 在采样时添加抖动

  + surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0); //生成时间梯度噪声

+ 绘制 shadowMap 时不绘制在当前联级之外的物体 

  + ```
    splitData.shadowCascadeBlendCullingFactor = cullingFactor
    ```

  

### 阴影裁剪

+ ```
  // shadow caster 中裁剪
  #if defined(_SHADOWS_CLIP)
    		clip(base.r - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
  #endif
  ```
  
+ <img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281454867.png" alt="image-20231028143015906" style="zoom:33%;" />

  

<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image202310281454629.png" alt="image-20231028144052590" style="zoom:33%;" />





## 其他

### unity Input

+ buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);) 设置矩阵，然后unity Input把这个矩阵放到shader里面



### unity中real变量

+ unity core RP Pipline Library real -- 根据不同平台成为float或half，定义在com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl



### Premultiplied Alpha (Alpha预乘)

[Premultiplied Alpha到底是干嘛用的](https://blog.csdn.net/mydreamremindme/article/details/50817294)

[Premultiplied Alpha Tips](https://zhuanlan.zhihu.com/p/344751308)

实现

+ ```
  // alpha 预乘
    	if (applyAlphaToDiffuse) {
    			brdf.diffuse *= surface.alpha;
    		}
  ```



### unity default

`default` 值的含义取决于 `ShadowSettings` 类型的定义。如果 `ShadowSettings` 是一个自定义结构或类，那么它的默认值将取决于它的构造函数或字段默认值。这样，`shadows` 字段将在Inspector中显示，并且其初始值将设置为 `ShadowSettings` 类型的默认值。

