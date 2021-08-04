using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rabbit.Core;
using System;

namespace TestRabbit
{
  [TestClass]
  public class TestHalf
  {
    [TestMethod]
    public void TestValue()
    {
      (ushort, float)[] tests =
      {
        (0x0300, 4.57763671875e-5f),

        (0x3c00, 1),
        (0x3c01, 1.0009765625f),
        (0x3c02, 1.001953125f),
        (0x3fff, 1.9990234375f),
        (0x4000, 2),
        (0xc000, -2),

        (0x7bfe, 65472),
        (0x7bff, Half.MaxValue),
        (0xfbff, Half.MinValue),

        (0x0400, 6.103515625E-5f),

        (0x0001, 5.960464477539063E-8f),

        (0x0000, 0),
        (0x8000, -0),

        (0x7c00, float.PositiveInfinity),
        (0xfc00, float.NegativeInfinity),
      };

      foreach((ushort u16, float correct) in tests)
      {
        Half half = new Half(u16);
        Assert.AreEqual<float>(correct, half, "’l‚ª³‚µ‚­İ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
      }
    }

    [TestMethod]
    public void TestFromFloat()
    {
      (ushort, float)[] tests =
      {
        (0x0300, 4.57763671875e-5f),

        (0x3c00, 1f),
        (0x3c01, 1.0009765625f),
        (0x3c02, 1.001953125f),
        (0x3fff, 1.9990234375f),
        (0x4000, 2f),
        (0xc000, -2f),

        (0x7bfe, 65472f),
        (0x7bff, Half.MaxValue),
        (0xfbff, Half.MinValue),

        (0x0400, 6.103515625E-5f),

        (0x0001, 5.960464477539063E-8f),

        (0x0000, 0f),
        (0x8000, -0f),

        (0x7c00, float.PositiveInfinity),
        (0xfc00, float.NegativeInfinity),
      };

      foreach ((ushort u16, float correct) in tests)
      {
        Half expect = new Half(u16);
        Half actual = correct;
        Assert.AreEqual(expect, actual, "’l‚ª³‚µ‚­İ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
      }
    }

    [TestMethod]
    public void TestCalc()
    {
      // a, b, a + b, a - b, a * b, a / b, a % b
      (float, float, float, float, float, float, float)[] tests =
      {
        (1f, 1f, 2f, 0f, 1f, 1f, 0f),
        (2f, 4f, 6f, -2f, 8f, 0.5f, 2f),
      };

      foreach((float a, float b, float add, float sub, float mul, float div, float rem) in tests)
      {
        Half ah = a;
        Half bh = b;

        Assert.AreEqual<float>(add, ah + bh, "‰ÁZ‚É¸”s‚µ‚Ä‚¢‚Ü‚·");
        Assert.AreEqual<float>(sub, ah - bh, "Œ¸Z‚É¸”s‚µ‚Ä‚¢‚Ü‚·");
        Assert.AreEqual<float>(mul, ah * bh, "æZ‚É¸”s‚µ‚Ä‚¢‚Ü‚·");
        Assert.AreEqual<float>(div, ah / bh, "œZ‚É¸”s‚µ‚Ä‚¢‚Ü‚·");
        Assert.AreEqual<float>(rem, ah % bh, "—]Z‚É¸”s‚µ‚Ä‚¢‚Ü‚·");
      }
    }
  }
}
