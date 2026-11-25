using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aiara.EditorTools
{
    /// <summary>
    /// Tạo sẵn bộ file âm thanh .wav (SFX) cho các hành động của MonsterBall
    /// (trồi lên, nhảy vọt, lăn, nổ bom và va chạm).
    /// </summary>
    [InitializeOnLoad]
    public static class CombatSfxGenerator
    {
        public const string SfxDir = "Assets/Game/3. Resources/Audio/SFX";

        static CombatSfxGenerator()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(SfxDir + "/sfx_bomb_explosion.wav") ||
                    !File.Exists(SfxDir + "/sfx_monster_jump.wav") ||
                    !File.Exists(SfxDir + "/sfx_monster_emerge.wav") ||
                    !File.Exists(SfxDir + "/sfx_monster_roll.wav") ||
                    !File.Exists(SfxDir + "/sfx_hit_impact.wav"))
                {
                    GenerateAllSfx();
                }
            };
        }

        [MenuItem("Tools/StreetFighter/Generate Combat SFX")]
        public static void GenerateAllSfx()
        {
            if (!Directory.Exists(SfxDir))
            {
                Directory.CreateDirectory(SfxDir);
            }

            WriteBombExplosionWav(SfxDir + "/sfx_bomb_explosion.wav");
            WriteJumpLaunchWav(SfxDir + "/sfx_monster_jump.wav");
            WriteEmergeWav(SfxDir + "/sfx_monster_emerge.wav");
            WriteRollWav(SfxDir + "/sfx_monster_roll.wav");
            WriteImpactWav(SfxDir + "/sfx_hit_impact.wav");

            AssetDatabase.Refresh();
            Debug.Log("[CombatSfxGenerator] Đã tạo bộ âm thanh SFX chiến đấu tại " + SfxDir);
        }

        private static void WriteBombExplosionWav(string path)
        {
            const int sr = 44100;
            const float dur = 1.55f;
            int n = (int)(sr * dur);
            float[] buf = new float[n];
            var rng = new System.Random(101);
            double lp = 0.0;
            double crackLp = 0.0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float subFreq = Mathf.Lerp(58f, 20f, Mathf.Clamp01(t / 0.55f));
                float sub = Mathf.Sin(2f * Mathf.PI * subFreq * t) * Mathf.Exp(-t * 3.4f);

                float midFreq = Mathf.Lerp(145f, 38f, Mathf.Clamp01(t / 0.25f));
                float mid = Mathf.Sin(2f * Mathf.PI * midFreq * t) * Mathf.Exp(-t * 8.5f);

                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.045 * (noise - lp);
                crackLp += 0.35 * (noise - crackLp);

                float crackEnv = Mathf.Exp(-t * 22f);
                float rumbleEnv = Mathf.Exp(-t * 2.4f);
                float fireCrackle = (t > 0.1f && rng.NextDouble() > 0.96) ? noise * 0.45f * Mathf.Exp(-t * 2.1f) : 0f;

                float s = sub * 0.9f + mid * 0.65f + (float)crackLp * crackEnv * 1.15f + (float)lp * rumbleEnv * 1.4f + fireCrackle;
                buf[i] = Mathf.Clamp((float)Math.Tanh(s * 1.25f), -0.98f, 0.98f);
            }

            WriteWav16(path, sr, buf);
        }

        private static void WriteJumpLaunchWav(string path)
        {
            const int sr = 44100;
            const float dur = 0.55f;
            int n = (int)(sr * dur);
            float[] buf = new float[n];
            var rng = new System.Random(202);
            double bp = 0.0;
            float phase = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float norm = t / dur;
                float freq = Mathf.Lerp(110f, 360f, norm);
                phase += 2f * Mathf.PI * freq / sr;

                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float alpha = Mathf.Lerp(0.08f, 0.28f, norm);
                bp += alpha * (noise - bp);

                float env = Mathf.Sin(norm * Mathf.PI) * Mathf.Exp(-t * 1.6f);
                float s = (Mathf.Sin(phase) * 0.45f + (float)bp * 1.35f) * env;
                buf[i] = Mathf.Clamp((float)Math.Tanh(s * 1.1f), -0.98f, 0.98f);
            }

            WriteWav16(path, sr, buf);
        }

        private static void WriteEmergeWav(string path)
        {
            const int sr = 44100;
            const float dur = 0.85f;
            int n = (int)(sr * dur);
            float[] buf = new float[n];
            var rng = new System.Random(303);
            double lp = 0.0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.06 * (noise - lp);

                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(85f, 32f, Mathf.Clamp01(t / 0.4f)) * t) * Mathf.Exp(-t * 4.5f);
                float debris = (rng.NextDouble() > 0.94 ? noise * 0.55f : 0f) * Mathf.Exp(-t * 3.0f);
                float env = Mathf.Clamp01(t / 0.04f) * Mathf.Exp(-t * 2.8f);

                float s = thud * 0.85f + (float)lp * env * 1.4f + debris;
                buf[i] = Mathf.Clamp((float)Math.Tanh(s * 1.15f), -0.98f, 0.98f);
            }

            WriteWav16(path, sr, buf);
        }

        private static void WriteRollWav(string path)
        {
            const int sr = 44100;
            const float dur = 0.65f;
            int n = (int)(sr * dur);
            float[] buf = new float[n];
            var rng = new System.Random(404);
            double lp = 0.0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float pulse = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 12f * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.1 * (noise - lp);
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float s = ((float)lp * 1.3f + Mathf.Sin(2f * Mathf.PI * 75f * t) * 0.4f) * pulse * env;
                buf[i] = Mathf.Clamp((float)Math.Tanh(s * 1.1f), -0.98f, 0.98f);
            }

            WriteWav16(path, sr, buf);
        }

        private static void WriteImpactWav(string path)
        {
            const int sr = 44100;
            const float dur = 0.42f;
            int n = (int)(sr * dur);
            float[] buf = new float[n];
            var rng = new System.Random(505);
            double lp = 0.0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float punch = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 40f, Mathf.Clamp01(t / 0.14f)) * t) * Mathf.Exp(-t * 12f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.22 * (noise - lp);
                float sizzle = (float)lp * Mathf.Exp(-t * 7f);
                float s = punch * 0.95f + sizzle * 0.75f;
                buf[i] = Mathf.Clamp((float)Math.Tanh(s * 1.2f), -0.98f, 0.98f);
            }

            WriteWav16(path, sr, buf);
        }

        private static void WriteWav16(string path, int sampleRate, float[] samples)
        {
            int dataSize = samples.Length * 2;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);

                for (int i = 0; i < samples.Length; i++)
                {
                    short v = (short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                    bw.Write(v);
                }
            }
        }
    }
}
