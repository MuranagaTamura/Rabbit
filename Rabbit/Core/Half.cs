using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Rabbit.Core
{
  public struct Half : IComparable, IComparable<Half>, IEquatable<Half>, IFormattable
  {
    [StructLayout(LayoutKind.Explicit)]
    private struct Union
    {
      [FieldOffset(0)]
      public int i32;
      [FieldOffset(0)]
      public float f32;
    }

    private bool _sign;
    private ushort _exponent;
    private ushort _fraction;

    public Half(ushort toFloat)
    {
      _sign = (toFloat & 0x8000) == 0;
      _exponent = (ushort)((toFloat & 0x7C00) >> 10);
      _fraction = (ushort)(toFloat & 0x03FF);
    }

    public static Half Epsilon => new Half(0x0001);
    public static Half MaxValue => new Half(0x7BFF);
    public static Half MinValue => new Half(0xFBFF);
    public static Half NaN => new Half(0x7C01);
    public static Half PositiveInfinity => new Half(0x7C00);
    public static Half NegativeInfinity => new Half(0xFC00);

    public static Half operator +(Half a, Half b)
      => (float)a + (float)b;

    public static Half operator -(Half a, Half b)
      => (float)a - (float)b;

    public static Half operator *(Half a, Half b)
      => (float)a * (float)b;

    public static Half operator /(Half a, Half b)
      => (float)a / (float)b;

    public static Half operator %(Half a, Half b)
      => (float)a % (float)b;

    public int CompareTo(object obj)
    {
      Half? val = obj as Half?;
      if (!val.HasValue)
        throw new ArgumentException("Object must by of type Half.");
      return CompareTo(val.Value);
    }

    public int CompareTo([AllowNull] Half other)
    {
      float self = this;
      return self.CompareTo(other);
    }

    public bool Equals([AllowNull] Half other)
    {
      float self = this;
      return self.Equals(other);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
      float self = this;
      return self.ToString(format, formatProvider);
    }

    public override string ToString()
    {
      return $"{(float)this}";
    }

    private static int NumOfLeadingZero(ushort val)
    {
      Union data = new Union() { f32 = val + 0.5f };
      return 158 - (data.i32 >> 23) - 16;
    }

    #region implicit half to float
    public static implicit operator float(Half half)
    {
      float value;

      // 指数部によって特殊な場合を場合分け
      switch (half._exponent)
      {
        case 0x00:
          // 正負のゼロ
          if (half._fraction == 0)
            value = half._sign ? 0 : -0;
          // 非正規化数
          else
            value = SubnormalNum(half._sign, half._fraction);
          break;
        case 0x1f:
          // 正負の無限
          if (half._fraction == 0)
            value = half._sign ? float.PositiveInfinity : float.NegativeInfinity;
          else
            value = float.NaN;
          break;
        default:
          value = NormalNum(half._sign, half._exponent, half._fraction);
          break;
      }

      return value;
    }

    private static float SubnormalNum(bool sign, ushort fraction)
    {
      // 10bit目に1が来るまでカウントする
      int count = NumOfLeadingZero(fraction) - 6 + 1;

      // 上位から数え下げて初めて1が出現したところ + 1まで左シフト
      int i32Fraction = (fraction << count) & 0x03FF;

      // 最後に何bit左にずらせばいいのかを求める（127 - 14 は確定）
      int i32Exponent = 127 - count - 14;
      return ToFloat(sign, i32Exponent, i32Fraction);
    }

    private static float NormalNum(bool sign, int exponent, int fraction)
    {
      return ToFloat(sign, exponent - 15 + 127, fraction);
    }

    private static float ToFloat(bool sign, int exponent, int fraction)
    {
      Union conv = new Union();
      conv.i32 = exponent << 23 | fraction << 13;
      if (!sign) conv.i32 = (int)(conv.i32 | 0x80000000);
      return conv.f32;
    }
    #endregion

    #region implicit float to half
    public static implicit operator Half(float f32)
    {
      if (f32 == 0 && float.IsNegative(f32))
        return new Half(0x8000);
      else if (f32 == 0)
        return new Half(0x0000);
      else if (f32 == float.PositiveInfinity)
        return PositiveInfinity;
      else if (f32 == float.NegativeInfinity)
        return NegativeInfinity;
      else if (float.IsNaN(f32))
        return NaN;
      else if (float.IsSubnormal(f32))
        return float.IsNegative(f32) ? new Half(0x8000) : new Half(0x0000);
      else
        return FromNormalNum(f32);
    }

    private static Half FromNormalNum(float f32)
    {
      Union union = new Union();
      union.f32 = f32;
      bool sign = (union.i32 & 0x80000000) == 0;
      int exponent = (union.i32 & 0x7F800000) >> 23;
      int fraction = (union.i32 & 0x007FFFFF);

      // exponent - 127 が[-14, 15]の範囲に収まる場合、正規化数で変換
      int isNormalVal = exponent - 127;
      if (Math.Clamp(isNormalVal, -14, 15) == isNormalVal)
      {
        return new Half()
        {
          _sign = sign,
          _exponent = (ushort)(isNormalVal + 15),
          _fraction = (ushort)(fraction >> 13),
        };
      }
      // exponent - 127 が[-15, -24]の範囲に収まる場合、非正規化数で変換
      else if (Math.Clamp(isNormalVal, -24, -15) == isNormalVal)
      {
        int lshift = -(isNormalVal + 14);
        return new Half()
        {
          _sign = sign,
          _exponent = 0,
          _fraction = (ushort)((fraction | 0x00800000) >> (13 + lshift)),
        };
      }
      // それ以外の場合は、符号付き0で変換
      else
      {
        return new Half()
        {
          _sign = sign,
          _fraction = 0,
          _exponent = 0,
        };
      }
    }
    #endregion

    #region implicit ushort
    public static implicit operator ushort(Half half)
    {
      return (ushort)((half._sign ? 0x0000 : 0x8000) | half._exponent << 10 | half._fraction);
    }

    public static implicit operator Half(ushort value)
    {
      return new Half(value);
    }
    #endregion
  }
}
