using Rabbit.Core;
using Rabbit.Generator;
using Rabbit.VirtualMachine;
using System;
using System.Collections.Generic;

namespace Rabbit
{
  class Program
  {
    static void Main(string[] args)
    {
      // ; A0 = n, A1 = sum, A2 = i, A3 = 1
      //          SYSCALL readline
      //          LOADI   A1, 0      ; int sum = 0
      //          LOADI   A2, 0      ; int i
      //          LOADI   A3, 1      ; const 1 
      //    LOOP: CMP     A2, A0     ; cmp i, n
      //          JLE     LOOP_END
      //          ADD     A1, A1, A2 ; sum = sum + i
      //          ADD     A2, A2, A3 ; i = i + 1
      //          JMP     LOOP
      //LOOP_END: SYSCALL writeline

      Assembler asm = new Assembler();
      asm.SYSCALL(0);
      asm.LOADI(RegId.A1, 0);
      asm.LOADI(RegId.A2, 0);
      asm.LOADI(RegId.A3, 1);
      asm.SetLabel("LOOP");
      asm.CMP(RegId.A2, RegId.A0);
      asm.JLE("LOOP_END");
      asm.ADD(RegId.A1, RegId.A1, RegId.A2);
      asm.ADD(RegId.A2, RegId.A2, RegId.A3);
      asm.JMP("LOOP");
      asm.SetLabel("LOOP_END");
      asm.PUSH(RegId.A1);
      asm.SYSCALL(1);

      byte[] bin = asm.GetResult();

      // Disassemble
      Console.WriteLine("Disassemble code");
      Disassembler dasm = new Disassembler();
      dasm.Init(bin);
      dasm.Disassemble();
      Console.WriteLine(dasm.GetAll());

      // Run
      Console.WriteLine("Run Start");
      RabbitVM vm = new RabbitVM();
      Result<bool> init = vm.Init(bin, 128);
      if (init.IsErr())
      {
        Console.WriteLine($"RabbitVM.InitError: {init.Err}");
        return;
      }
      vm.RegisterSysFunc(0, ReadLine);
      vm.RegisterSysFunc(1, WriteLine);
      Result<bool> step = (true, null);
      while (true)
      {
        step = vm.StepRun();
        if (step.IsErr())
        {
          Console.WriteLine($"RabbitVM.RunError({vm.Ip}): {step.Err}");
          return;
        }
        if (!step.Ok) break;
      }
      Console.WriteLine("Run Finish");
    }

    private static IEnumerator<SyscallReturn> WriteLine(SyscallInfo info)
    {
      info.Initiate();
      
      Result<ushort> val = info.ReadArgUint16(0);
      if(!val.IsErr())
        Console.WriteLine($"RabbitVm.Print: {val.Ok}");
      else
      {
        Console.WriteLine("引数の取得に失敗しました");
        yield return SyscallReturn.ERROR;
      }

      info.Terminate();
      yield return SyscallReturn.END;
    }

    private static IEnumerator<SyscallReturn> ReadLine(SyscallInfo info)
    {
      info.Initiate();

      Console.Write("連続加算の長さを指定してください:");
      string num = Console.ReadLine();
      if(int.TryParse(num, out int ret))
      {
        info.SetReturnUint16((ushort)ret);
        info.Terminate();
        yield return SyscallReturn.END;
      }

      info.SetError("数値以外が入力されました");
      yield return SyscallReturn.ERROR;
    }
  }
}
