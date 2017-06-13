using System;

namespace UnityEngine.Experimental.PostProcessing
{
    public abstract class ParameterOverride
    {
        public bool overrideState;

        internal abstract void Interp(ParameterOverride from, ParameterOverride to, float t);

        public abstract int GetHash();

        public T GetValue<T>()
        {
            return ((ParameterOverride<T>)this).value;
        }
    }

    [Serializable]
    public class ParameterOverride<T> : ParameterOverride
    {
        public T value;

        public ParameterOverride()
            : this(default(T), false)
        {
        }

        public ParameterOverride(T value)
            : this(value, false)
        {
        }

        public ParameterOverride(T value, bool overrideState)
        {
            this.value = value;
            this.overrideState = overrideState;
        }

        internal override void Interp(ParameterOverride from, ParameterOverride to, float t)
        {
            // Note: this isn't completely safe but it'll do fine
            Interp(from.GetValue<T>(), to.GetValue<T>(), t);
        }

        public virtual void Interp(T from, T to, float t)
        {
            // Returns `b` if `dt > 0` by default so we don't have to write overrides for bools and
            // enumerations.
            value = t > 0f ? to : from;
        }

        public void Override(T x)
        {
            overrideState = true;
            value = x;
        }

        public override int GetHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();
                hash = hash * 23 + value.GetHashCode();
                return hash;
            }
        }

        // Implicit conversion; assuming the following:
        //
        //   var myFloatProperty = new ParameterOverride<float> { value = 42f; };
        //
        // It allows for implicit casts:
        //
        //   float myFloat = myFloatProperty.value; // No implicit cast
        //   float myFloat = myFloatProperty;       // Implicit cast
        //
        // For safety reason this is one-way only.
        public static implicit operator T(ParameterOverride<T> prop)
        {
            return prop.value;
        }
    }

    // Bypassing the limited unity serialization system...
    [Serializable]
    public sealed class FloatParameter : ParameterOverride<float>
    {
        public override void Interp(float from, float to, float t)
        {
            value = from + (to - from) * t;
        }
    }

    [Serializable]
    public sealed class IntParameter : ParameterOverride<int>
    {
        public override void Interp(int from, int to, float t)
        {
            // Int snapping interpolation. Don't use this for enums as they don't necessarily have
            // contiguous values. Use the default interpolator instead (same as bool).
            value = (int)(from + (to - from) * t);
        }
    }

    [Serializable]
    public sealed class BoolParameter : ParameterOverride<bool> {}

    [Serializable]
    public sealed class ColorParameter : ParameterOverride<Color>
    {
        public override void Interp(Color from, Color to, float t)
        {
            // Lerping color values is a sensitive subject... We looked into lerping colors using
            // HSV and LCH but they have some downsides that make them not work correctly in all
            // situations, so we stick with RGB lerping for now, at least its behavior is
            // predictable despite looking desaturated when `t ~= 0.5` and it's faster anyway.
            value.r = from.r + (to.r - from.r) * t;
            value.g = from.g + (to.g - from.g) * t;
            value.b = from.b + (to.b - from.b) * t;
            value.a = from.a + (to.a - from.a) * t;
        }
    }

    [Serializable]
    public sealed class Vector2Parameter : ParameterOverride<Vector2>
    {
        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
        }
    }

    [Serializable]
    public sealed class Vector3Parameter : ParameterOverride<Vector3>
    {
        public override void Interp(Vector3 from, Vector3 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
            value.z = from.z + (to.z - from.z) * t;
        }
    }

    [Serializable]
    public sealed class Vector4Parameter : ParameterOverride<Vector4>
    {
        public override void Interp(Vector4 from, Vector4 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
            value.z = from.z + (to.z - from.z) * t;
            value.w = from.w + (to.w - from.w) * t;
        }
    }

    [Serializable]
    public sealed class SplineParameter : ParameterOverride<Spline>
    {
        public override void Interp(Spline from, Spline to, float t)
        {
            int frameCount = Time.renderedFrameCount;
            from.Cache(frameCount);
            to.Cache(frameCount);

            for (int i = 0; i < Spline.k_Precision; i++)
            {
                float a = from.cachedData[i];
                float b = to.cachedData[i];
                value.cachedData[i] = a + (b - a) * t;
            }
        }
    }

    [Serializable]
    public sealed class TextureParameter : ParameterOverride<Texture>
    {
        public override void Interp(Texture from, Texture to, float t)
        {
            if (from == null || to == null)
            {
                base.Interp(from, to, t);
                return;
            }

            value = TextureLerper.instance.Lerp(from, to, t);
        }
    }
} 
