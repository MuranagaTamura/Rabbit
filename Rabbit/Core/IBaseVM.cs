using System;

namespace Rabbit.Core
{
  using ResultBool = Result<bool>;
  using RunFunc = Func<byte[], Result<bool>>;

  public interface IBaseVM<T>
  {
    ResultBool Init(byte[] code, T memorySize);
    ResultBool StepRun();
    ResultBool RegisterRunFunc(byte opcode, RunFunc func);
    ResultBool RegisterUnit(IUnit<T> unit);
    Result<T> GetReg(T regId);
    ResultBool SetReg(T regId, T value);
    Result<T> GetMem(T ptr);
    ResultBool SetMem(T ptr, T value);
    Result<T> GetConst(T ptr);
  }
}
