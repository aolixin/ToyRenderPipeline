# unity SRP

-- 敖立鑫

2024.8.8

[Custom Render Pipeline (catlikecoding.com)](https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/)  -- 参考这个教程

本次尝试着重关注渲染效果, 较少关注优雅的工程结构

初步目标:

1. 搭建基本管线
2. 实现 pbr
3. 实现 CSM

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
    - 预卷积天空盒
    - 处理成球谐函数 — 以 rgb 为为输出, xyz 为输入的函数
    - 基底是 以 rgb 为为输出, xyz 为输入的函数
  - 镜面反射
    - LOD
    - 将天空盒搞成 mipmap
    - 不同粗糙度在不同的 step 采样
    - LUT（Look up texture) — 根据 nv 和 粗糙度采样



天空盒 hdr 来自 https://hdri-haven.com/

其中预卷积天空盒使用 cmftStudio 制作

<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240809164739678.png" alt="image-20240809164739678" style="zoom: 67%;" />



### 效果

<img src="https://aolixin-typora-image.oss-cn-beijing.aliyuncs.com/image-20240809164312736.png" alt="image-20240809164312736" style="zoom:67%;" />
