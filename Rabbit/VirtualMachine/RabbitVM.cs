using Rabbit.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbit.VirtualMachine
{
  using ResultBool = Result<bool>;
  using ResultU16 = Result<ushort>;
  using RunFunc = Func<byte[], Result<bool>>;
  using SysFunc = Func<SyscallInfo, IEnumerator<SyscallReturn>>;

  public enum RegId
  {
    Ip,
    Sp,
    Fp,
    A0,
    A1,
    A2,
    A3,
    A4,
    A5,
    A6,
    A7,
    A8,
    A9,
    A10,
    A11,
    A12,

    MAX,
  }

  public class RabbitVM : IBaseVM<ushort>
  {
    private const byte IS_CALL_SYSCALL_TRUE = 0b0001_0000;
    private const byte IS_CALL_SYSCALL_FALSE = 0b1110_1111;
    private const byte IS_SIGN_TRUE = 0b0000_0010;
    private const byte IS_SIGN_FALSE = 0b1111_1101;
    private const byte IS_ZERO_TRUE = 0b0000_0001;
    private const byte IS_ZERO_FALSE = 0b1111_1110;

    private ushort[] _constants = null;
    private byte[] _code = null;
    private ushort[] _memory = null;
    private ushort[] _registers = null;

    private byte _flags = 0b0000_0000;

    private List<IUnit<ushort>> _units = new List<IUnit<ushort>>();

    private Dictionary<byte, RunFunc> _runFunc = new Dictionary<byte, RunFunc>();

    private Dictionary<ushort, SysFunc> _sysFunc = new Dictionary<ushort, SysFunc>();
    private IEnumerator<SyscallReturn> _currentSysCall = null;
    
    public ushort Ip
    {
      get => GetReg(RegId.Ip).Unwrap();
      set => SetReg(RegId.Ip, value);
    }

    public ushort Sp
    {
      get => GetReg(RegId.Sp).Unwrap();
      set => SetReg(RegId.Sp, value);
    }

    public ushort Fp
    {
      get => GetReg(RegId.Fp).Unwrap();
      set => SetReg(RegId.Fp, value);
    }

    public bool IsCallSysCall
    {
      get => (_flags & IS_CALL_SYSCALL_TRUE) != 0;
      set => _flags = value ?
        (byte)(_flags | IS_CALL_SYSCALL_TRUE) :
        (byte)(_flags & IS_CALL_SYSCALL_FALSE);
    }

    public bool IsSign
    {
      get => (_flags & IS_SIGN_TRUE) != 0;
      set => _flags = value ?
        (byte)(_flags | IS_SIGN_TRUE) :
        (byte)(_flags & IS_SIGN_FALSE);
    }

    public bool IsZero
    {
      get => (_flags & IS_ZERO_TRUE) != 0;
      set => _flags = value ?
        (byte)(_flags | IS_ZERO_TRUE) :
        (byte)(_flags & IS_ZERO_FALSE);
    }

    public string Error { get; set; } = null;
    public bool HasErr() => !string.IsNullOrEmpty(Error);

    public RabbitVM()
    {
      // IUnitの登録
      RabbitALU alu = new RabbitALU();
      RegisterUnit(alu);
      RabbitFPU fpu = new RabbitFPU();
      RegisterUnit(fpu);

      // RunFuncの登録
      RegisterRunFunc((byte)Opcode.SYSCALL, SYSCALL);
    }

    public ResultBool Init(byte[] code, ushort memorySize)
    {
      // 定数領域、メモリ領域初期化
      ResultBool init = RreLoad(code);
      if (init.IsErr()) return init;

      // メモリ領域の初期化
      _memory = new ushort[memorySize];

      // レジスタ領域の初期化
      _registers = new ushort[(byte)RegId.MAX];
      Ip = 0;
      Sp = (ushort)(memorySize - 1);
      Fp = (ushort)(memorySize - 1);

      return (true, null);
    }

    public ResultBool StepRun()
    {
      // Syscallを進める
      if (RunSyscall())
        return (true, null);

      // Errorがあるか
      if (HasErr())
        return (false, Error);

      // 現在の実行位置を取得
      ushort ip = GetReg(RegId.Ip).Unwrap();
      SetReg(RegId.Ip, (ushort)(ip + 1));

      // コード領域の範囲内か
      if (Math.Clamp(ip, 0, _code.Length - 1) != ip)
        //return (false, $"実行位置が範囲外です: 実際 => {ip}, 期待 => [0, {_code.Length - 1}]");
        return (false, null);

      // Opcodeを取得
      byte op = _code[ip];

      // 実行できる？
      if (_runFunc.TryGetValue(op, out RunFunc func))
        return func(_code);

      return (false, $"実行可能でないOPCODEです: {op}");
    }

    public ResultBool RegisterRunFunc(byte opcode, RunFunc func)
    {
      if (_runFunc.ContainsKey(opcode))
        return (false, $"すでに登録されているOPCODEです: {opcode}");

      _runFunc[opcode] = func;
      return (true, null);
    }

    public ResultBool RegisterSysFunc(ushort id, SysFunc func)
    {
      if (_sysFunc.ContainsKey(id))
        return (false, $"すでに登録されているIDです: {id}");

      _sysFunc[id] = func;
      return (true, null);
    }

    public ResultBool RegisterUnit(IUnit<ushort> unit)
    {
      unit.Init(this);
      _units.Add(unit);
      return (true, null);
    }

    public override string ToString()
    {
      StringBuilder builder = new StringBuilder();
      builder.Append($"\"RebbitVM\":{{");
      builder.Append($"\"ConstantsSize\": {_constants.Length}, ");
      builder.Append($"\"CodeSize\": {_code.Length}, ");
      builder.Append($"\"MemorySize\": {_memory.Length}");
      builder.Append($"}}");
      return builder.ToString();
    }

    #region Access Register
    public ResultU16 GetReg(RegId regId)
    {
      if (Math.Clamp((byte)regId, (byte)0, (byte)RegId.MAX) != (byte)regId)
        return (0, $"指定されたレジスタが無効です: {regId}");

      return (_registers[(byte)regId], null);
    }

    public ResultU16 GetReg(ushort regId)
      => GetReg((RegId)regId);

    public ResultBool SetReg(RegId regId, ushort value)
    {
      if (Math.Clamp((byte)regId, (byte)0, (byte)RegId.MAX) != (byte)regId)
        return (false, $"指定されたレジスタが無効です: {regId}");

      _registers[(byte)regId] = value;

      return (true, null);
    }

    public ResultBool SetReg(ushort regId, ushort value)
      => SetReg((RegId)regId, value);
    #endregion

    #region Access Memory & Constants
    public Result<ushort[]> GetArray(ushort[] src, ushort ptr, ushort length)
    {
      return ArrayConverter.GetRangeU16(src, ptr, length);
    }

    public ResultBool SetArray(ushort[] src, ushort[] dst, ushort ptr, ushort length)
    {
      if (Math.Clamp(ptr, 0, dst.Length - 1) != ptr)
        return (false, $"参照位置が無効です: 実際 => {ptr}, 期待 => [0, {dst.Length - 1}]");
      if (Math.Min(ptr + length, dst.Length) != ptr + length)
        return (false, $"参照位置が無効です: 実際 => {ptr} + {length}, 期待 => [0, {dst.Length - 1}]");

      Array.Copy(src, 0, dst, ptr, length);
      return (true, "");
    }

    public ResultU16 GetConst(ushort ptr)
    {
      Result<ushort[]> ret = GetConst(ptr, 1);
      if (ret.IsErr())
        return (0, ret.Err);

      return (ret.Ok[0], null);
    }

    public Result<ushort[]> GetConst(ushort ptr, ushort length)
    {
      return ArrayConverter.GetRangeU16(_constants, ptr, length);
    }

    public ResultBool SetConst(ushort[] src, ushort ptr, ushort length)
    {
      return SetArray(src, _constants, ptr, length);
    }

    public ResultU16 GetMem(ushort ptr)
    {
      Result<ushort[]> ret = GetMem(ptr, 1);
      if (ret.IsErr())
        return (0, ret.Err);

      return (ret.Ok[0], null);
    }

    public Result<ushort[]> GetMem(ushort ptr, ushort length)
    {
      return ArrayConverter.GetRangeU16(_memory, ptr, length);
    }

    public ResultBool SetMem(ushort[] src, ushort ptr, ushort length)
    {
      return SetArray(src, _memory, ptr, length);
    }

    public ResultBool SetMem(ushort src, ushort ptr)
    {
      return SetArray(new ushort[] { src }, _memory, ptr, 1);
    }
    #endregion

    private ResultBool SYSCALL(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = Ip;
      // 呼び込む関数IDを取得
      ResultU16 id = ArrayConverter.GetUint16(code, ref ip);
      if (id.IsErr())
        return (false, $"syscallのidの参照に失敗しました: {id.Err}");

      // Syscallの下準備
      SysFunc func;
      if (!_sysFunc.TryGetValue(id.Ok, out func))
        return (false, $"このidは設定されていません: {id.Ok}");
      _currentSysCall = func(new SyscallInfo(this));
      IsCallSysCall = true;

      // 実行位置を変更
      Ip = ip;

      return (true, null);
    }

    private ResultBool RreLoad(byte[] code)
    {
      _constants = new ushort[0];
      _code = new byte[0];
      _memory = new ushort[0];

      ushort idx = 0;
      while (idx < code.Length)
      {
        PreOp op = (PreOp)code[idx++];
        switch (op)
        {
          // 定数領域の初期化
          case PreOp.INIT_CONST:
            ResultU16 constSize = ArrayConverter.GetUint16(code, ref idx);
            if (constSize.IsErr())
              return (false, $"定数領域を初期化中にエラーが発生しました:\n" +
                $" Err => {constSize.Err}");

            Result<ushort[]> constData = ArrayConverter.GetRangeU16(code, idx, constSize.Ok);
            if (constData.IsErr())
              return (false, $"定数領域を初期化中にエラーが発生しました:\n" +
                $" Err => {constData.Err}");

            _constants = constData.Ok;
            idx += constSize.Ok;
            break;
          // コード領域の初期化
          case PreOp.INIT_CODE:
            ResultU16 codeSize = ArrayConverter.GetUint16(code, ref idx);
            if (codeSize.IsErr())
              return (false, $"コード領域を初期化中にエラーが発生しました:\n" +
                $" Err => {codeSize.Err}");

            Result<byte[]> codeData = ArrayConverter.GetRangeU8(code, idx, codeSize.Ok);
            if (codeData.IsErr())
              return (false, $"コード領域を初期化中にエラーが発生しました:\n" +
                $" Err => {codeData.Err}");

            _code = codeData.Ok;
            idx += codeSize.Ok;
            break;
          default:
            return (false, $"領域初期化に失敗しました:\n" +
              $" Opcode => {op}");
        }
      }

      return (true, "");
    }

    private bool RunSyscall()
    {
      // Syscallを実行中？
      if (IsCallSysCall)
      {
        // Syscallの実行を進める
        if (_currentSysCall.MoveNext())
        {
          // Syscallを返り値を取得して、終了なのか継続なのか判断する
          SyscallReturn sysCallRet = _currentSysCall.Current;
          switch (sysCallRet)
          {
            case SyscallReturn.END:
              IsCallSysCall = false;
              return true;
            case SyscallReturn.ERROR:
              IsCallSysCall = false;
              return false;
            case SyscallReturn.CONTINUE:
              return true;
            default:
              return false;
          }
        }
        else
        {
          // 終了
          IsCallSysCall = false;
          return false;
        }
      }

      // Syscallは実行されていない
      return false;
    }
  } // class
} // namespace
