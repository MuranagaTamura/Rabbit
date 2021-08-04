using Rabbit.Core;
using Rabbit.VirtualMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbit.Generator
{
  using ResultBool = Result<bool>;
  using CodeFunc = Action<ushort, byte[]>;

  struct Instruction
  {
    public string opcode;
    public string[] args;
    public ushort idx;
  }

  public class Disassembler
  {
    public const int BYTE_USHORT_SIZE = 2;

    private byte[] _codeBuf = null;
    private byte[] _constBuf = null;
    private ushort _codeIdx = 0;

    private List<ushort> _label = new List<ushort>();
    private List<Instruction> _instructions = new List<Instruction>();

    private Dictionary<byte, CodeFunc> _codeFunc = new Dictionary<byte, CodeFunc>();

    public void Init(byte[] binnary)
    {
      _codeBuf = new byte[0];
      _constBuf = new byte[0];
      _codeIdx = 0;
      _label.Clear();
      _codeFunc.Clear();

      ushort idx = 0;
      while (idx < binnary.Length)
      {
        PreOp op = (PreOp)binnary[idx++];
        switch (op)
        {
          // 定数領域の初期化
          case PreOp.INIT_CONST:
            ushort constSize = GetUint16(binnary, ref idx);
            byte[] constData = GetRangeU8(binnary, idx, constSize);

            _constBuf = constData;
            idx += constSize;
            break;
          // コード領域の初期化
          case PreOp.INIT_CODE:
            ushort codeSize = GetUint16(binnary, ref idx);
            byte[] codeData = GetRangeU8(binnary, idx, codeSize);

            _codeBuf = codeData;
            idx += codeSize;
            break;
          default:
            return;
        }
      }

      RegisterCodeFuncAll();
    }

    public void Init(byte[] constants, byte[] code)
    {
      _codeBuf = new byte[0];
      _constBuf = new byte[0];
      _codeIdx = 0;
      _label.Clear();
      _codeFunc.Clear();

      _constBuf = constants;
      _codeBuf = code;

      RegisterCodeFuncAll();
    }

    public void Disassemble()
    {
      _codeIdx = 0;
      while (_codeIdx < _codeBuf.Length)
      {
        byte op = _codeBuf[_codeIdx++];
        if (_codeFunc.TryGetValue(op, out CodeFunc func))
          func((ushort)(_codeIdx - 1), _codeBuf);
        else
          Default(_codeIdx, op);
      }
    }

    public string GetAll()
    {
      StringBuilder builder = new StringBuilder();
      builder.Append('=', 64);
      builder.AppendLine();
      builder.AppendLine($"  CONSTANTS");
      builder.AppendLine($"  SIZE: 0x{_constBuf.Length:X4}");
      builder.Append('=', 64);
      builder.AppendLine();
      builder.AppendLine(GetConst());
      builder.Append('=', 64);
      builder.AppendLine();
      builder.AppendLine($"  CODE");
      builder.AppendLine($"  SIZE: 0x{_codeBuf.Length:X4}");
      builder.Append('=', 64);
      builder.AppendLine();
      builder.AppendLine(GetCode());

      return builder.ToString();
    }

    public string GetCode()
    {
      if (_codeBuf.Length == 0) return "Error: not code init";

      StringBuilder builder = new StringBuilder();
      foreach (Instruction itr in _instructions)
      {
        // ラベル
        if (_label.Contains(itr.idx))
          builder.Append($"_{itr.idx:X4}: ");
        else
          builder.Append("       ");
        // 命令
        builder.Append(itr.opcode);
        // 引数
        builder.AppendLine(" " + string.Join(", ", itr.args));
      }

      return builder.ToString();
    }

    const int CONST_LINE_SIZE = 0x10;
    public string GetConst()
    {
      if (_codeBuf.Length == 0) return "Error: not const init";

      StringBuilder builder = new StringBuilder();
      for(int i = 0; i < CONST_LINE_SIZE; ++i)
        builder.Append($"{i:X2} ");
      builder.AppendLine();

      byte[] buf;
      for(int y = 0; y < _constBuf.Length; y += CONST_LINE_SIZE)
      {
        buf = new byte[CONST_LINE_SIZE];
        int x = 0;
        for(; y + x < _constBuf.Length && x < CONST_LINE_SIZE; ++x)
        {
          builder.Append($"{_constBuf[y + x]:X2} ");
          buf[x] = _constBuf[y + x];
        }
        builder.Append(' ', (CONST_LINE_SIZE - x) * 3);
        builder.AppendLine(Encoding.UTF8.GetString(buf));
      }

      return builder.ToString();
    }

    #region Convert Byte Array
    public byte GetUint8(byte[] code, ref ushort idx)
    {
      return code[idx++];
    }

    public ushort GetUint16(byte[] code, ref ushort idx)
    {
      idx += BYTE_USHORT_SIZE;
      return BitConverter.ToUInt16(code, idx - BYTE_USHORT_SIZE);
    }

    public byte[] GetRangeU8(byte[] mem, ushort idx, int length)
    {
      byte[] buf = new byte[length];
      Array.Copy(mem, idx, buf, 0, length);
      return buf;
    }

    public ushort[] GetRangeU16(byte[] code, ushort idx, int length)
    {
      int bufLen = length / BYTE_USHORT_SIZE
        + (length % BYTE_USHORT_SIZE == 0 ? 0 : 1);
      ushort[] buf = new ushort[bufLen];
      Buffer.BlockCopy(code, idx, buf, 0, length);
      return buf;
    }
    #endregion

    #region CodeFunc
    private void LOADI(ushort idx, byte[] code)
    {
      // dstレジスタIDを取得
      RegId dstReg = (RegId)GetUint8(code, ref _codeIdx);
      // 即値を取得
      ushort imm = GetUint16(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "LOADI",
        args = new string[] { $"{dstReg}", $"0x{imm:X4}" },
        idx = idx,
      });
    }

    private void LOAD(ushort idx, byte[] code)
    {
      // dstレジスタIDを取得
      RegId dstReg = (RegId)GetUint8(code, ref _codeIdx);
      // ptrレジスタIDを取得
      RegId srcPtr = (RegId)GetUint8(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "LOAD",
        args = new string[] { $"{dstReg}", $"[{srcPtr}]" },
        idx = idx,
      });
    }

    private void STOREI(ushort idx, byte[] code)
    {
      // dstレジスタIDを取得
      RegId dstReg = (RegId)GetUint8(code, ref _codeIdx);
      // 即値を取得
      ushort imm = GetUint16(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "STOREI",
        args = new string[] { $"{dstReg}", $"0x{imm:X4}" },
        idx = idx,
      });
    }

    private void STORE(ushort idx, byte[] code)
    {
      // ptrレジスタIDを取得
      RegId dstPtr = (RegId)GetUint8(code, ref _codeIdx);
      // srcレジスタIDを取得
      RegId srcReg = (RegId)GetUint8(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "STORE",
        args = new string[] { $"[{dstPtr}]", $"{srcReg}" },
        idx = idx,
      });
    }

    private void MOVE(ushort idx, byte[] code)
    {
      // dstレジスタIDを取得
      RegId dstReg = (RegId)GetUint8(code, ref _codeIdx);
      // srcレジスタIDを取得
      RegId srcReg = (RegId)GetUint8(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "MOVE",
        args = new string[] { $"{dstReg}", $"{srcReg}" },
        idx = idx,
      });
    }

    private void ClacHelper(ushort idx, byte[] code, string calcop)
    {
      // dstレジスタIDを取得
      RegId dstReg = (RegId)GetUint8(code, ref _codeIdx);
      // srcレジスタIDを取得
      RegId srcReg = (RegId)GetUint8(code, ref _codeIdx);
      // targetレジスタIDを取得
      RegId tarReg = (RegId)GetUint8(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = calcop,
        args = new string[] { $"{dstReg}", $"{srcReg}", $"{tarReg}" },
        idx = idx,
      });
    }

    private void ADD(ushort idx, byte[] code)
      => ClacHelper(idx, code, "ADD");

    private void ADDF(ushort idx, byte[] code)
      => ClacHelper(idx, code, "ADDF");

    private void SUB(ushort idx, byte[] code)
      => ClacHelper(idx, code, "SUB");

    private void SUBF(ushort idx, byte[] code)
      => ClacHelper(idx, code, "SUBF");

    private void MUL(ushort idx, byte[] code)
      => ClacHelper(idx, code, "MUL");

    private void MULF(ushort idx, byte[] code)
      => ClacHelper(idx, code, "MULF");

    private void DIV(ushort idx, byte[] code)
      => ClacHelper(idx, code, "DIV");

    private void DIVF(ushort idx, byte[] code)
      => ClacHelper(idx, code, "DIVF");

    private void AND(ushort idx, byte[] code)
      => ClacHelper(idx, code, "AND");

    private void OR(ushort idx, byte[] code)
      => ClacHelper(idx, code, "OR");

    private void XOR(ushort idx, byte[] code)
      => ClacHelper(idx, code, "XOR");

    private void CMP(ushort idx, byte[] code)
    {
      // aレジスタIDを取得
      RegId a = (RegId)GetUint8(code, ref _codeIdx);
      // bレジスタIDを取得
      RegId b = (RegId)GetUint8(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "CMP",
        args = new string[] { $"{a}", $"{b}" },
        idx = idx,
      });
    }

    private void CMPF(ushort idx, byte[] code)
    {
      // aレジスタIDを取得
      RegId a = (RegId)GetUint8(code, ref _codeIdx);
      // bレジスタIDを取得
      RegId b = (RegId)GetUint8(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "CMPF",
        args = new string[] { $"{a}", $"{b}" },
        idx = idx,
      });
    }

    private void PUSH(ushort idx, byte[] code)
    {
      // RegIdを取得
      RegId reg = (RegId)GetUint8(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "PUSH",
        args = new string[] { $"{reg}" },
        idx = idx,
      });
    }

    private void POP(ushort idx, byte[] code)
    {
      // RegIdを取得
      RegId reg = (RegId)GetUint8(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "POP",
        args = new string[] { $"{reg}" },
        idx = idx,
      });
    }

    private void JumpHelper(ushort idx, byte[] code, string op)
    {
      // ジャンプ先を取得
      ushort address = GetUint16(code, ref _codeIdx);

      // ラベル先を登録
      _label.Add(address);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = op,
        args = new string[] { $"0x{address:X4}" },
        idx = idx,
      });
    }

    private void JMP(ushort idx, byte[] code)
      => JumpHelper(idx, code, "JMP");

    private void JE(ushort idx, byte[] code)
      => JumpHelper(idx, code, "JZ");

    private void JNE(ushort idx, byte[] code)
      => JumpHelper(idx, code, "JNZ");

    private void JG(ushort idx, byte[] code)
      => JumpHelper(idx, code, "JG");

    private void JGE(ushort idx, byte[] code)
      => JumpHelper(idx, code, "JGE");

    private void JL(ushort idx, byte[] code)
      => JumpHelper(idx, code, "JL");

    private void JLE(ushort idx, byte[] code)
      => JumpHelper(idx, code, "JLE");

    private void SYSCALL(ushort idx, byte[] code)
    {
      // idを取得
      ushort id = GetUint16(code, ref _codeIdx);

      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = "SYSCALL",
        args = new string[] { $"0x{id:X4}" },
        idx = idx,
      });
    }

    private void Default(ushort idx, byte code)
    {
      // 構造体を生成
      _instructions.Add(new Instruction()
      {
        opcode = $"{(Opcode)code}",
        args = new string[0],
        idx = idx,
      });
    }
    #endregion

    private ResultBool RegisterCodeFunc(byte opcode, CodeFunc func)
    {
      if (_codeFunc.ContainsKey(opcode))
        return (false, $"すでに登録されているOPCODEです: {opcode}");

      _codeFunc[opcode] = func;
      return (true, null);
    }

    private void RegisterCodeFuncAll()
    {
      // TODO: Opcodeが追加されるたびに追加する
      RegisterCodeFunc((byte)Opcode.LOADI, LOADI);
      RegisterCodeFunc((byte)Opcode.LOAD, LOAD);
      RegisterCodeFunc((byte)Opcode.STOREI, STOREI);
      RegisterCodeFunc((byte)Opcode.STORE, STORE);
      RegisterCodeFunc((byte)Opcode.MOVE, MOVE);
      RegisterCodeFunc((byte)Opcode.ADD, ADD);
      RegisterCodeFunc((byte)Opcode.ADDF, ADDF);
      RegisterCodeFunc((byte)Opcode.SUB, SUB);
      RegisterCodeFunc((byte)Opcode.SUBF, SUBF);
      RegisterCodeFunc((byte)Opcode.MUL, MUL);
      RegisterCodeFunc((byte)Opcode.MULF, MULF);
      RegisterCodeFunc((byte)Opcode.DIV, DIV);
      RegisterCodeFunc((byte)Opcode.DIVF, DIVF);
      RegisterCodeFunc((byte)Opcode.AND, AND);
      RegisterCodeFunc((byte)Opcode.OR, OR);
      RegisterCodeFunc((byte)Opcode.XOR, XOR);
      RegisterCodeFunc((byte)Opcode.CMP, CMP);
      RegisterCodeFunc((byte)Opcode.CMPF, CMPF);
      RegisterCodeFunc((byte)Opcode.PUSH, PUSH);
      RegisterCodeFunc((byte)Opcode.POP, POP);
      RegisterCodeFunc((byte)Opcode.JMP, JMP);
      RegisterCodeFunc((byte)Opcode.JE, JE);
      RegisterCodeFunc((byte)Opcode.JNE, JNE);
      RegisterCodeFunc((byte)Opcode.JG, JG);
      RegisterCodeFunc((byte)Opcode.JGE, JGE);
      RegisterCodeFunc((byte)Opcode.JL, JL);
      RegisterCodeFunc((byte)Opcode.JLE, JLE);
      RegisterCodeFunc((byte)Opcode.SYSCALL, SYSCALL);
    }
  }
}
