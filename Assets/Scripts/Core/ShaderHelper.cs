using UnityEngine;

namespace VoxelSouls.Core
{
    /// <summary>
    /// Helper tạo Material tự động tương thích với cả Built-in Render Pipeline và URP
    /// trên cả macOS (Metal) và Windows (DirectX/Vulkan), chống lỗi màu hồng (Pink Shader).
    /// </summary>
    public static class ShaderHelper
    {
        private static Shader _cachedLitShader;
        private static Shader _cachedUnlitShader;

        public static Shader GetLitShader()
        {
            if (_cachedLitShader != null) return _cachedLitShader;

            // Ưu tiên URP nếu project dùng Universal Render Pipeline
            _cachedLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_cachedLitShader == null)
                _cachedLitShader = Shader.Find("Standard");
            if (_cachedLitShader == null)
                _cachedLitShader = Shader.Find("Legacy Shaders/Diffuse");

            return _cachedLitShader;
        }

        public static Shader GetUnlitShader()
        {
            if (_cachedUnlitShader != null) return _cachedUnlitShader;

            _cachedUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_cachedUnlitShader == null)
                _cachedUnlitShader = Shader.Find("Unlit/Color");
            if (_cachedUnlitShader == null)
                _cachedUnlitShader = GetLitShader();

            return _cachedUnlitShader;
        }

        public static Material CreateMaterial(Color baseColor, float smoothness = 0.25f, float metallic = 0.1f, Color? emissionColor = null)
        {
            Material mat = new Material(GetLitShader());
            mat.color = baseColor;

            // Tương thích cả URP (_BaseColor) và Standard (_Color)
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", baseColor);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);

            if (emissionColor.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", emissionColor.Value);
            }

            return mat;
        }

        public static GameObject CreateVoxelPart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Color color, Color? emission = null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = localScale;

            Collider col = cube.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(col);
                else
                    Object.DestroyImmediate(col);
            }

            Renderer rend = cube.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = CreateMaterial(color, 0.2f, 0.15f, emission);
            }

            return cube;
        }
    }
}
