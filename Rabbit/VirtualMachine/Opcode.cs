namespace Rabbit.VirtualMachine
{
  public enum Opcode
  {
    LOADI, // LOADI Reg(u8) Imm(u16)
    LOAD, // LOADM Reg(u8) Reg(Mem)(u8)
    STOREI, // STORE Reg(Mem)(u8) Imm(u16)
    STORE, // STORE Reg(Mem)(u8) Reg(u8)
    MOVE, // MOVE Reg(u8) Reg(u8)
    ADD, // ADD Reg(u8) Reg(u8) Reg(u8)
    ADDF, // ADDF Reg(u8) Reg(u8) Reg(u8)
    SUB, // SUB Reg(u8) Reg(u8) Reg(u8)
    SUBF, // SUBF Reg(u8) Reg(u8) Reg(u8)
    MUL, // MUL Reg(u8) Reg(u8) Reg(u8)
    MULF, // MULF Reg(u8) Reg(u8) Reg(u8)
    DIV, // DIV Reg(u8) Reg(u8) Reg(u8)
    DIVF, // DIVF Reg(u8) Reg(u8) Reg(u8)
    AND, // AND Reg(u8) Reg(u8) Reg(u8)
    OR, // OR Reg(u8) Reg(u8) Reg(u8)
    XOR, // XOR Reg(u8) Reg(u8) Reg(u8)
    CMP, // CMP Reg(u8) Reg(u8)
    CMPF, // CMP Reg(u8) Reg(u8)
    JMP, // JMP Address(u16)
    JE, // JE Address(u16)
    JNE, // JNE Address(u16)
    JG, // JG Address(u16)
    JGE, // JGE Address(u16)
    JL, // JL Address(u16)
    JLE, // JLE Address(u16)
    PUSH, // PUSH Reg(u8)
    POP, // POP Reg(u8)
    SYSCALL, // SYSCALL Id(u16)
  }

  public enum PreOp
  {
    INIT_CONST,
    INIT_CODE,
  }
}
