using System;

namespace Rabbit.Core
{
  using ResultU8 = Result<byte>;
  using ResultU16 = Result<ushort>;

  public static class ArrayConverter
  {
    public const int BYTE_USHORT_SIZE = 2;

    public static ResultU8 GetUint8(byte[] code, ref ushort idx)
    {
      if (Math.Clamp(idx, 0, code.Length - 1) != idx)
        return (0, $"参照位置が無効です: 実際 => {idx}, 期待 => [0, {code.Length - 1}]");

      return (code[idx++], null);
    }

    public static ResultU16 GetUint16(byte[] code, ref ushort idx)
    {
      if (Math.Clamp(idx, 0, code.Length - 1) != idx)
        return (0, $"参照位置が無効です: 実際 => {idx}, 期待 => [0, {code.Length - 1}]");
      if (Math.Min(idx + BYTE_USHORT_SIZE, code.Length) != idx + BYTE_USHORT_SIZE)
        return (0, $"参照位置が無効です: 実際 => {idx} + {BYTE_USHORT_SIZE}, 期待 => [0, {code.Length - 1}]");

      idx += BYTE_USHORT_SIZE;
      return (BitConverter.ToUInt16(code, idx - BYTE_USHORT_SIZE), null);
    }

    public static Result<byte[]> GetRangeU8(byte[] mem, ushort idx, int length)
    {
      if (Math.Clamp(idx, 0, mem.Length - 1) != idx)
        return (null, $"参照位置が無効です: 実際 => {idx}, 期待 => [0, {mem.Length - 1}]");
      if (Math.Min(idx + length, mem.Length) != idx + length)
        return (null, $"参照位置が無効です: 実際 => {idx} + {length}, 期待 => [0, {mem.Length - 1}]");

      byte[] buf = new byte[length];
      Array.Copy(mem, idx, buf, 0, length);
      return (buf, null);
    }

    public static Result<ushort[]> GetRangeU16(byte[] code, ushort idx, int length)
    {
      if (Math.Clamp(idx, 0, code.Length - 1) != idx)
        return (null, $"参照位置が無効です: 実際 => {idx}, 期待 => [0, {code.Length - 1}]");
      if (Math.Min(idx + length, code.Length) != idx + length)
        return (null, $"参照位置が無効です: 実際 => {idx} + {length}, 期待 => [0, {code.Length - 1}]");

      int bufLen = length / BYTE_USHORT_SIZE
        + (length % BYTE_USHORT_SIZE == 0 ? 0 : 1);
      ushort[] buf = new ushort[bufLen];
      Buffer.BlockCopy(code, idx, buf, 0, length);
      return (buf, null);
    }

    public static Result<ushort[]> GetRangeU16(ushort[] mem, ushort idx, int length)
    {
      if (Math.Clamp(idx, 0, mem.Length - 1) != idx)
        return (null, $"参照位置が無効です: 実際 => {idx}, 期待 => [0, {mem.Length - 1}]");
      if (Math.Min(idx + length, mem.Length) != idx + length)
        return (null, $"参照位置が無効です: 実際 => {idx} + {length}, 期待 => [0, {mem.Length - 1}]");

      ushort[] buf = new ushort[length];
      Array.Copy(mem, idx, buf, 0, length);
      return (buf, null);
    }
  }
}
