using System.IO;
using UnityEngine;
using UnityEditor;

namespace Aiara.EditorTools
{
    /// <summary>
    /// Sinh bộ Sprite UI phong cách thư pháp cọ mực (Ink Brush) khớp giao diện trong Guide Video 1, 2, 3:
    /// - UI_Avatar_Ring.png: Vòng tròn viền cọ mực đen
    /// - UI_Avatar_Portrait.png: Chân dung 3D nhân vật JangHyu
    /// - UI_Brush_NameSplash.png: Vệt mực đen kéo dài phía sau tên nhân vật / tên quái vật
    /// - UI_Brush_BarBg.png: Khung viền nét mực cho thanh HP / MP / EXP
    /// - UI_Brush_BarFill.png: Lõi thanh trượt nét cọ mực có mép xước tự nhiên
    /// </summary>
    public static class CreateUiBrushSprites
    {
        public const string SpriteFolder = "Assets/Resources/UI/Sprites";

        [MenuItem("Tools/StreetFighter/Generate Brush UI Sprites")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(SpriteFolder))
            {
                Directory.CreateDirectory(SpriteFolder);
            }

            CreateAvatarRingTexture(Path.Combine(SpriteFolder, "UI_Avatar_Ring.png"));
            CreateNameSplashTexture(Path.Combine(SpriteFolder, "UI_Brush_NameSplash.png"));
            CreateBarBgTexture(Path.Combine(SpriteFolder, "UI_Brush_BarBg.png"));
            CreateBarFillTexture(Path.Combine(SpriteFolder, "UI_Brush_BarFill.png"));
            CreateAvatarPortraitTexture(Path.Combine(SpriteFolder, "UI_Avatar_Portrait.png"));

            AssetDatabase.Refresh();

            ConfigureSpriteImport(Path.Combine(SpriteFolder, "UI_Avatar_Ring.png"), Vector4.zero);
            ConfigureSpriteImport(Path.Combine(SpriteFolder, "UI_Avatar_Portrait.png"), Vector4.zero);
            ConfigureSpriteImport(Path.Combine(SpriteFolder, "UI_Brush_NameSplash.png"), Vector4.zero);
            ConfigureSpriteImport(Path.Combine(SpriteFolder, "UI_Brush_BarBg.png"), new Vector4(14f, 4f, 14f, 4f));
            ConfigureSpriteImport(Path.Combine(SpriteFolder, "UI_Brush_BarFill.png"), Vector4.zero);

            AssetDatabase.Refresh();
            Debug.Log("[CreateUiBrushSprites] Đã tạo thành công bộ Sprite UI phong cách cọ mực giống video!");
        }

        private static void ConfigureSpriteImport(string assetPath, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (border != Vector4.zero)
            {
                importer.spriteBorder = border;
            }
            importer.SaveAndReimport();
        }

        private static float HashNoise(float x, float y)
        {
            return Mathf.PerlinNoise(x * 0.13f + 17.3f, y * 0.13f + 31.7f);
        }

        private static void CreateAvatarRingTexture(string path)
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
                    float angle = Mathf.Atan2(dy, dx);

                    float brushWobble = (Mathf.PerlinNoise(Mathf.Cos(angle) * 4f + 10f, Mathf.Sin(angle) * 4f + 10f) - 0.5f) * 0.085f;
                    float fineGrain = HashNoise(x * 1.8f, y * 1.8f);

                    float innerR = 0.78f + brushWobble * 0.5f;
                    float outerR = 0.96f + brushWobble;

                    if (dist >= innerR && dist <= outerR)
                    {
                        float edgeIn = Mathf.Clamp01((dist - innerR) / 0.04f);
                        float edgeOut = Mathf.Clamp01((outerR - dist) / 0.045f);
                        float alpha = Mathf.Min(edgeIn, edgeOut);
                        if (dist > outerR - 0.04f && fineGrain < 0.42f) alpha *= 0.35f;

                        // Viền sáng mảnh ở rìa trong giống khung cọ mực trong video
                        bool isInnerRim = dist < innerR + 0.035f;
                        Color c = isInnerRim ? new Color(0.88f, 0.9f, 0.88f, alpha * 0.9f) : new Color(0.05f, 0.05f, 0.06f, alpha);
                        tex.SetPixel(x, y, c);
                    }
                    else if (dist < innerR)
                    {
                        // Nền trong suốt để lồng Avatar Portrait ở layer dưới
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateNameSplashTexture(string path)
        {
            int w = 512;
            int h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float n1 = Mathf.PerlinNoise(nx * 9f + 2.1f, ny * 3.5f + 4.7f);
                    float n2 = Mathf.PerlinNoise(nx * 24f + 8.3f, ny * 12f + 1.4f);

                    // Đậm đặc ở phía trái (0..0.55), xước đuôi chổi cọ nhọn dần từ 0.55..1.0
                    float verticalHalfWidth = Mathf.Lerp(0.82f, 0.16f, Mathf.Pow(nx, 1.4f)) + (n1 - 0.5f) * 0.25f;
                    float distY = Mathf.Abs(ny);

                    if (distY < verticalHalfWidth && nx < 0.98f)
                    {
                        float alphaY = Mathf.Clamp01((verticalHalfWidth - distY) / 0.14f);
                        float tailMask = Mathf.SmoothStep(0.32f, 0.72f, nx);
                        float rawStreak = Mathf.Clamp01((0.97f - nx) / 0.55f + (n2 - 0.45f) * 0.58f);
                        float streakFade = Mathf.Lerp(1f, rawStreak, tailMask);
                        float leftFade = Mathf.Clamp01(nx / 0.03f);
                        float brushTeeth = (distY > verticalHalfWidth * 0.68f && n2 < 0.48f) ? 0.25f : 1f;

                        float alpha = Mathf.Clamp01(alphaY * streakFade * leftFade * brushTeeth) * 0.92f;
                        tex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.06f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateBarBgTexture(string path)
        {
            int w = 256;
            int h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float noise = Mathf.PerlinNoise(nx * 14f + 3f, ny * 7f + 5f);
                    float fine = Mathf.PerlinNoise(nx * 35f, ny * 20f);

                    // Góc vát xước nhẹ ở đầu phải giống nét cọ quét ngang
                    float rightCutoff = 0.96f - Mathf.Abs(ny) * 0.04f + (noise - 0.5f) * 0.03f;
                    float leftCutoff = 0.02f + (noise - 0.5f) * 0.015f;
                    float yLimit = 0.88f + (noise - 0.5f) * 0.12f;

                    if (nx >= leftCutoff && nx <= rightCutoff && Mathf.Abs(ny) <= yLimit)
                    {
                        bool isBorder = (Mathf.Abs(ny) > yLimit - 0.16f) || (nx < leftCutoff + 0.025f) || (nx > rightCutoff - 0.025f);
                        float edgeAlpha = Mathf.Clamp01((yLimit - Mathf.Abs(ny)) / 0.08f);
                        if (fine < 0.3f && Mathf.Abs(ny) > 0.72f) edgeAlpha *= 0.4f;

                        Color c = isBorder
                            ? new Color(0.04f, 0.04f, 0.05f, 0.95f * edgeAlpha)
                            : new Color(0.10f, 0.10f, 0.12f, 0.82f * edgeAlpha);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateBarFillTexture(string path)
        {
            int w = 256;
            int h = 40;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float streak = Mathf.PerlinNoise(nx * 10f + 1.2f, ny * 6f + 9.4f);

                    // Gradient trắng sáng nhẹ ở giữa để nhân với Image.color vẫn giữ đúng sắc nét màu xanh/đỏ/vàng
                    float centerHighlight = 1f - Mathf.Abs(ny) * 0.18f;
                    float v = Mathf.Clamp01(centerHighlight * (0.9f + streak * 0.15f));

                    float alpha = 1f;
                    if (Mathf.Abs(ny) > 0.85f) alpha = Mathf.Clamp01((1f - Mathf.Abs(ny)) / 0.15f);
                    if (nx > 0.96f) alpha *= Mathf.Clamp01((1f - nx) / 0.04f + (streak - 0.4f) * 0.4f);

                    tex.SetPixel(x, y, new Color(v, v, v, Mathf.Clamp01(alpha)));
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateAvatarPortraitTexture(string path)
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            var player = GameObject.Find("JangHyu_AttackCombo2") ?? GameObject.FindWithTag("Player");
            bool capturedFromModel = false;

            if (player != null)
            {
                // Tìm xương đầu của JangHyu để chụp chân dung thật
                Transform headBone = null;
                foreach (var tr in player.GetComponentsInChildren<Transform>(true))
                {
                    string n = tr.name.ToLower();
                    if (n.Contains("head") && !n.Contains("end") && !n.Contains("weapon"))
                    {
                        headBone = tr;
                        break;
                    }
                }

                if (headBone != null)
                {
                    var camGo = new GameObject("TempPortraitCam");
                    var cam = camGo.AddComponent<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.08f, 0.12f, 0.13f, 1f);
                    cam.fieldOfView = 26f;
                    cam.nearClipPlane = 0.05f;
                    cam.farClipPlane = 5f;

                    Vector3 headCenter = headBone.position + Vector3.up * 0.04f;
                    Vector3 lookDir = (player.transform.forward * 0.85f + player.transform.right * 0.35f).normalized;
                    cam.transform.position = headCenter + lookDir * 0.82f - Vector3.up * 0.05f;
                    cam.transform.LookAt(headCenter);

                    var rt = new RenderTexture(size, size, 24);
                    var prevActive = RenderTexture.active;
                    cam.targetTexture = rt;
                    cam.Render();

                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    tex.Apply();

                    RenderTexture.active = prevActive;
                    cam.targetTexture = null;
                    Object.DestroyImmediate(rt);
                    Object.DestroyImmediate(camGo);
                    capturedFromModel = true;
                }
            }

            if (!capturedFromModel)
            {
                // Fallback gradient chân dung nếu chưa tìm thấy model
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        tex.SetPixel(x, y, new Color(0.14f, 0.38f, 0.35f, 1f));
                    }
                }
            }

            // Cắt mặt nạ hình tròn vừa vặn với UI_Avatar_Ring
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
                    Color c = tex.GetPixel(x, y);
                    if (dist > 0.84f)
                    {
                        c.a = 0f;
                    }
                    else if (dist > 0.78f)
                    {
                        c.a *= Mathf.Clamp01((0.84f - dist) / 0.06f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
