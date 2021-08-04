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

      Assert.IsTrue(ok.IsOk(), "�^�ϊ����s");
      Assert.IsTrue(err.IsErr(), "�^�ϊ����s");

      Assert.IsFalse(ok.IsErr(), "�^�ϊ����s");
      Assert.IsFalse(err.IsOk(), "�^�ϊ����s");

      Assert.AreEqual(ok.Unwrap(), true, "�^�ϊ����s");

      (bool, string) okValue = ok;
      Assert.AreEqual(okValue.Item1, true, "�^�ϊ����s");
      Assert.AreEqual(okValue.Item2, "", "�^�ϊ����s");

      (bool, string) errValue = err;
      Assert.AreEqual(errValue.Item1, false, "�^�ϊ����s");
      Assert.AreEqual(errValue.Item2, "err msg", "�^�ϊ����s");
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
