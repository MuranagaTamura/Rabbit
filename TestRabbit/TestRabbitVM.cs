using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rabbit.VirtualMachine;
using Rabbit.Core;
using Rabbit.Generator;
using System.Collections.Generic;

namespace TestRabbit
{
  [TestClass]
  public class TestRabbitVM
  {
    [TestMethod]
    public void TestLOADI()
    {
      (RegId, ushort)[] tests =
      {
        (RegId.A0, 0x0001),
        (RegId.A1, 0x0002),
        (RegId.A12, 0xFFFF),
        (RegId.Sp, 0xFFFE),
      };

      foreach ((RegId id, ushort test) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(id, test);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual(test, vm.GetReg(id).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestMemoryAccess()
    {
      ushort ptr = 0x000F;
      ushort val = 0xFEDC;

      // A0: ptr, A1: store val, S2: load val, 
      Assembler generator = new Assembler();
      generator.LOADI(RegId.A0, ptr);
      generator.LOADI(RegId.A1, val);
      generator.STORE(RegId.A0, RegId.A1);
      generator.LOAD(RegId.A2, RegId.A0);

      RabbitVM vm = new RabbitVM();
      Result<bool> initRes = vm.Init(generator.GetResult(), 128);
      Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

      Result<bool> stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
      stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
      stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
      stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

      Assert.AreEqual(ptr, vm.GetReg(RegId.A0).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ì‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½");
      Assert.AreEqual(val, vm.GetReg(RegId.A1).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ì‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½");
      Assert.AreEqual(val, vm.GetMem(ptr).Unwrap(), "ƒƒ‚ƒŠ‚Ì’l‚ÌƒXƒgƒA‚É¸”s‚µ‚Ü‚µ‚½");
      Assert.AreEqual(val, vm.GetReg(RegId.A2).Unwrap(), "ƒƒ‚ƒŠ‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
    }

    [TestMethod]
    public void TestMOVE()
    {
      (RegId, RegId, ushort)[] tests =
      {
        (RegId.A0, RegId.A1, 0x0001),
        (RegId.A0, RegId.A2, 0x0001),
      };

      foreach ((RegId a, RegId b, ushort test) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(a, test);
        generator.MOVE(b, a);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual(test, vm.GetReg(a).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual(test, vm.GetReg(b).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚ÌˆÚ“®‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestJMP()
    {
      Assembler generator = new Assembler();
      generator.JMP("Label");
      for (int i = 0; i < 10; ++i)
        generator.LOADI(RegId.A0, 0x01);
      generator.SetLabel("Label");
      generator.LOADI(RegId.A1, 0x02);

      RabbitVM vm = new RabbitVM();
      Result<bool> initRes = vm.Init(generator.GetResult(), 128);
      Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

      // ƒWƒƒƒ“ƒv‚µ‚ÄAƒŒƒWƒXƒ^‚É’l‚ğİ’è‚·‚é2H’ö‚ª•K—v
      Result<bool> stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
      stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

      Assert.AreEqual(0x00, vm.GetReg(RegId.A0).Unwrap(), "JMP‚É¸”s‚µ‚Ü‚µ‚½");
      Assert.AreEqual(0x02, vm.GetReg(RegId.A1).Unwrap(), "JMP‚É¸”s‚µ‚Ü‚µ‚½");
    }

    [TestMethod]
    public void TestADD()
    {
      // a + b = c
      (ushort, ushort, ushort)[] tests =
      {
        (0x0000, 0x0000, 0x0000),
        (0x0001, 0x0001, 0x0002),
        (0x0050, 0x0050, 0x00A0),
        (0xFFFF, 0xFFFE, 0xFFFD),
      };

      foreach((ushort a, ushort b, ushort c) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(RegId.A0, a);
        generator.LOADI(RegId.A1, b);
        generator.ADD(RegId.A1, RegId.A0, RegId.A1);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        // ƒŒƒWƒXƒ^‚É’l‚ğİ’è‚·‚é x 2, ‰ÁZ–½—ß x 1 = 3
        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual(a, vm.GetReg(RegId.A0).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual(c, vm.GetReg(RegId.A1).Unwrap(), "ƒŒƒWƒXƒ^“¯m‚Ì‰ÁZ‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestADDF()
    {
      // a + b = c
      (Half, Half, Half)[] tests =
      {
        (1f, 1f, 2f),
        (1.1f, 2.1f, 3.2f),
      };

      foreach ((Half a, Half b, Half c) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(RegId.A0, a);
        generator.LOADI(RegId.A1, b);
        generator.ADDF(RegId.A1, RegId.A0, RegId.A1);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        // ƒŒƒWƒXƒ^‚É’l‚ğİ’è‚·‚é x 2, ‰ÁZ–½—ß x 1 = 3
        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual<Half>(a, vm.GetReg(RegId.A0).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual<Half>(c, vm.GetReg(RegId.A1).Unwrap(), "ƒŒƒWƒXƒ^“¯m‚Ì‰ÁZ‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestSUBF()
    {
      // a - b = c
      (Half, Half, Half)[] tests =
      {
        (1f, 1f, 0f),
        (1.1f, 2.1f, -1.0f),
      };

      foreach ((Half a, Half b, Half c) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(RegId.A0, a);
        generator.LOADI(RegId.A1, b);
        generator.SUBF(RegId.A1, RegId.A0, RegId.A1);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual<Half>(a, vm.GetReg(RegId.A0).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual<Half>(c, vm.GetReg(RegId.A1).Unwrap(), "ƒŒƒWƒXƒ^“¯m‚Ì‰ÁZ‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestSUB()
    {
      // a - b = c
      (ushort, ushort, ushort)[] tests =
      {
        (0x0000, 0x0000, 0x0000),
        (0xFFFF, 0xFFFE, 0x0001),
        (0x0000, 0xFFFE, 0x0002),
        (0xFFFE, 0x0001, 0xFFFD),
      };

      foreach ((ushort a, ushort b, ushort c) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(RegId.A0, a);
        generator.LOADI(RegId.A1, b);
        generator.SUB(RegId.A1, RegId.A0, RegId.A1);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        // ƒŒƒWƒXƒ^‚É’l‚ğİ’è‚·‚é x 2, ‰ÁZ–½—ß x 1 = 3
        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual(a, vm.GetReg(RegId.A0).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual(c, vm.GetReg(RegId.A1).Unwrap(), "ƒŒƒWƒXƒ^“¯m‚ÌŒ¸Z‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestCMP()
    {
      // a - b, sign, zero
      (ushort, ushort, bool, bool)[] tests =
      {
        (0x0000, 0x0000, false, true),
        (0xFFFF, 0xFFFE, false, false),
        (0x0000, 0xFFFE, true, false),
      };

      foreach ((ushort a, ushort b, bool sign, bool zero) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(RegId.A0, a);
        generator.LOADI(RegId.A1, b);
        generator.CMP(RegId.A0, RegId.A1);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual(a, vm.GetReg(RegId.A0).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual(b, vm.GetReg(RegId.A1).Unwrap(), "ƒŒƒWƒXƒ^‚Ì’l‚Ìƒ[ƒh‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual(sign, vm.IsSign, "”äŠr‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual(zero, vm.IsZero, "”äŠr‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestPUSH()
    {
      (RegId, ushort)[] tests =
      {
        (RegId.A0, 0x0000),
        (RegId.A12, 0x0001),
        (RegId.A1, 0xFFFF),
      };

      foreach ((RegId reg, ushort a) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(reg, a);
        generator.PUSH(reg);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        // LOAD x1, PUSH x1 = 2
        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual(a, vm.GetReg(reg).Unwrap(), "ƒvƒbƒVƒ…‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual(a, vm.GetMem((ushort)(vm.Sp + 1)).Unwrap(), "ƒvƒbƒVƒ…‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestPOP()
    {
      (ushort, RegId, RegId)[] tests =
      {
        (0x0000, RegId.A0, RegId.A1),
      };

      foreach ((ushort a, RegId reg0, RegId reg1) in tests)
      {
        Assembler generator = new Assembler();
        generator.LOADI(reg0, a);
        generator.PUSH(reg0);
        generator.POP(reg1);

        RabbitVM vm = new RabbitVM();
        Result<bool> initRes = vm.Init(generator.GetResult(), 128);
        Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");

        Result<bool> stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
        stepRun = vm.StepRun();
        Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

        Assert.AreEqual(a, vm.GetReg(reg0).Unwrap(), "LOADI‚É¸”s‚µ‚Ü‚µ‚½");
        Assert.AreEqual(a, vm.GetReg(reg1).Unwrap(), "POP‚É¸”s‚µ‚Ü‚µ‚½");
      }
    }

    [TestMethod]
    public void TestSYSCALL()
    {
      Assembler generator = new Assembler();
      generator.SetConst("‚±‚ê‚ÍƒeƒXƒg‚Å‚·");
      generator.LOADI(RegId.A0, 0x00);
      generator.PUSH(RegId.A0);
      generator.SYSCALL(0x00);

      RabbitVM vm = new RabbitVM();
      Result<bool> initRes = vm.Init(generator.GetResult(), 128);
      Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");
      vm.RegisterSysFunc(0x00, Test);

      Result<bool> stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
      stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
      stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
      stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

      Assert.AreEqual("‚±‚ê‚ÍƒeƒXƒg‚Å‚·", testText, $"‚«‚¿‚ñ‚ÆŒÄ‚Ñ‚Ü‚ê‚Ä‚Ü‚¹‚ñ");
    }

    [TestMethod]
    public void TestSYSCALL2()
    {
      Assembler generator = new Assembler();
      generator.SYSCALL(0x00);

      RabbitVM vm = new RabbitVM();
      Result<bool> initRes = vm.Init(generator.GetResult(), 128);
      Assert.IsTrue(initRes.Ok, $"‰Šú‰»‚É¸”s‚µ‚Ü‚µ‚½: {initRes.Err}");
      vm.RegisterSysFunc(0x00, Test2);

      Result<bool> stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");
      stepRun = vm.StepRun();
      Assert.IsTrue(stepRun.Ok, $"StepRun‚É¸”s‚µ‚Ü‚µ‚½: {stepRun.Err}");

      Assert.AreEqual(0x1234, vm.GetReg(RegId.A0).Unwrap(), $"•Ô‚è’l‚ª‚¤‚Ü‚­“®ì‚µ‚Ä‚¢‚Ü‚¹‚ñ");
    }

    #region Test Syscall
    static string testText = "";
    private IEnumerator<SyscallReturn> Test(SyscallInfo info)
    {
      info.Initiate();
      Result<string> str = info.ReadArgString(0);
      if (str.IsErr())
        testText = str.Err;
      else
        testText = str.Ok;

      info.Terminate();
      yield return SyscallReturn.END;
    }

    private IEnumerator<SyscallReturn> Test2(SyscallInfo info)
    {
      info.Initiate();
      info.SetReturnUint16(0x1234);
      info.Terminate();
      yield return SyscallReturn.END;
    }
    #endregion
  } // class
} // namespcae
