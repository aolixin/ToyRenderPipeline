# unity SRP

-- 敖立鑫

2024.8.8

[Custom Render Pipeline (catlikecoding.com)](https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/)  -- 参考这个教程

本次尝试着重关注渲染效果, 较少关注优雅的工程结构

初步目标:

1. 搭建基本管线
2. 实现 pbr
3. 实现 CSM
4. 实现软阴影

## srp执行命令流程

1. 用 context 直接执行

   + 可以直接执行一些命令如context.DrawSkybox(camera);

2. 用 buffer 间接执行
   1. 创建commandBuffer 
   2. 给 buffer 填充 command 如:buffer.ClearRenderTarget(true, true, Color.clear);
   3. 用 context（上下文）执行buffer
   4. 清除buffer

## 基本结构

引用 tutorial 的一段话, SRP 做了什么

- 管线画什么取决于 mesh
- 怎么画取决于 shader
  - 需要一些额外信息
  - object's transformation matrices
  - material properties.

SRP 主要是把这两个过程抽象出来, 使得用户可以编程这两个结构

一个类对应一个 pass, 例如:

camera render 对应 unlit pass

light 类对应 lit pass

shadow 类对应 shadow caster pass



## 管线搭建

### 基本结构

参考 catlike

RenderPipelineAsset --> create instance --> RenderPipline

RenderPipline override Render( ScriptableRenderContext, List< Camera > ) 函数

构建类 CameraRender 工具类, 提供 Render( ScriptableRenderContext, Camera ) 函数

主要渲染流程都在 CameraRender 内实现

tips: 如果要换一个渲染方式, 就换一个 CameraRender 类







### 基础绘制

```
    public class ToyCameraRenderer
    {
        private Camera _camera;
        private CommandBuffer buffer;
        
        public ToyCameraRenderer()
        {
            buffer = new CommandBuffer();
            buffer.name = "Render Camera";
        }
        public void Render(ScriptableRenderContext context, Camera camera,  ShadowSettings shadowSettings)
        {
            _camera = camera;
            context.SetupCameraProperties(_camera);
            
            buffer.ClearRenderTarget(true,true, Color.clear);
            context.ExecuteCommandBuffer(buffer);
            
            _camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);
            
            ShaderTagId shaderTagId = new ShaderTagId("gbuffer");
            var sortingSettings = new SortingSettings(_camera);
            var drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            var filteringSettings = FilteringSettings.defaultValue;
            
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            
            context.DrawSkybox(_camera);
            if (Handles.ShouldRenderGizmos()) 
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
            
            context.Submit();
        }
    }
```



<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240808162146481.png" alt="image-20240808162146481" style="zoom:50%;" />

## PBR

总共有两个 pass

1. gbuffer pass
2. lighting pass



gbuffer pass 在每个物体上, 将 pbr 参数输出到全局的纹理

lighting pass 相当于后处理阶段, 在一个全屏四边形上, 所以在 shader 中取到的 uv 也是全屏四边形的 uv

### Gbuffer

设计 gbuffer 格式

![img](https://pic1.zhimg.com/80/v2-9989a3487f30ea04b3e966d59243d7be_720w.webp?source=d16d100b)



### PRB 分解

<img src="https://pic2.zhimg.com/80/v2-3491b7ecd5be7defa078cd2dc9c14aa1_720w.png" alt="img" style="zoom: 67%;" />

PBR 可以先分解成直接光照和间接光照两部分

直接光和间接光又可以分成漫反射和高光反射

- 直接光照
  - 漫反射
    - 直接积分, 除以 PI
  - 镜面反射
    - 算 DGF
- 间接光照 IBL -- Image Based Lighting
  - 漫反射
    - 假设片元在天空盒中心, 所以近似为半球积分, 可以用发现采样
    - 预卷积天空盒
  - 镜面反射
    - 近似分为两部分积分
    - 第一部分: 因为是镜面反射, 所以采样要参考粗糙度, 于是用 mimap, 通过粗糙度对天空盒采样
    - 第二部分是对 brdf 积分得到 LUT, 这部分是固定的
    - LUT（Look up texture) — 根据 nv 和 粗糙度采样



### lighting pass

lighting pass 相当于后处理阶段, 这个阶段会用到一些世界空间下的坐标, 所以要在屏幕空间构建世界坐标

一种办法是通过 uv 和 depth 构建世界坐标 -- https://blog.csdn.net/yinfourever/article/details/120935179



### 着色效果

天空盒 hdr 来自 https://hdri-haven.com/

其中预卷积天空盒使用 cmftStudio 制作

<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240809164739678.png" alt="image-20240809164739678" style="zoom: 67%;" />



效果

build-in 管线

![image-20240809203802964](https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240809203802964.png)



ToyRenderPipline

![image-20240810005556455](https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240810005556455.png)
=======



## CSM

### 划分视椎体

1. 建立 CSM 分割 camera 视椎体
2. 求 AABB box, box 满足
   1. 包含子视椎体
   2. 始终和光源方向平行



-- 截图时为了清晰, 将 fov 调整到了 15

<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240815230649702.png" alt="image-20240815230649702" style="zoom: 50%;" />





### 绘制shadowmap

因为要用同一个 camera 绘制 gbuffer 和 shadowmap, 所以要绘制 shadowmap 前要把 camera 移动到光源的位置

绘制 map 之后还原

![image-20240816122647115](https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240816122647115.png)





### shadowmap 采样

在计算完直接光后对 shdowmap 采样

```
float ShadowMap01(float4 worldPos, sampler2D _shadowtex, float4x4 _shadowVpMatrix)
{
    float4 shadowNdc = mul(_shadowVpMatrix, worldPos);
    shadowNdc /= shadowNdc.w;
    float2 uv = shadowNdc.xy * 0.5 + 0.5;

    if(uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1) return 1.0f;

    float d = shadowNdc.z;
    float d_sample = tex2D(_shadowtex, uv).r;

    #if defined (UNITY_REVERSED_Z)
    if(d_sample>d) return 0.0f;
    #else
    if(d_sample<d) return 0.0f;
    #endif

    return 1.0f;
}
```

![image-20240816125701903](https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240816125701903.png)





## 软阴影

### PCF

PCF 原理 -- https://banbao991.github.io/2021/06/18/CG/Algorithm/SM-PCF-PCSS-VSM/#%E5%9C%BA%E6%99%AF%E8%AF%B4%E6%98%8E

添加 PCF3x3 阴影采样函数

```
float PCF3x3(float4 worldPos, sampler2D _shadowtex, float4x4 _shadowVpMatrix, float shadowMapResolution, float bias)
{
    ...

    float d_shadingPoint = shadowNdc.z;
    float shadow = 0.0;

    for(int i=-1; i<=1; i++)
    {
        for(int j=-1; j<=1; j++)
        {
            float2 offset = float2(i, j) / shadowMapResolution;
            float d_sample = tex2D(_shadowtex, uv+offset).r;

            #if defined (UNITY_REVERSED_Z)
            if(d_sample-bias>d_shadingPoint)
                #else
            if(d_sample<d_shadingPoint)
                #endif
            shadow += 1.0;
        }
    }
	...
}
```





可以看到效果还是很好的

<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240821121106995.png" alt="image-20240821121106995" style="zoom:80%;" />



