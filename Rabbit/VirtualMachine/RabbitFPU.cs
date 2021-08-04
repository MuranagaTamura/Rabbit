using Rabbit.Core;
using System;

namespace Rabbit.VirtualMachine
{
  using ResultBool = Result<bool>;
  using ResultU8 = Result<byte>;
  using ResultU16 = Result<ushort>;

  public class RabbitFPU : IUnit<ushort>
  {
    private RabbitVM _vm = null;

    public void Init(IBaseVM<ushort> vm)
    {
      _vm = (RabbitVM)vm;
      AllRegisterFunc();
    }

    private void AllRegisterFunc()
    {
      // TODO: OpcodeでFloat関係が追加されるたびに追加する
      _vm.RegisterRunFunc((byte)Opcode.ADDF, ADDF);
      _vm.RegisterRunFunc((byte)Opcode.SUBF, SUBF);
      _vm.RegisterRunFunc((byte)Opcode.MULF, MULF);
      _vm.RegisterRunFunc((byte)Opcode.DIVF, DIVF);
      _vm.RegisterRunFunc((byte)Opcode.CMPF, CMPF);
    }

    private ResultBool CalcHelperF(byte[] code, Func<Half, Half, Half> calc)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;
      // dstレジスタIDを取得
      ResultU8 dstReg = ArrayConverter.GetUint8(code, ref ip);
      if (dstReg.IsErr())
        return (false, $"dstレジスタの参照に失敗しました: {dstReg.Err}");
      // srcレジスタIDを取得
      ResultU8 srcReg = ArrayConverter.GetUint8(code, ref ip);
      if (srcReg.IsErr())
        return (false, $"dstレジスタの参照に失敗しました: {srcReg.Err}");
      // targetレジスタIDを取得
      ResultU8 tarReg = ArrayConverter.GetUint8(code, ref ip);
      if (tarReg.IsErr())
        return (false, $"targetレジスタの参照に失敗しました: {tarReg.Err}");

      // srcとtargetの値を取得する
      ResultU16 src = _vm.GetReg((RegId)srcReg.Ok);
      if (src.IsErr())
        return (false, $"srcレジスタの値の参照に失敗しました: {src.Err}");
      ResultU16 tar = _vm.GetReg((RegId)tarReg.Ok);
      if (tar.IsErr())
        return (false, $"targetレジスタの値の参照に失敗しました: {tar.Err}");

      // 取得した値で加算してdstに設定
      ResultBool setReg = _vm.SetReg((RegId)dstReg.Ok, calc(src.Ok, tar.Ok));
      if (setReg.IsErr())
        return (false, setReg.Err);

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool ADDF(byte[] code)
      => CalcHelperF(code, (a, b) => a + b);

    private ResultBool SUBF(byte[] code)
      => CalcHelperF(code, (a, b) => a - b);

    private ResultBool MULF(byte[] code)
      => CalcHelperF(code, (a, b) => a * b);

    private ResultBool DIVF(byte[] code)
      => CalcHelperF(code, (a, b) => a / b);

    private ResultBool CMPF(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;
      // aレジスタIDを取得
      ResultU8 aReg = ArrayConverter.GetUint8(code, ref ip);
      if (aReg.IsErr())
        return (false, $"dstレジスタの参照に失敗しました: {aReg.Err}");
      // bレジスタIDを取得
      ResultU8 bReg = ArrayConverter.GetUint8(code, ref ip);
      if (bReg.IsErr())
        return (false, $"targetレジスタの参照に失敗しました: {bReg.Err}");

      // aとbの値を取得する
      ResultU16 a = _vm.GetReg((RegId)aReg.Ok);
      if (a.IsErr())
        return (false, $"srcレジスタの値の参照に失敗しました: {a.Err}");
      ResultU16 b = _vm.GetReg((RegId)bReg.Ok);
      if (b.IsErr())
        return (false, $"targetレジスタの値の参照に失敗しました: {b.Err}");

      // 取得した値で減算
      Half ret = a.Ok - b.Ok;
      // 最上位ビットが1かどうか、ゼロかどうかのフラグを設定
      _vm.IsSign = ret < 0;
      _vm.IsZero = ret == 0;

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }
  } // class
} // namespace
