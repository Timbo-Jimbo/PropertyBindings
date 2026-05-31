using UnityEngine;

namespace TimboJimbo.PropertyBindings
{
    internal static class ColorExtra 
    {
        public static void RGBToOkLab(Color color, out float l, out float a, out float b)
        {
            Vector3 lab = ColorToOkLab(color);
            l = lab.x;
            a = lab.y;
            b = lab.z;
        }

        public static Color OkLabToRGB(float l, float a, float b, float? withAlpha = null)
        {
            return OkLabToColor(new Vector3(l, a, b), withAlpha ?? 1f);
        }

        public static void RGBToOkLCh(Color color, out float l, out float c, out float h)
        {
            Vector3 lab = ColorToOkLab(color);
            Vector3 lch = OkLabToOkLCh(lab);
            l = lch.x;
            c = lch.y;
            h = lch.z;
        }

        public static Color OkLChToRGB(float l, float c, float h, float? withAlpha = null)
        {
            return OkLabToColor(OkLChToOkLab(new Vector3(l, c, h)), withAlpha ?? 1f);
        }
        
        public static Color LerpHSV(Color from, Color to, float t)
        {
            return LerpUnclampedHSV(from, to, Mathf.Clamp01(t));
        }

        public static Color LerpUnclampedHSV(Color from, Color to, float t)
        {
            Color.RGBToHSV(from, out float fromH, out float fromS, out float fromV);
            Color.RGBToHSV(to, out float toH, out float toS, out float toV);

            // if hue difference is > 180 degrees then we should lerp the shorter way around the color wheel
            if (Mathf.Abs(toH - fromH) > 0.5f)
            {
                if (toH > fromH)
                    fromH += 1f; // wrap around up
                else
                    toH += 1f; // wrap around up
            }

            float h = Mathf.LerpUnclamped(fromH, toH, t);
            float s = Mathf.LerpUnclamped(fromS, toS, t);
            float v = Mathf.LerpUnclamped(fromV, toV, t);

            // ensure hue is in [0, 1] range after lerp
            if (h < 0f) h += 1f;
            else if (h > 1f) h -= 1f;

            Color result = Color.HSVToRGB(h, s, v);
            result.a = Mathf.LerpUnclamped(from.a, to.a, t);
            return result;
        }

        public static Color LerpOkLab(Color from, Color to, float t)
        {
            return LerpUnclampedOkLab(from, to, Mathf.Clamp01(t));
        }

        public static Color LerpUnclampedOkLab(Color from, Color to, float t)
        {
            RGBToOkLab(from, out float fromL, out float fromA, out float fromB);
            RGBToOkLab(to, out float toL, out float toA, out float toB);

            float l = Mathf.LerpUnclamped(fromL, toL, t);
            float a = Mathf.LerpUnclamped(fromA, toA, t);
            float b = Mathf.LerpUnclamped(fromB, toB, t);

            return OkLabToRGB(l, a, b, withAlpha: Mathf.LerpUnclamped(from.a, to.a, t));
        }

        public static Color LerpOkLCh(Color from, Color to, float t)
        {
            return LerpUnclampedOkLCh(from, to, Mathf.Clamp01(t));
        }

        public static Color LerpUnclampedOkLCh(Color from, Color to, float t)
        {
            RGBToOkLCh(from, out float fromL, out float fromC, out float fromH);
            RGBToOkLCh(to, out float toL, out float toC, out float toH);

            // if hue difference is > 180 degrees (π radians), lerp the shorter way around the hue circle
            if (Mathf.Abs(toH - fromH) > Mathf.PI)
            {
                if (toH > fromH)
                    fromH += 2f * Mathf.PI;
                else
                    toH += 2f * Mathf.PI;
            }

            float l = Mathf.LerpUnclamped(fromL, toL, t);
            float c = Mathf.LerpUnclamped(fromC, toC, t);
            float h = Mathf.LerpUnclamped(fromH, toH, t);

            // normalize hue back to [-π, π]
            if (h > Mathf.PI) h -= 2f * Mathf.PI;
            else if (h < -Mathf.PI) h += 2f * Mathf.PI;

            return OkLChToRGB(l, c, h, withAlpha: Mathf.LerpUnclamped(from.a, to.a, t));
        }

        private static Color EnsureLinear(Color c) 
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? c : c.linear;
        }

        private static Color LinearToProjectSpecific(Color c)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? c : c.gamma;
        }

        private static Vector3 LinearToLMS(Vector3 linear)
        {
            return new Vector3(
                0.4122214708f * linear.x + 0.5363325363f * linear.y + 0.0514459929f * linear.z,
                0.2119034982f * linear.x + 0.6806995451f * linear.y + 0.1073969566f * linear.z,
                0.0883024619f * linear.x + 0.2817188376f * linear.y + 0.6299787005f * linear.z
            );
        }

        private static Vector3 LMSToOkLab(Vector3 lms)
        {
            float l_ = Mathf.Pow(lms.x, 1f / 3f);
            float m_ = Mathf.Pow(lms.y, 1f / 3f);
            float s_ = Mathf.Pow(lms.z, 1f / 3f);

            return new Vector3(
                0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
                1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
                0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_
            );
        }

        private static Vector3 OkLabToLMS(Vector3 lab)
        {
            float l_ = lab.x + 0.3963377774f * lab.y + 0.2158037573f * lab.z;
            float m_ = lab.x - 0.1055613458f * lab.y - 0.0638541728f * lab.z;
            float s_ = lab.x - 0.0894841775f * lab.y - 1.2914855480f * lab.z;

            return new Vector3(l_ * l_ * l_, m_ * m_ * m_, s_ * s_ * s_);
        }

        private static Vector3 LMSToLinear(Vector3 lms)
        {
            return new Vector3(
                 4.0767416621f * lms.x - 3.3077115913f * lms.y + 0.2309699292f * lms.z,
                -1.2684380046f * lms.x + 2.6097574011f * lms.y - 0.3413193965f * lms.z,
                -0.0041960863f * lms.x - 0.7034186147f * lms.y + 1.7076147010f * lms.z
            );
        }

        private static Vector3 ColorToOkLab(Color c)
        {
            Color linear = EnsureLinear(c);
            return LMSToOkLab(LinearToLMS(new Vector3(linear.r, linear.g, linear.b)));
        }

        private static Color OkLabToColor(Vector3 lab, float alpha)
        {
            Vector3 linear = LMSToLinear(OkLabToLMS(lab));
            Color linearColor = new Color(linear.x, linear.y, linear.z, alpha);
            return LinearToProjectSpecific(linearColor);
        }

        private static Vector3 OkLabToOkLCh(Vector3 lab)
        {
            return new Vector3(
                lab.x,
                Mathf.Sqrt(lab.y * lab.y + lab.z * lab.z),
                Mathf.Atan2(lab.z, lab.y)
            );
        }

        private static Vector3 OkLChToOkLab(Vector3 lch)
        {
            return new Vector3(
                lch.x,
                lch.y * Mathf.Cos(lch.z),
                lch.y * Mathf.Sin(lch.z)
            );
        }

    }
}
