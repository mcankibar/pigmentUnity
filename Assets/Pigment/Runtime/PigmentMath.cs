using System;
using UnityEngine;

namespace Pigment
{
    /// <summary>Direct port of liquidGame mix.js and the 40-disc vessel volume solver.</summary>
    public static class PigmentMath
    {
        static readonly Vector3[] corners =
        {
            new Vector3(.97f, .965f, .95f),
            new Vector3(.9f, .05f, .11f),
            new Vector3(1, .84f, .06f),
            new Vector3(1, .45f, .02f),
            new Vector3(.06f, .27f, .86f),
            new Vector3(.78f, .05f, .6f),
            new Vector3(.05f, .6f, .26f),
            new Vector3(.16f, .1f, .07f),
        };

        public static float Total(Vector4 m) => m.x + m.y + m.z + m.w;

        public static Color MixColor(Vector4 m)
        {
            float total = Total(m);
            if (total <= 0)
                return new Color(.97f, .965f, .95f);
            Vector3 p = new Vector3(m.x, m.y, m.z) / total;
            float peak = Mathf.Max(p.x, p.y, p.z);
            if (peak > 0)
                p *= ((m.x + m.y + m.z) / total) / peak;
            Vector3 rgb = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                float w =
                    ((i & 1) != 0 ? p.x : 1 - p.x)
                    * ((i & 2) != 0 ? p.y : 1 - p.y)
                    * ((i & 4) != 0 ? p.z : 1 - p.z);
                rgb += corners[i] * w;
            }
            return new Color(rgb.x, rgb.y, rgb.z, 1);
        }

        public static Vector4 Pure(PigmentId id)
        {
            var m = Vector4.zero;
            m[(int)id] = 1;
            return m;
        }

        static double Linear(double c) =>
            c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4);

        public static Vector3 Oklab(Color c)
        {
            double r = Linear(c.r),
                g = Linear(c.g),
                b = Linear(c.b);
            double l = Math.Pow(.4122214708 * r + .5363325363 * g + .0514459929 * b, 1.0 / 3),
                m = Math.Pow(.2119034982 * r + .6806995451 * g + .1073969566 * b, 1.0 / 3),
                s = Math.Pow(.0883024619 * r + .2817188376 * g + .6299787005 * b, 1.0 / 3);
            return new Vector3(
                (float)(.2104542553 * l + .793617785 * m - .0040720468 * s),
                (float)(1.9779984951 * l - 2.428592205 * m + .4505937099 * s),
                (float)(.0259040371 * l + .7827717662 * m - .808675766 * s)
            );
        }

        public static int Match(Vector4 recipe, Vector4 mix)
        {
            if (Total(recipe) <= 0 || Total(mix) <= 0)
                return 0;
            float score = Mathf.Max(
                0,
                1 - Vector3.Distance(Oklab(MixColor(recipe)), Oklab(MixColor(mix))) / .3f
            );
            return Mathf.FloorToInt(100 * Mathf.Pow(score, 1.35f) + .5f);
        }

        public sealed class Cavity
        {
            const int Slices = 40;
            readonly double[] ys = new double[Slices],
                rs = new double[Slices];
            public readonly float capacity,
                bottom,
                top,
                rimInner,
                rimOuter;
            readonly double dy;

            public Cavity(VesselDefinition d, float scale = 1)
            {
                bottom = d.baseThickness * scale;
                top = d.height * scale;
                rimInner = (d.Outer(d.height) - d.wall) * scale - .005f;
                rimOuter = d.rTop * scale;
                dy = (top - bottom) / Slices;
                double cap = 0;
                for (int i = 0; i < Slices; i++)
                {
                    ys[i] = bottom + (i + .5) * dy;
                    rs[i] = (d.Outer((float)ys[i] / scale) - d.wall) * scale - .005;
                    cap += Math.PI * rs[i] * rs[i] * dy;
                }
                capacity = (float)cap;
            }

            static double Area(double r, double a)
            {
                if (a >= r)
                    return Math.PI * r * r;
                if (a <= -r)
                    return 0;
                return r * r * Math.Acos(-a / r) + a * Math.Sqrt(r * r - a * a);
            }

            static double Integral(double r, double a)
            {
                if (a <= -r)
                    return 0;
                if (a >= r)
                    return Math.PI * r * r * a;
                return a * Area(r, a) + 2.0 / 3 * Math.Pow(r * r - a * a, 1.5);
            }

            public float Below(Vector3 up, float c)
            {
                double side = Math.Max(Math.Sqrt(up.x * up.x + up.z * up.z), 1e-6),
                    vol = 0,
                    half = dy * .5;
                for (int i = 0; i < Slices; i++)
                {
                    double y = ys[i],
                        r = rs[i];
                    if (Math.Abs(up.y) < 1e-4)
                    {
                        vol += Area(r, (c - y * up.y) / side) * dy;
                        continue;
                    }
                    double a0 = (c - (y - half) * up.y) / side,
                        a1 = (c - (y + half) * up.y) / side;
                    vol += side / up.y * (Integral(r, a0) - Integral(r, a1));
                }
                return Mathf.Clamp((float)vol, 0, capacity);
            }

            public float Lip(Vector3 up) =>
                top * up.y - rimInner * Mathf.Sqrt(up.x * up.x + up.z * up.z);

            public float CapacityAt(Vector3 up) => Below(up, Lip(up));

            public float Solve(Vector3 up, float volume)
            {
                float side = Mathf.Sqrt(up.x * up.x + up.z * up.z),
                    lo = float.PositiveInfinity,
                    hi = float.NegativeInfinity;
                for (int i = 0; i < Slices; i++)
                {
                    float y = (float)ys[i],
                        r = (float)rs[i],
                        half = (float)dy * .5f;
                    lo = Mathf.Min(lo, (y - half) * up.y - r * side, (y + half) * up.y - r * side);
                    hi = Mathf.Max(hi, (y - half) * up.y + r * side, (y + half) * up.y + r * side);
                }
                if (volume <= 0)
                    return lo;
                for (int i = 0; i < 26; i++)
                {
                    float mid = (lo + hi) * .5f;
                    if (Below(up, mid) < volume)
                        lo = mid;
                    else
                        hi = mid;
                }
                return (lo + hi) * .5f;
            }
        }
    }
}
