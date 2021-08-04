using Rabbit.Core;
using System;
using System.Text;

namespace Rabbit.VirtualMachine
{
  public enum SyscallReturn
  {
    ERROR,
    CONTINUE,
    END,
  }

  public struct SyscallInfo
  {
    private RabbitVM _vm;

    public SyscallInfo(RabbitVM vm) 
    {
      _vm = vm;
    }

    public Result<ushort> ReadArgUint16(ushort argIdx)
    {
      ushort ptr = (ushort)(_vm.Fp - argIdx + 2);
      Result<ushort> arg = _vm.GetMem(ptr);
      if (arg.IsErr())
        return (0, $"引数が設定されていません: {arg.Err}");

      return (arg.Ok, null);
    }

    public Result<string> ReadArgString(ushort argIdx)
    {
      ushort ptr = (ushort)(_vm.Fp - argIdx + 2);
      Result<ushort> constPtr = _vm.GetMem(ptr);
      if (constPtr.IsErr())
        return (null, $"定数が設定されていません: {constPtr.Err}");

      Result<ushort> constLen = _vm.GetConst(constPtr.Ok);
      if (constLen.IsErr())
        return (null, $"文字列の長さの取得に失敗しました: {constLen.Err}");

      Result<ushort[]> constStr = _vm.GetConst((ushort)(constPtr.Ok + 1), constLen.Ok);
      if (constStr.IsErr())
        return (null, $"文字列の取得に失敗しました: {constStr.Err}");

      byte[] strBin = new byte[constStr.Ok.Length * sizeof(ushort)];
      Buffer.BlockCopy(constStr.Ok, 0, strBin, 0, strBin.Length);

      return (Encoding.UTF8.GetString(strBin), null);
    }

    public void SetReturnUint16(ushort retval)
    {
      _vm.SetReg(RegId.A0, retval);
    }

    public void SetError(string msg)
    {
      _vm.Error = msg;
    }

    public void Initiate()
    {
      // PUSH FP
      ushort fpVal = _vm.GetMem(_vm.Fp).Unwrap();
      _vm.SetMem(fpVal, _vm.Sp--);

      // MOVE FP, SP
      _vm.Fp = _vm.Sp;
    }

    public void Terminate()
    {
      // MOVE SP, FP
      _vm.Sp = _vm.Fp;

      // POP FP
      ushort fpVal = _vm.GetMem(++_vm.Sp).Unwrap();
      _vm.SetReg(RegId.Fp, fpVal);
    }
  }
}
