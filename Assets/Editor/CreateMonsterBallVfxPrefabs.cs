using System.IO;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor;
using Aiara.Combat;

namespace Aiara.EditorTools
{
    [InitializeOnLoad]
    public static class CreateMonsterBallVfxPrefabs
    {
        private const string TargetDir = "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs";

        static CreateMonsterBallVfxPrefabs()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(TargetDir + "/VFX_GroundCrack_Complete.prefab") ||
                    !File.Exists(TargetDir + "/VFX_GroundErupt_Complete.prefab") ||
                    !File.Exists(TargetDir + "/VFX_RollTrail_Complete.prefab") ||
                    !File.Exists(TargetDir + "/VFX_JumpTrail_Complete.prefab") ||
                    !File.Exists(TargetDir + "/VFX_BombExplosion_Complete.prefab") ||
                    !File.Exists(TargetDir + "/Clean_Flame_VFX.prefab"))
                {
                    BuildAllVfxPrefabs();
                }
            };
        }

        [MenuItem("Tools/StreetFighter/Build All MonsterBall VFX Prefabs")]
        public static void BuildAllVfxPrefabs()
        {
            if (!Directory.Exists(TargetDir))
            {
                Directory.CreateDirectory(TargetDir);
            }

            var crackMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/4. Rendering/Materials/VFX_CrackMat.mat");
            var debrisMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/4. Rendering/Materials/VFX_DebrisMat.mat");
            var flameMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/4. Rendering/Materials/VFX_FlameMat.mat");
            var smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/4. Rendering/Materials/VFX_SmokeMat.mat");

            // Chuẩn hóa VFX_FlameMat dùng uni_glow.png + Additive (đốm lửa tròn phát sáng, không nhân màu quá mức làm cháy trắng)
            var glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UNI VFX/Common/Textures/uni_glow.png");
            if (flameMat != null)
            {
                if (glowTex != null)
                {
                    flameMat.SetTexture("_BaseMap", glowTex);
                    flameMat.mainTexture = glowTex;
                }
                flameMat.SetColor("_BaseColor", Color.white);
                flameMat.SetColor("_Color", Color.white);
                flameMat.SetColor("_EmissionColor", Color.black);
                flameMat.SetFloat("_Surface", 1f);
                flameMat.SetFloat("_Blend", 2f); // 2 = Additive
                flameMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                flameMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                flameMat.SetFloat("_ZWrite", 0f);
                flameMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                flameMat.DisableKeyword("_ALPHAMODULATE_ON");
                flameMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                flameMat.EnableKeyword("_BLENDMODE_ADD");
                flameMat.renderQueue = 3000;
                EditorUtility.SetDirty(flameMat);
            }

            // Chuẩn hóa VFX_SmokeMat dùng WispySmoke03b_8x8.png + Alpha blend màu trắng gốc (không làm mờ khói đen)
            var wispySmoke = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UNI VFX/Common/Textures/WispySmoke03b_8x8.png");
            if (smokeMat != null && wispySmoke != null)
            {
                smokeMat.SetTexture("_BaseMap", wispySmoke);
                smokeMat.mainTexture = wispySmoke;
                smokeMat.SetColor("_BaseColor", Color.white);
                smokeMat.SetColor("_Color", Color.white);
                smokeMat.SetColor("_EmissionColor", Color.black);
                smokeMat.SetFloat("_Surface", 1f);
                smokeMat.SetFloat("_Blend", 0f); // Alpha blend
                smokeMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                smokeMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                smokeMat.SetFloat("_ZWrite", 0f);
                smokeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                smokeMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                smokeMat.DisableKeyword("_BLENDMODE_ADD");
                smokeMat.renderQueue = 2990;
                EditorUtility.SetDirty(smokeMat);
            }

            // Tạo/chuẩn hóa VFX_FirePlumeMat (Additive trên WispySmoke03b_8x8.png) để tạo ngọn lửa cuộn sắc nét cho quả bom nổ
            string firePlumeMatPath = "Assets/Game/4. Rendering/Materials/VFX_FirePlumeMat.mat";
            var firePlumeMat = AssetDatabase.LoadAssetAtPath<Material>(firePlumeMatPath);
            if (firePlumeMat == null && flameMat != null)
            {
                firePlumeMat = new Material(flameMat);
                AssetDatabase.CreateAsset(firePlumeMat, firePlumeMatPath);
            }
            if (firePlumeMat != null && wispySmoke != null)
            {
                if (flameMat != null) firePlumeMat.shader = flameMat.shader;
                firePlumeMat.SetTexture("_BaseMap", wispySmoke);
                firePlumeMat.mainTexture = wispySmoke;
                firePlumeMat.SetColor("_BaseColor", Color.white);
                firePlumeMat.SetColor("_Color", Color.white);
                firePlumeMat.SetColor("_EmissionColor", Color.black);
                firePlumeMat.SetFloat("_Surface", 1f);
                firePlumeMat.SetFloat("_Blend", 2f);
                firePlumeMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                firePlumeMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                firePlumeMat.SetFloat("_ZWrite", 0f);
                firePlumeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                firePlumeMat.EnableKeyword("_BLENDMODE_ADD");
                firePlumeMat.renderQueue = 3010;
                EditorUtility.SetDirty(firePlumeMat);
            }

            // Chuẩn hóa VFX_DebrisMat cho 3D Mesh sỏi đá
            if (debrisMat != null)
            {
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null) debrisMat.shader = urpLit;
                debrisMat.mainTexture = null;
                debrisMat.SetColor("_BaseColor", new Color(0.22f, 0.16f, 0.12f, 1f));
                debrisMat.SetFloat("_Smoothness", 0.1f);
                debrisMat.renderQueue = 2000;
                EditorUtility.SetDirty(debrisMat);
            }

            BuildGroundCrackPrefab(crackMat, smokeMat);
            BuildGroundEruptPrefab(smokeMat, debrisMat, flameMat);
            BuildRollTrailPrefab(smokeMat, flameMat);
            BuildJumpTrailPrefab(smokeMat, flameMat);
            BuildBombExplosionPrefab(smokeMat, debrisMat, flameMat, firePlumeMat ?? flameMat);
            BuildCleanFlamePrefab(flameMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateMonsterBallVfxPrefabs] Đã tạo thành công toàn bộ Prefab VFX chuẩn hóa cho MonsterBall!");
        }

        private static void BuildGroundCrackPrefab(Material crackMat, Material smokeMat)
        {
            string path = TargetDir + "/VFX_GroundCrack_Complete.prefab";
            var root = new GameObject("VFX_GroundCrack_Complete");

            // Khói bụi rỉ nhẹ từ dưới mặt đất quanh 2 sừng nhô lên (giống Guide video 3)
            var smokeGo = new GameObject("Crack_SeepSmoke");
            smokeGo.transform.SetParent(root.transform, false);
            smokeGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            smokeGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var ps = smokeGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
            main.startColor = new Color(0.65f, 0.65f, 0.68f, 0.25f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.4f));

            var colMod = ps.colorOverLifetime;
            colMod.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.75f, 0.75f, 0.78f), 0f), new GradientColorKey(new Color(0.45f, 0.45f, 0.48f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.28f, 0.25f), new GradientAlphaKey(0f, 1f) }
            );
            colMod.color = grad;

            EnableSmokeSpritesheet8x8(ps);

            var psr = smokeGo.GetComponent<ParticleSystemRenderer>();
            if (psr != null && smokeMat != null)
            {
                psr.sharedMaterial = smokeMat;
            }

            SaveAndDestroy(root, path);
        }

        private static void EnableSmokeSpritesheet8x8(ParticleSystem ps)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = 8;
            tsa.numTilesY = 8;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f, new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 0.85f)));
            tsa.cycleCount = 1;
        }

        private static void BuildGroundEruptPrefab(Material smokeMat, Material debrisMat, Material flameMat)
        {
            string path = TargetDir + "/VFX_GroundErupt_Complete.prefab";
            var root = new GameObject("VFX_GroundErupt_Complete");

            // 1. Cột khói đen bùng nổ vọt lên
            var smokeGo = new GameObject("Erupt_SmokePuff");
            smokeGo.transform.SetParent(root.transform, false);
            smokeGo.transform.localPosition = Vector3.zero;
            smokeGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var psSmoke = smokeGo.AddComponent<ParticleSystem>();
            var mSmoke = psSmoke.main;
            mSmoke.duration = 1.0f;
            mSmoke.loop = false;
            mSmoke.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.6f);
            mSmoke.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            mSmoke.startSize = new ParticleSystem.MinMaxCurve(1.4f, 2.6f);
            mSmoke.startColor = new Color(0.16f, 0.16f, 0.18f, 0.85f);
            mSmoke.simulationSpace = ParticleSystemSimulationSpace.World;

            var eSmoke = psSmoke.emission;
            eSmoke.rateOverTime = 0f;
            eSmoke.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var sSmoke = psSmoke.shape;
            sSmoke.shapeType = ParticleSystemShapeType.Cone;
            sSmoke.angle = 20f;
            sSmoke.radius = 0.4f;

            var solSmoke = psSmoke.sizeOverLifetime;
            solSmoke.enabled = true;
            solSmoke.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.8f));

            var colSmoke = psSmoke.colorOverLifetime;
            colSmoke.enabled = true;
            var gradSmoke = new Gradient();
            gradSmoke.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.18f, 0.18f, 0.18f), 0f), new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0.6f, 0.35f), new GradientAlphaKey(0f, 1f) }
            );
            colSmoke.color = gradSmoke;

            EnableSmokeSpritesheet8x8(psSmoke);
            var psrSmoke = smokeGo.GetComponent<ParticleSystemRenderer>();
            if (psrSmoke != null && smokeMat != null) psrSmoke.sharedMaterial = smokeMat;

            // 2. Mảnh đất đá văng tung tóe (3D Mesh sỏi đá thật)
            var debrisGo = new GameObject("Erupt_DebrisBurst");
            debrisGo.transform.SetParent(root.transform, false);
            debrisGo.transform.localPosition = Vector3.zero;
            debrisGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var psDebris = debrisGo.AddComponent<ParticleSystem>();
            var mDebris = psDebris.main;
            mDebris.duration = 1.0f;
            mDebris.loop = false;
            mDebris.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
            mDebris.startSpeed = new ParticleSystem.MinMaxCurve(5.0f, 9.0f);
            mDebris.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            mDebris.startRotation3D = true;
            mDebris.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            mDebris.startRotationY = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            mDebris.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            mDebris.gravityModifier = 1.6f;
            mDebris.simulationSpace = ParticleSystemSimulationSpace.World;

            var eDebris = psDebris.emission;
            eDebris.rateOverTime = 0f;
            eDebris.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });

            var sDebris = psDebris.shape;
            sDebris.shapeType = ParticleSystemShapeType.Cone;
            sDebris.angle = 45f;
            sDebris.radius = 0.3f;

            var psrDebris = debrisGo.GetComponent<ParticleSystemRenderer>();
            if (psrDebris != null)
            {
                psrDebris.renderMode = ParticleSystemRenderMode.Mesh;
                psrDebris.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                if (debrisMat != null) psrDebris.sharedMaterial = debrisMat;
            }

            // 3. Tia lửa phun từ lòng đất
            var sparkGo = new GameObject("Erupt_FireSparks");
            sparkGo.transform.SetParent(root.transform, false);
            sparkGo.transform.localPosition = Vector3.zero;
            sparkGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var psSpark = sparkGo.AddComponent<ParticleSystem>();
            var mSpark = psSpark.main;
            mSpark.duration = 0.8f;
            mSpark.loop = false;
            mSpark.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            mSpark.startSpeed = new ParticleSystem.MinMaxCurve(4.0f, 8.0f);
            mSpark.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f); // Tia lửa nhỏ sáng
            mSpark.gravityModifier = 0.5f;
            mSpark.simulationSpace = ParticleSystemSimulationSpace.World;

            var eSpark = psSpark.emission;
            eSpark.rateOverTime = 0f;
            eSpark.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var sSpark = psSpark.shape;
            sSpark.shapeType = ParticleSystemShapeType.Cone;
            sSpark.angle = 30f;
            sSpark.radius = 0.2f;

            var colSpark = psSpark.colorOverLifetime;
            colSpark.enabled = true;
            var gradSpark = new Gradient();
            gradSpark.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.7f, 0.15f), 0f), new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colSpark.color = gradSpark;

            var psrSpark = sparkGo.GetComponent<ParticleSystemRenderer>();
            if (psrSpark != null && flameMat != null) psrSpark.sharedMaterial = flameMat;

            SaveAndDestroy(root, path);
        }

        private static void AddFlameTrailEmitter(
            GameObject root, Material flameMat, string name, Vector3 localPos,
            float rateOverTime, float rateOverDistance, float minSize, float maxSize, float radius)
        {
            var fireGo = new GameObject(name);
            fireGo.transform.SetParent(root.transform, false);
            fireGo.transform.localPosition = localPos;
            var psFire = fireGo.AddComponent<ParticleSystem>();
            var mFire = psFire.main;
            mFire.duration = 1.0f;
            mFire.loop = true;
            mFire.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
            mFire.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.4f);
            mFire.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            mFire.startColor = new ParticleSystem.MinMaxGradient(new Color(3.2f, 1.15f, 0.2f, 0.95f));
            mFire.gravityModifier = -0.35f;
            mFire.simulationSpace = ParticleSystemSimulationSpace.World;

            var eFire = psFire.emission;
            eFire.rateOverTime = rateOverTime;
            eFire.rateOverDistance = rateOverDistance;

            var sFire = psFire.shape;
            sFire.shapeType = ParticleSystemShapeType.Sphere;
            sFire.radius = radius;

            var solFire = psFire.sizeOverLifetime;
            solFire.enabled = true;
            solFire.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1.0f, 1f, 0.15f));

            var colFire = psFire.colorOverLifetime;
            colFire.enabled = true;
            var gradFire = new Gradient();
            gradFire.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.85f, 0.25f), 0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.55f),
                    new GradientColorKey(new Color(0.55f, 0.06f, 0.01f), 1f)
                },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.75f, 0.45f), new GradientAlphaKey(0f, 1f) }
            );
            colFire.color = gradFire;

            var psrFire = fireGo.GetComponent<ParticleSystemRenderer>();
            if (psrFire != null && flameMat != null) psrFire.sharedMaterial = flameMat;
        }

        private static void BuildRollTrailPrefab(Material smokeMat, Material flameMat)
        {
            string path = TargetDir + "/VFX_RollTrail_Complete.prefab";
            var root = new GameObject("VFX_RollTrail_Complete");

            // 1. Vệt khói cuộn dưới sàn
            var smokeGo = new GameObject("Roll_SmokeTrail");
            smokeGo.transform.SetParent(root.transform, false);
            smokeGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            var psSmoke = smokeGo.AddComponent<ParticleSystem>();
            var mSmoke = psSmoke.main;
            mSmoke.duration = 1.0f;
            mSmoke.loop = true;
            mSmoke.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            mSmoke.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            mSmoke.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            mSmoke.startColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            mSmoke.simulationSpace = ParticleSystemSimulationSpace.World;

            var eSmoke = psSmoke.emission;
            eSmoke.rateOverTime = 20f;
            eSmoke.rateOverDistance = 4f;

            var sSmoke = psSmoke.shape;
            sSmoke.shapeType = ParticleSystemShapeType.Sphere;
            sSmoke.radius = 0.4f;

            var solSmoke = psSmoke.sizeOverLifetime;
            solSmoke.enabled = true;
            solSmoke.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.6f));

            var colSmoke = psSmoke.colorOverLifetime;
            colSmoke.enabled = true;
            var gradSmoke = new Gradient();
            gradSmoke.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.22f, 0.22f, 0.22f), 0f), new GradientColorKey(new Color(0.12f, 0.12f, 0.12f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colSmoke.color = gradSmoke;

            EnableSmokeSpritesheet8x8(psSmoke);
            var psrSmoke = smokeGo.GetComponent<ParticleSystemRenderer>();
            if (psrSmoke != null && smokeMat != null) psrSmoke.sharedMaterial = smokeMat;

            // 2. Tia lửa ma sát với mặt đất
            var sparkGo = new GameObject("Roll_FrictionSparks");
            sparkGo.transform.SetParent(root.transform, false);
            sparkGo.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var psSpark = sparkGo.AddComponent<ParticleSystem>();
            var mSpark = psSpark.main;
            mSpark.duration = 1.0f;
            mSpark.loop = true;
            mSpark.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            mSpark.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.0f);
            mSpark.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.15f);
            mSpark.gravityModifier = 0.8f;
            mSpark.simulationSpace = ParticleSystemSimulationSpace.World;

            var eSpark = psSpark.emission;
            eSpark.rateOverTime = 25f;
            eSpark.rateOverDistance = 6f;

            var sSpark = psSpark.shape;
            sSpark.shapeType = ParticleSystemShapeType.Sphere;
            sSpark.radius = 0.35f;

            var colSpark = psSpark.colorOverLifetime;
            colSpark.enabled = true;
            var gradSpark = new Gradient();
            gradSpark.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.75f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colSpark.color = gradSpark;

            var psrSpark = sparkGo.GetComponent<ParticleSystemRenderer>();
            if (psrSpark != null && flameMat != null) psrSpark.sharedMaterial = flameMat;

            // 3. Trail lửa cam đỏ bùng cháy dọc đường di chuyển / lăn
            AddFlameTrailEmitter(root, flameMat, "Roll_FlameTrail", new Vector3(0f, 0.42f, 0f), 26f, 14f, 0.55f, 1.05f, 0.38f);

            SaveAndDestroy(root, path);
        }

        private static void BuildCleanFlamePrefab(Material flameMat)
        {
            string path = TargetDir + "/Clean_Flame_VFX.prefab";
            var root = new GameObject("Clean_Flame_VFX");
            root.AddComponent<SkinnedMeshVfxBinder>();

            // 1. Trích xuất chính xác VisualEffect từ Destructive Elements Flame Skinned Mesh (giữ nguyên toàn bộ HDR ColorOverLife & tham số gốc, loại bỏ hoàn toàn Ellen & binder cũ)
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UNI VFX/Characters & Artifacts/Prefabs/Destructive Elements Flame Skinned Mesh.prefab");
            if (sourcePrefab != null)
            {
                var temp = Object.Instantiate(sourcePrefab);
                var srcVfx = temp.GetComponentInChildren<VisualEffect>(true);
                if (srcVfx != null)
                {
                    var vfxGo = srcVfx.gameObject;
                    vfxGo.transform.SetParent(root.transform, false);
                    vfxGo.name = "UNI_Flame_VisualEffect";
                    vfxGo.transform.localPosition = Vector3.zero;
                    vfxGo.transform.localRotation = Quaternion.identity;
                    vfxGo.transform.localScale = Vector3.one;

                    // Xóa mọi component phụ thuộc vào Ellen theo đúng thứ tự dependency (BinderBase trước, VFXPropertyBinder sau)
                    var allMbs = vfxGo.GetComponents<MonoBehaviour>();
                    foreach (var mb in allMbs)
                    {
                        if (mb != null && mb.GetType().Name.Contains("Binder") && mb.GetType().Name != "VFXPropertyBinder")
                        {
                            Object.DestroyImmediate(mb);
                        }
                    }
                    foreach (var mb in vfxGo.GetComponents<MonoBehaviour>())
                    {
                        if (mb != null) Object.DestroyImmediate(mb);
                    }
                    srcVfx.initialEventName = "loop";
                    if (srcVfx.HasSkinnedMeshRenderer("SkinnedMesh"))
                    {
                        srcVfx.SetSkinnedMeshRenderer("SkinnedMesh", null);
                    }
                    if (srcVfx.HasTexture("VFXMeshMask"))
                    {
                        srcVfx.SetTexture("VFXMeshMask", Texture2D.whiteTexture);
                    }
                }
                Object.DestroyImmediate(temp);
            }
            else
            {
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(
                    "Assets/UNI VFX/Characters & Artifacts/Visual Effects/UNI_destructive_elements.vfx");
                if (vfxAsset != null)
                {
                    var vfxGo = new GameObject("UNI_Flame_VisualEffect");
                    vfxGo.transform.SetParent(root.transform, false);
                    var vfx = vfxGo.AddComponent<VisualEffect>();
                    vfx.visualEffectAsset = vfxAsset;
                    vfx.initialEventName = "loop";
                }
            }

            // 2. Tia lửa lấp lánh (Flame Sparks) bổ sung
            var sparkGo = new GameObject("BodyFlame_Sparks");
            sparkGo.transform.SetParent(root.transform, false);
            sparkGo.transform.localPosition = new Vector3(0f, 0.7f, 0f);

            var psSpark = sparkGo.AddComponent<ParticleSystem>();
            var mSpark = psSpark.main;
            mSpark.duration = 1.0f;
            mSpark.loop = true;
            mSpark.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            mSpark.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
            mSpark.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            mSpark.gravityModifier = -0.35f;
            mSpark.simulationSpace = ParticleSystemSimulationSpace.World;

            var eSpark = psSpark.emission;
            eSpark.rateOverTime = 16f;

            var sSpark = psSpark.shape;
            sSpark.shapeType = ParticleSystemShapeType.Sphere;
            sSpark.radius = 0.65f;

            var colSpark = psSpark.colorOverLifetime;
            colSpark.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.6f), new GradientColorKey(new Color(0.8f, 0.1f, 0.02f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colSpark.color = grad;

            var psrSpark = sparkGo.GetComponent<ParticleSystemRenderer>();
            if (psrSpark != null && flameMat != null)
            {
                psrSpark.sharedMaterial = flameMat;
            }

            SaveAndDestroy(root, path);
        }

        private static void BuildJumpTrailPrefab(Material smokeMat, Material flameMat)
        {
            string path = TargetDir + "/VFX_JumpTrail_Complete.prefab";
            var root = new GameObject("VFX_JumpTrail_Complete");

            // Vệt khói đen dày kéo dài theo quỹ đạo nhảy vọt trên không
            var smokeGo = new GameObject("Jump_BlackSmokeTrail");
            smokeGo.transform.SetParent(root.transform, false);
            var psSmoke = smokeGo.AddComponent<ParticleSystem>();
            var mSmoke = psSmoke.main;
            mSmoke.duration = 1.0f;
            mSmoke.loop = true;
            mSmoke.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.45f);
            mSmoke.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            mSmoke.startSize = new ParticleSystem.MinMaxCurve(0.65f, 1.35f);
            mSmoke.startColor = new Color(0.06f, 0.06f, 0.07f, 0.88f);
            mSmoke.simulationSpace = ParticleSystemSimulationSpace.World;

            var eSmoke = psSmoke.emission;
            eSmoke.rateOverTime = 28f;
            eSmoke.rateOverDistance = 12f;

            var sSmoke = psSmoke.shape;
            sSmoke.shapeType = ParticleSystemShapeType.Sphere;
            sSmoke.radius = 0.45f;

            var solSmoke = psSmoke.sizeOverLifetime;
            solSmoke.enabled = true;
            solSmoke.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 2.2f));

            var colSmoke = psSmoke.colorOverLifetime;
            colSmoke.enabled = true;
            var gradSmoke = new Gradient();
            gradSmoke.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.09f, 0.08f, 0.08f), 0f), new GradientColorKey(new Color(0.03f, 0.03f, 0.03f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.6f, 0.45f), new GradientAlphaKey(0f, 1f) }
            );
            colSmoke.color = gradSmoke;
            EnableSmokeSpritesheet8x8(psSmoke);
            var psrSmoke = smokeGo.GetComponent<ParticleSystemRenderer>();
            if (psrSmoke != null && smokeMat != null) psrSmoke.sharedMaterial = smokeMat;

            // Tia lửa rớt theo đường nhảy
            var sparkGo = new GameObject("Jump_FlameSparks");
            sparkGo.transform.SetParent(root.transform, false);
            var psSpark = sparkGo.AddComponent<ParticleSystem>();
            var mSpark = psSpark.main;
            mSpark.duration = 1.0f;
            mSpark.loop = true;
            mSpark.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            mSpark.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.8f);
            mSpark.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            mSpark.startColor = new Color(1f, 0.65f, 0.15f, 1f);
            mSpark.gravityModifier = 0.5f;
            mSpark.simulationSpace = ParticleSystemSimulationSpace.World;

            var eSpark = psSpark.emission;
            eSpark.rateOverTime = 30f;
            eSpark.rateOverDistance = 10f;

            var psrSpark = sparkGo.GetComponent<ParticleSystemRenderer>();
            if (psrSpark != null && flameMat != null) psrSpark.sharedMaterial = flameMat;

            // Trail lửa cam vàng rực rỡ kết hợp cùng vệt khói đen khi nhảy
            AddFlameTrailEmitter(root, flameMat, "Jump_FlameTrail", new Vector3(0f, 0.1f, 0f), 30f, 16f, 0.65f, 1.25f, 0.45f);

            SaveAndDestroy(root, path);
        }

        private static void BuildBombExplosionPrefab(Material smokeMat, Material debrisMat, Material flameMat, Material firePlumeMat)
        {
            string path = TargetDir + "/VFX_BombExplosion_Complete.prefab";
            var root = new GameObject("VFX_BombExplosion_Complete");

            // 1. Lõi chớp sáng gọn tại tâm vụ nổ (tránh bết trắng cả khung hình)
            var coreGo = new GameObject("Explosion_FlashCore");
            coreGo.transform.SetParent(root.transform, false);
            coreGo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var psCore = coreGo.AddComponent<ParticleSystem>();
            var mCore = psCore.main;
            mCore.duration = 0.3f;
            mCore.loop = false;
            mCore.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
            mCore.startSpeed = 0f;
            mCore.startSize = new ParticleSystem.MinMaxCurve(1.8f, 2.5f);
            mCore.startColor = new ParticleSystem.MinMaxGradient(new Color(2.2f, 1.05f, 0.25f, 0.95f));
            var eCore = psCore.emission;
            eCore.rateOverTime = 0f;
            eCore.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });
            var solCore = psCore.sizeOverLifetime;
            solCore.enabled = true;
            solCore.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.25f));
            var colCore = psCore.colorOverLifetime;
            colCore.enabled = true;
            var gCore = new Gradient();
            gCore.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.04f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.55f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            colCore.color = gCore;
            var psrCore = coreGo.GetComponent<ParticleSystemRenderer>();
            if (psrCore != null && flameMat != null) psrCore.sharedMaterial = flameMat;

            // 2. Vùng lửa bom bùng phát mạnh ở trung tâm (cuộn ngọn lửa WispySmoke HDR cam vàng đỏ)
            var fireGo = new GameObject("Explosion_Fireball");
            fireGo.transform.SetParent(root.transform, false);
            fireGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            fireGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var psFire = fireGo.AddComponent<ParticleSystem>();
            var mFire = psFire.main;
            mFire.duration = 1.0f;
            mFire.loop = false;
            mFire.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            mFire.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 4.4f);
            mFire.startSize = new ParticleSystem.MinMaxCurve(1.85f, 3.2f);
            mFire.startColor = new ParticleSystem.MinMaxGradient(new Color(2.4f, 0.95f, 0.16f, 0.96f));
            mFire.gravityModifier = -0.55f;
            mFire.simulationSpace = ParticleSystemSimulationSpace.World;
            var eFire = psFire.emission;
            eFire.rateOverTime = 0f;
            eFire.SetBursts(new[] {
                new ParticleSystem.Burst(0.0f, 32),
                new ParticleSystem.Burst(0.08f, 20)
            });
            var sFire = psFire.shape;
            sFire.shapeType = ParticleSystemShapeType.Cone;
            sFire.angle = 36f;
            sFire.radius = 0.42f;
            var solFire = psFire.sizeOverLifetime;
            solFire.enabled = true;
            solFire.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.65f, 1f, 1.45f));
            var colFire = psFire.colorOverLifetime;
            colFire.enabled = true;
            var gFire = new Gradient();
            gFire.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.88f, 0.32f), 0f),
                    new GradientColorKey(new Color(1f, 0.32f, 0.04f), 0.48f),
                    new GradientColorKey(new Color(0.55f, 0.08f, 0.01f), 1f)
                },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.8f, 0.45f), new GradientAlphaKey(0f, 1f) }
            );
            colFire.color = gFire;
            EnableSmokeSpritesheet8x8(psFire);
            var psrFire = fireGo.GetComponent<ParticleSystemRenderer>();
            if (psrFire != null && firePlumeMat != null) psrFire.sharedMaterial = firePlumeMat;

            // 3. Cột khói đen đặc quánh bốc cao từ trung tâm bom nổ (dùng WispySmoke03b_8x8 Alpha blend)
            var smokeGo = new GameObject("Explosion_BlackSmoke");
            smokeGo.transform.SetParent(root.transform, false);
            smokeGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            smokeGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var psSmoke = smokeGo.AddComponent<ParticleSystem>();
            var mSmoke = psSmoke.main;
            mSmoke.duration = 2.6f;
            mSmoke.loop = false;
            mSmoke.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.5f);
            mSmoke.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 5.2f);
            mSmoke.startSize = new ParticleSystem.MinMaxCurve(2.0f, 3.8f);
            mSmoke.startColor = new Color(0.05f, 0.05f, 0.06f, 0.98f);
            mSmoke.gravityModifier = -0.42f;
            mSmoke.simulationSpace = ParticleSystemSimulationSpace.World;
            var eSmoke = psSmoke.emission;
            eSmoke.rateOverTime = 0f;
            eSmoke.SetBursts(new[] {
                new ParticleSystem.Burst(0.0f, 38),
                new ParticleSystem.Burst(0.10f, 28),
                new ParticleSystem.Burst(0.24f, 20)
            });
            var sSmoke = psSmoke.shape;
            sSmoke.shapeType = ParticleSystemShapeType.Cone;
            sSmoke.angle = 44f;
            sSmoke.radius = 0.55f;
            var solSmoke = psSmoke.sizeOverLifetime;
            solSmoke.enabled = true;
            solSmoke.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.75f, 1f, 2.15f));
            var colSmoke = psSmoke.colorOverLifetime;
            colSmoke.enabled = true;
            var gSmoke = new Gradient();
            gSmoke.SetKeys(
                new[] { new GradientColorKey(new Color(0.08f, 0.07f, 0.07f), 0f), new GradientColorKey(new Color(0.02f, 0.02f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(0.92f, 0f), new GradientAlphaKey(0.85f, 0.55f), new GradientAlphaKey(0f, 1f) }
            );
            colSmoke.color = gSmoke;
            EnableSmokeSpritesheet8x8(psSmoke);
            var psrSmoke = smokeGo.GetComponent<ParticleSystemRenderer>();
            if (psrSmoke != null && smokeMat != null)
            {
                psrSmoke.sharedMaterial = smokeMat;
                psrSmoke.sortingFudge = 0.8f;
            }

            // 4. Vòng sóng lửa lan sát mặt đất
            var waveGo = new GameObject("Explosion_ShockwaveRing");
            waveGo.transform.SetParent(root.transform, false);
            waveGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            waveGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var psWave = waveGo.AddComponent<ParticleSystem>();
            var mWave = psWave.main;
            mWave.duration = 0.6f;
            mWave.loop = false;
            mWave.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.45f);
            mWave.startSpeed = new ParticleSystem.MinMaxCurve(7.5f, 11.5f);
            mWave.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            mWave.startColor = new ParticleSystem.MinMaxGradient(new Color(2.8f, 0.95f, 0.2f, 0.95f));
            mWave.simulationSpace = ParticleSystemSimulationSpace.World;
            var eWave = psWave.emission;
            eWave.rateOverTime = 0f;
            eWave.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });
            var sWave = psWave.shape;
            sWave.shapeType = ParticleSystemShapeType.Cone;
            sWave.angle = 88f;
            sWave.radius = 0.25f;
            var psrWave = waveGo.GetComponent<ParticleSystemRenderer>();
            if (psrWave != null && flameMat != null) psrWave.sharedMaterial = flameMat;

            // 5. Mảnh đá cháy sém & tia lửa văng tứ tung
            var debrisGo = new GameObject("Explosion_DebrisAndSparks");
            debrisGo.transform.SetParent(root.transform, false);
            debrisGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var psDebris = debrisGo.AddComponent<ParticleSystem>();
            var mDebris = psDebris.main;
            mDebris.duration = 1.2f;
            mDebris.loop = false;
            mDebris.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            mDebris.startSpeed = new ParticleSystem.MinMaxCurve(5.5f, 12.0f);
            mDebris.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
            mDebris.gravityModifier = 1.9f;
            mDebris.simulationSpace = ParticleSystemSimulationSpace.World;
            var eDebris = psDebris.emission;
            eDebris.rateOverTime = 0f;
            eDebris.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });
            var sDebris = psDebris.shape;
            sDebris.shapeType = ParticleSystemShapeType.Cone;
            sDebris.angle = 55f;
            sDebris.radius = 0.35f;
            var psrDebris = debrisGo.GetComponent<ParticleSystemRenderer>();
            if (psrDebris != null)
            {
                psrDebris.renderMode = ParticleSystemRenderMode.Mesh;
                psrDebris.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                if (debrisMat != null) psrDebris.sharedMaterial = debrisMat;
            }

            SaveAndDestroy(root, path);
        }

        private static void SaveAndDestroy(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
