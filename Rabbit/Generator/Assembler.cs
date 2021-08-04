using System;
using System.Collections.Generic;
using Rabbit.VirtualMachine;
using System.Text;

namespace Rabbit.Generator
{
  public class Assembler
  {
    public const int USHORT_BYTE_SIZE = 2;

    Dictionary<string, List<ushort>> _unsolvedLabel = new Dictionary<string, List<ushort>>();
    Dictionary<string, ushort> _labelName = new Dictionary<string, ushort>();

    private List<byte> _codeBuf = new List<byte>();
    private List<byte> _constBuf = new List<byte>();

    public void Init()
    {
      _codeBuf.Clear();
      _constBuf.Clear();
      _unsolvedLabel.Clear();
      _labelName.Clear();
    }

    public byte[] GetResult()
    {
      List<byte> buf = new List<byte>();

      // 定数領域
      buf.Add((byte)PreOp.INIT_CONST);
      AddUint16(buf, (ushort)_constBuf.Count);
      buf.AddRange(_constBuf);

      // コード領域
      buf.Add((byte)PreOp.INIT_CODE);
      AddUint16(buf, (ushort)_codeBuf.Count);
      buf.AddRange(_codeBuf);

      return buf.ToArray();
    }

    public void SetLabel(string label)
    {
      _labelName[label] = (ushort)_codeBuf.Count;

      // 未解決なラベルが指定された部分を解決する
      SolveLabel(label, (ushort)_codeBuf.Count);
    }

    public void SetConst(string str)
    {
      byte[] buf = Encoding.UTF8.GetBytes(str);
      _constBuf.AddRange(BitConverter.GetBytes((ushort)(buf.Length / sizeof(ushort))));
      _constBuf.AddRange(buf);
    }

    public void LOADI(RegId reg, ushort value)
      => RegImm(Opcode.LOADI, reg, value);

    public void LOAD(RegId dst, RegId ptr)
      => RegReg(Opcode.LOAD, dst, ptr);

    public void STOREI(RegId ptr, ushort value)
      => RegImm(Opcode.STORE, ptr, value);

    public void STORE(RegId ptr, RegId src)
      => RegReg(Opcode.STORE, ptr, src);

    public void MOVE(RegId dst, RegId src)
      => RegReg(Opcode.MOVE, dst, src);

    public void ADD(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.ADD, dst, src, target);

    public void ADDF(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.ADDF, dst, src, target);

    public void SUB(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.SUB, dst, src, target);

    public void SUBF(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.SUBF, dst, src, target);

    public void MUL(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.MUL, dst, src, target);

    public void MULF(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.MULF, dst, src, target);

    public void DIV(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.DIV, dst, src, target);

    public void DIVF(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.DIVF, dst, src, target);

    public void AND(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.AND, dst, src, target);

    public void OR(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.OR, dst, src, target);

    public void XOR(RegId dst, RegId src, RegId target)
      => RegRegReg(Opcode.XOR, dst, src, target);

    public void CMP(RegId a, RegId b)
      => RegReg(Opcode.CMP, a, b);

    public void CMPF(RegId a, RegId b)
      => RegReg(Opcode.CMP, a, b);

    public void PUSH(RegId reg)
      => Reg(Opcode.PUSH, reg);

    public void POP(RegId reg)
      => Reg(Opcode.POP, reg);

    public void JMP(string label)
      => Jump(Opcode.JMP, label);

    public void JE(string label)
      => Jump(Opcode.JE, label);

    public void JNE(string label)
      => Jump(Opcode.JNE, label);

    public void JG(string label)
      => Jump(Opcode.JG, label);

    public void JGE(string label)
      => Jump(Opcode.JGE, label);

    public void JL(string label)
      => Jump(Opcode.JL, label);

    public void JLE(string label)
      => Jump(Opcode.JLE, label);

    public void SYSCALL(ushort id)
      => Imm(Opcode.SYSCALL, id);

    #region Place Common Byte
    private void Imm(Opcode op, ushort imm)
    {
      _codeBuf.Add((byte)op);
      AddUint16(_codeBuf, imm);
    }

    private void Reg(Opcode op, RegId reg)
    {
      _codeBuf.Add((byte)op);
      _codeBuf.Add((byte)reg);
    }

    private void RegReg(Opcode op, RegId dst, RegId src)
    {
      _codeBuf.Add((byte)op);
      _codeBuf.Add((byte)dst);
      _codeBuf.Add((byte)src);
    }

    private void RegRegReg(Opcode op, RegId dst, RegId src, RegId target)
    {
      _codeBuf.Add((byte)op);
      _codeBuf.Add((byte)dst);
      _codeBuf.Add((byte)src);
      _codeBuf.Add((byte)target);
    }

    private void RegImm(Opcode op, RegId reg, ushort imm)
    {
      _codeBuf.Add((byte)op);
      _codeBuf.Add((byte)reg);
      AddUint16(_codeBuf, imm);
    }

    private void Jump(Opcode op, string label)
    {
      _codeBuf.Add((byte)op);

      // ラベルが解決できる
      if (_labelName.TryGetValue(label, out ushort codeIndex))
        AddUint16(_codeBuf, codeIndex);
      // ラベルが未定義で分からない
      else
        UnSolvedLabel(_codeBuf, label);
    }
    #endregion

    private void AddUint16(List<byte> buf, ushort val)
    {
      byte[] valBin = BitConverter.GetBytes(val);
      buf.AddRange(valBin);
    }

    private void InsertUint16(List<byte> list, int idx, ushort val)
    {
      byte[] valBin = BitConverter.GetBytes(val);
      for (int i = 0; i < USHORT_BYTE_SIZE; ++i)
        list[idx + i] = valBin[i];
    }

    private void UnSolvedLabel(List<byte> list, string label)
    {
      // 未定義、かつ、未知なラベルなら
      if (!_unsolvedLabel.ContainsKey(label))
        _unsolvedLabel[label] = new List<ushort>();

      // 未解決なラベルであるとして、今から挿入するオペランドの位置を未解決リストに登録
      _unsolvedLabel[label].Add((ushort)list.Count);

      // 未解決なまま実行されても、途中で止まるように0を設定
      AddUint16(list, ushort.MaxValue);
    }

    private void SolveLabel(string label, ushort idx)
    {
      // 未解決なラベルでないなら、すでに解決済みか
      // まだ使用されてないラベルである
      if (!_unsolvedLabel.ContainsKey(label)) return;

      // 未解決リストにあるものをすべて解決させる
      foreach (var itr in _unsolvedLabel[label])
        InsertUint16(_codeBuf, itr, idx);

      // 未解決なラベルでないので、削除
      _unsolvedLabel.Remove(label);
    }
  } // class
} // namespace
