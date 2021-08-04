using Rabbit.Core;
using System;

namespace Rabbit.VirtualMachine
{
  using ResultBool = Result<bool>;
  using ResultU8 = Result<byte>;
  using ResultU16 = Result<ushort>;
  using ArithmeticFunc = Func<ushort, ushort, ushort>;

  public class RabbitALU : IUnit<ushort>
  {
    private RabbitVM _vm = null;

    public void Init(IBaseVM<ushort> vm)
    {
      _vm = (RabbitVM)vm;
      AllRegisterFunc();
    }

    private void AllRegisterFunc()
    {
      // TODO: Opcodeが追加されるたびに追加する
      _vm.RegisterRunFunc((byte)Opcode.LOADI, LOADI);
      _vm.RegisterRunFunc((byte)Opcode.LOAD, LOAD);
      _vm.RegisterRunFunc((byte)Opcode.STOREI, STOREI);
      _vm.RegisterRunFunc((byte)Opcode.STORE, STORE);
      _vm.RegisterRunFunc((byte)Opcode.MOVE, MOVE);
      _vm.RegisterRunFunc((byte)Opcode.ADD, ADD);
      _vm.RegisterRunFunc((byte)Opcode.SUB, SUB);
      _vm.RegisterRunFunc((byte)Opcode.MUL, MUL);
      _vm.RegisterRunFunc((byte)Opcode.DIV, DIV);
      _vm.RegisterRunFunc((byte)Opcode.AND, AND);
      _vm.RegisterRunFunc((byte)Opcode.OR, OR);
      _vm.RegisterRunFunc((byte)Opcode.XOR, XOR);
      _vm.RegisterRunFunc((byte)Opcode.CMP, CMP);
      _vm.RegisterRunFunc((byte)Opcode.PUSH, PUSH);
      _vm.RegisterRunFunc((byte)Opcode.POP, POP);
      _vm.RegisterRunFunc((byte)Opcode.JMP, JMP);
      _vm.RegisterRunFunc((byte)Opcode.JE, JE);
      _vm.RegisterRunFunc((byte)Opcode.JNE, JNE);
      _vm.RegisterRunFunc((byte)Opcode.JG, JG);
      _vm.RegisterRunFunc((byte)Opcode.JGE, JGE);
      _vm.RegisterRunFunc((byte)Opcode.JL, JL);
      _vm.RegisterRunFunc((byte)Opcode.JLE, JLE);
    }

    #region RunFunc
    private ResultBool CalcHelper(byte[] code, ArithmeticFunc calc)
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

    private ResultBool JmpHepler(byte[] code, Func<bool> cond)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;
      // ジャンプ先を取得
      ResultU16 address = ArrayConverter.GetUint16(code, ref ip);
      if (address.IsErr())
        return (false, $"address値の参照に失敗しました: {address.Err}");

      // 実行位置を変更
      if (cond())
        _vm.Ip = address.Ok;
      else
        _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool LOADI(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;
      // dstレジスタIDを取得
      ResultU8 dstReg = ArrayConverter.GetUint8(code, ref ip);
      if (dstReg.IsErr())
        return (false, $"dstレジスタの参照に失敗しました: {dstReg.Err}");
      // 即値を取得
      ResultU16 imm = ArrayConverter.GetUint16(code, ref ip);
      if (imm.IsErr())
        return (false, $"imm値の参照に失敗しました: {imm.Err}");

      // dstレジスタに設定
      ResultBool setReg = _vm.SetReg((RegId)dstReg.Ok, imm.Ok);
      if (setReg.IsErr())
        return (false, setReg.Err);

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool LOAD(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;
      // dstレジスタIDを取得
      ResultU8 dstReg = ArrayConverter.GetUint8(code, ref ip);
      if (dstReg.IsErr())
        return (false, $"dstレジスタの参照に失敗しました: {dstReg.Err}");
      // ptrレジスタIDを取得
      ResultU8 ptrReg = ArrayConverter.GetUint8(code, ref ip);
      if (ptrReg.IsErr())
        return (false, $"ptrレジスタの参照に失敗しました: {ptrReg.Err}");

      // ptrレジスタの値を取得
      ResultU16 ptrVal = _vm.GetReg((RegId)ptrReg.Ok);
      if (ptrVal.IsErr())
        return (false, ptrVal.Err);
      // ptr先のメモリの値を取得
      ResultU16 srcVal = _vm.GetMem(ptrVal.Ok);
      if (srcVal.IsErr())
        return (false, srcVal.Err);

      // dstレジスタに設定
      ResultBool setReg = _vm.SetReg((RegId)dstReg.Ok, srcVal.Ok);
      if (setReg.IsErr())
        return (false, setReg.Err);

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool STOREI(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;
      // dstレジスタIDを取得
      ResultU8 dstReg = ArrayConverter.GetUint8(code, ref ip);
      if (dstReg.IsErr())
        return (false, $"dstレジスタの参照に失敗しました: {dstReg.Err}");
      // 即値を取得
      ResultU16 imm = ArrayConverter.GetUint16(code, ref ip);
      if (imm.IsErr())
        return (false, $"imm値の参照に失敗しました: {imm.Err}");

      // dstレジスタの値を取得
      ResultU16 dstPtr = _vm.GetReg((RegId)dstReg.Ok);
      if (dstPtr.IsErr())
        return (false, dstPtr.Err);

      // ptr先に設定
      ResultBool setReg = _vm.SetMem(imm.Ok, dstPtr.Ok);
      if (setReg.IsErr())
        return (false, setReg.Err);

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool STORE(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;
      // ptrレジスタIDを取得
      ResultU8 ptrReg = ArrayConverter.GetUint8(code, ref ip);
      if (ptrReg.IsErr())
        return (false, $"ptrレジスタの参照に失敗しました: {ptrReg.Err}");
      // srcレジスタIDを取得
      ResultU8 srcReg = ArrayConverter.GetUint8(code, ref ip);
      if (srcReg.IsErr())
        return (false, $"srcレジスタの参照に失敗しました: {srcReg.Err}");

      // ptrレジスタの値を取得
      ResultU16 ptrVal = _vm.GetReg((RegId)ptrReg.Ok);
      if (ptrVal.IsErr())
        return (false, ptrVal.Err);
      // srcレジスタの値を取得
      ResultU16 srcVal = _vm.GetReg((RegId)srcReg.Ok);
      if (srcVal.IsErr())
        return (false, srcVal.Err);

      // ptr先に設定
      ResultBool setReg = _vm.SetMem(srcVal.Ok, ptrVal.Ok);
      if (setReg.IsErr())
        return (false, setReg.Err);

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool MOVE(byte[] code)
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
        return (false, $"srcレジスタの参照に失敗しました: {srcReg.Err}");

      // srcレジスタの値を取得
      ResultU16 srcVal = _vm.GetReg((RegId)srcReg.Ok);
      if (srcVal.IsErr())
        return (false, srcVal.Err);
      // dstレジスタに設定
      ResultBool setReg = _vm.SetReg((RegId)dstReg.Ok, srcVal.Ok);
      if (setReg.IsErr())
        return (false, setReg.Err);

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool ADD(byte[] code)
      => CalcHelper(code, (a, b) => (ushort)(a + b));

    private ResultBool SUB(byte[] code)
      => CalcHelper(code, (a, b) => (ushort)(a - b));

    private ResultBool MUL(byte[] code)
      => CalcHelper(code, (a, b) => (ushort)(a * b));

    private ResultBool DIV(byte[] code)
      => CalcHelper(code, (a, b) => (ushort)(a / b));

    private ResultBool AND(byte[] code)
      => CalcHelper(code, (a, b) => (ushort)(a & b));

    private ResultBool OR(byte[] code)
      => CalcHelper(code, (a, b) => (ushort)(a | b));

    private ResultBool XOR(byte[] code)
      => CalcHelper(code, (a, b) => (ushort)(a ^ b));

    private ResultBool CMP(byte[] code)
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
      int ret = a.Ok - b.Ok;
      // 最上位ビットが1かどうか、ゼロかどうかのフラグを設定
      _vm.IsSign = ret < 0;
      _vm.IsZero = ret == 0;

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool PUSH(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;

      // レジスタIDを取得
      ResultU8 reg = ArrayConverter.GetUint8(code, ref ip);
      if (reg.IsErr())
        return (false, $"dstレジスタの参照に失敗しました: {reg.Err}");
      // レジスタの値を取得
      ResultU16 val = _vm.GetReg((RegId)reg.Ok);
      if (val.IsErr())
        return (false, val.Err);

      // メモリに値を書き込む
      ResultBool setMem = _vm.SetMem(val.Ok, _vm.Sp--);
      if (setMem.IsErr())
        return (false, setMem.Err);

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool POP(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = _vm.Ip;

      // レジスタIDを取得
      ResultU8 reg = ArrayConverter.GetUint8(code, ref ip);
      if (reg.IsErr())
        return (false, $"dstレジスタの参照に失敗しました: {reg.Err}");

      // メモリに値を取得
      ResultU16 getMem = _vm.GetMem(++_vm.Sp);
      if (getMem.IsErr())
        return (false, getMem.Err);

      // レジスタIDに値を設定
      ResultBool setReg = _vm.SetReg((RegId)reg.Ok, getMem.Ok);
      if (setReg.IsErr())
        return (false, setReg.Err);

      // 実行位置を変更
      _vm.Ip = ip;

      return (true, null);
    }

    private ResultBool JMP(byte[] code)
      => JmpHepler(code, () => true);

    private ResultBool JE(byte[] code)
      => JmpHepler(code, () => _vm.IsZero);

    private ResultBool JNE(byte[] code)
      => JmpHepler(code, () => !_vm.IsZero);

    private ResultBool JG(byte[] code)
      => JmpHepler(code, () => !_vm.IsZero && !_vm.IsSign);

    private ResultBool JGE(byte[] code)
      => JmpHepler(code, () => _vm.IsZero || !_vm.IsSign);

    private ResultBool JL(byte[] code)
      => JmpHepler(code, () => !_vm.IsZero && _vm.IsSign);

    private ResultBool JLE(byte[] code)
      => JmpHepler(code, () => _vm.IsZero || !_vm.IsSign);
    #endregion
  } // class
} // naespace
