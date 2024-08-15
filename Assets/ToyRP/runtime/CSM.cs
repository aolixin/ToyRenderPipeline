using UnityEngine;
using UnityEngine.Rendering;

namespace ToyRP.runtime
{
    public class CSM
    {
        // 分割参数
        public float[] splts = { 0.07f, 0.13f, 0.25f, 0.55f };

        // 主相机视锥体
        Vector3[] farCorners = new Vector3[4];
        Vector3[] nearCorners = new Vector3[4];

        // 主相机划分四个视锥体
        Vector3[] f0_near = new Vector3[4], f0_far = new Vector3[4];
        Vector3[] f1_near = new Vector3[4], f1_far = new Vector3[4];
        Vector3[] f2_near = new Vector3[4], f2_far = new Vector3[4];
        Vector3[] f3_near = new Vector3[4], f3_far = new Vector3[4];

        private Vector3[] box0 = new Vector3[8];
        private Vector3[] box1 = new Vector3[8];
        private Vector3[] box2 = new Vector3[8];
        private Vector3[] box3 = new Vector3[8];

        // 齐次坐标矩阵乘法变换
        Vector3 matTransform(Matrix4x4 m, Vector3 v, float w)
        {
            Vector4 v4 = new Vector4(v.x, v.y, v.z, w);
            v4 = m * v4;
            return new Vector3(v4.x, v4.y, v4.z);
        }

        // 计算光源方向包围盒的世界坐标
        Vector3[] LightSpaceAABB(Vector3[] nearCorners, Vector3[] farCorners, Vector3 lightDir)
        {
            Matrix4x4 toShadowViewInv = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
            Matrix4x4 toShadowView = toShadowViewInv.inverse;

            // 视锥体顶点转光源方向
            for (int i = 0; i < 4; i++)
            {
                farCorners[i] = matTransform(toShadowView, farCorners[i], 1.0f);
                nearCorners[i] = matTransform(toShadowView, nearCorners[i], 1.0f);
            }

            // 计算 AABB 包围盒
            float[] x = new float[8];
            float[] y = new float[8];
            float[] z = new float[8];
            for (int i = 0; i < 4; i++)
            {
                x[i] = nearCorners[i].x;
                x[i + 4] = farCorners[i].x;
                y[i] = nearCorners[i].y;
                y[i + 4] = farCorners[i].y;
                z[i] = nearCorners[i].z;
                z[i + 4] = farCorners[i].z;
            }

            float xmin = Mathf.Min(x), xmax = Mathf.Max(x);
            float ymin = Mathf.Min(y), ymax = Mathf.Max(y);
            float zmin = Mathf.Min(z), zmax = Mathf.Max(z);

            // 包围盒顶点转世界坐标
            Vector3[] points =
            {
                new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmin),
                new Vector3(xmin, ymax, zmax),
                new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmin),
                new Vector3(xmax, ymax, zmax)
            };
            for (int i = 0; i < 8; i++)
                points[i] = matTransform(toShadowViewInv, points[i], 1.0f);

            // 视锥体顶还原
            for (int i = 0; i < 4; i++)
            {
                farCorners[i] = matTransform(toShadowViewInv, farCorners[i], 1.0f);
                nearCorners[i] = matTransform(toShadowViewInv, nearCorners[i], 1.0f);
            }

            return points;
        }
        
        // 用主相机和光源方向更新 CSM 划分
        public void Update(Camera mainCam, Vector3 lightDir)
        {
            // 获取主相机视锥体
            mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farCorners);
            mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearCorners);

            // 视锥体顶点转世界坐标
            for (int i = 0; i < 4; i++)
            {
                farCorners[i] = mainCam.transform.TransformVector(farCorners[i]) + mainCam.transform.position;
                nearCorners[i] = mainCam.transform.TransformVector(nearCorners[i]) + mainCam.transform.position;
            }

            // 按照比例划分相机视锥体
            for(int i=0; i<4; i++)
            {
                Vector3 dir = farCorners[i] - nearCorners[i];

                f0_near[i] = nearCorners[i];
                f0_far[i] = f0_near[i] + dir * splts[0];

                f1_near[i] = f0_far[i];
                f1_far[i] = f1_near[i] + dir * splts[1];

                f2_near[i] = f1_far[i];
                f2_far[i] = f2_near[i] + dir * splts[2];

                f3_near[i] = f2_far[i];
                f3_far[i] = f3_near[i] + dir * splts[3];
            }

            // 计算包围盒
            box0 = LightSpaceAABB(f0_near, f0_far, lightDir);
            box1 = LightSpaceAABB(f1_near, f1_far, lightDir);
            box2 = LightSpaceAABB(f2_near, f2_far, lightDir);
            box3 = LightSpaceAABB(f3_near, f3_far, lightDir);
        }
        
        // 画相机视锥体
        void DrawFrustum(Vector3[] nearCorners, Vector3[] farCorners, Color color)
        {
            for (int i = 0; i < 4; i++)
                Debug.DrawLine(nearCorners[i], farCorners[i], color);

            Debug.DrawLine(farCorners[0], farCorners[1], color);
            Debug.DrawLine(farCorners[0], farCorners[3], color);
            Debug.DrawLine(farCorners[2], farCorners[1], color);
            Debug.DrawLine(farCorners[2], farCorners[3], color);
            Debug.DrawLine(nearCorners[0], nearCorners[1], color);
            Debug.DrawLine(nearCorners[0], nearCorners[3], color);
            Debug.DrawLine(nearCorners[2], nearCorners[1], color);
            Debug.DrawLine(nearCorners[2], nearCorners[3], color);
        }

// 画光源方向的 AABB 包围盒
        void DrawAABB(Vector3[] points, Color color)
        {
            // 画线
            Debug.DrawLine(points[0], points[1], color);
            Debug.DrawLine(points[0], points[2], color);
            Debug.DrawLine(points[0], points[4], color);

            Debug.DrawLine(points[6], points[2], color);
            Debug.DrawLine(points[6], points[7], color);
            Debug.DrawLine(points[6], points[4], color);

            Debug.DrawLine(points[5], points[1], color);
            Debug.DrawLine(points[5], points[7], color);
            Debug.DrawLine(points[5], points[4], color);

            Debug.DrawLine(points[3], points[1], color);
            Debug.DrawLine(points[3], points[2], color);
            Debug.DrawLine(points[3], points[7], color);
        }

        public void DebugDraw()
        {
            DrawFrustum(nearCorners, farCorners, Color.white);
            DrawAABB(box0, Color.yellow);  
            DrawAABB(box1, Color.magenta);
            DrawAABB(box2, Color.green);
            DrawAABB(box3, Color.cyan);
        }
    }
}