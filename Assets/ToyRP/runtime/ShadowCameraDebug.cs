using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToyRP.runtime
{
    
    // 这个脚本挂相机上测试
    [ExecuteAlways]
    public class ShadowCameraDebug : MonoBehaviour
    {
        CSM csm;
        public Camera camera;

        void Update()
        {
            // 获取光源信息
            Light light = RenderSettings.sun;
            Vector3 lightDir = light.transform.rotation * Vector3.forward;

            // 更新 shadowmap
            if (csm == null) csm = new CSM();
            csm.Update(camera, lightDir);
            csm.DebugDraw();
        }
    }
}