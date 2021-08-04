using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rabbit.Core;

namespace TestRabbit
{
  [TestClass]
  public class TestResult
  {
    [TestMethod]
    public void TestImplict()
    {
      Result<bool> ok = (true, "");
      Result<bool> err = (false, "err msg");

      Assert.IsTrue(ok.IsOk(), "Œ^•ÏŠ·Ž¸”s");
      Assert.IsTrue(err.IsErr(), "Œ^•ÏŠ·Ž¸”s");

      Assert.IsFalse(ok.IsErr(), "Œ^•ÏŠ·Ž¸”s");
      Assert.IsFalse(err.IsOk(), "Œ^•ÏŠ·Ž¸”s");

      Assert.AreEqual(ok.Unwrap(), true, "Œ^•ÏŠ·Ž¸”s");

      (bool, string) okValue = ok;
      Assert.AreEqual(okValue.Item1, true, "Œ^•ÏŠ·Ž¸”s");
      Assert.AreEqual(okValue.Item2, "", "Œ^•ÏŠ·Ž¸”s");

      (bool, string) errValue = err;
      Assert.AreEqual(errValue.Item1, false, "Œ^•ÏŠ·Ž¸”s");
      Assert.AreEqual(errValue.Item2, "err msg", "Œ^•ÏŠ·Ž¸”s");
    }

    [TestMethod]
    [ExpectedException(typeof(ResultException))]
    public void TestException()
    {
      Result<bool> err = (false, "err msg");
      err.Unwrap();
      err.Expect("expect");
    }
  }
}
