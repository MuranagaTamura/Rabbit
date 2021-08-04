* メモリ配置は以下のように想定する

  * 定数領域、コード領域、メモリ領域、レジスタ領域に分ける
  * 整数は16bit固定（コード領域以外のサイズはすべて16bitにする）
  * ○○領域（最大サイズの絶対値）

  | 定数領域(u16.MinValu) | コード領域(u16.MaxValue) | メモリ領域(u16.MaxValue) | レジスタ領域（16） |
  | --------------------- | ------------------------ | ------------------------ | ------------------ |
  | 定数                  | コード領域               | メモリ（ヒープ）         | レジスタ           |
  | ...                   | ...                      | ...                      | ...                |
  | ...                   | ...                      | メモリ（スタック）       | ...                |
  
* VMの実装は別に任せる

* RegisterFuncで登録していない関数は実行されない

  * 関数の例

    ```c#
    // 関数形式はFunc<byte[], Result<bool>>
    Result<bool> ADDI(byte[] code)
    {
      // 現在の実行位置を取得
      ushort ip = GetReg(RegId.Ip);
      // dstレジスタを取得
      Result<ushort> dstReg = ReadUint16(code, ref ip);
      if(dstReg.IsErr())
        return (false, ＄"dstレジスタの参照に失敗しました: {dstReg.Err}");
      // srcレジスタに値を取得
      Result<ushort> val0 = GetReg(ReadUint16(code, ref ip));
      if(val0.IsErr())
        return (false, ＄"srcレジスタの参照に失敗しました: {val0.Err}");
      // 即値を取得
      Result<ushort> val1 = ReadUint16(code, ref ip);
      if(val1.IsErr())
        return (false, ＄"imm値の参照に失敗しました: {val1.Err}");
      
      // 計算して指定したレジスタに値を設定
      uint addVal = val0 + val1; // できたらここの部分以外を共通化したい
      SetReg(dstReg, reg);
      
      // 実行位置を変更
      SetReg(RegId.Ip, ip);
    
      return (true, "");
    }
    ```

* 定数のアクセスはLOAD_CONSTを用意

  * レジスタのみアクセス可能
  * メモリに格納する場合はMOVE命令を使う
  
* OPCODEの追加時の順番

  * Opcode列挙体に求めているものを追加（書式をコメントで書く）
  * (TestRabbitVmでテストを作る（BytecodeGeneratorに実装があるかのように書く）)
  * BytecodeGeneratorに実装していないメソッドを実装する
  * RabbitVMのRegisterRunFuncAllでRunFuncを登録する
  * 登録対象のRunFuncをRabbitVMに実装する
  * DisassemblerのRegisterCodeFuncAllでCodeFuncを登録する
  * 登録対象のCodeFuncをDisassemblerに実装する

* Syscallの呼び出し規則

  * 基本的に通常のCallを呼ぶのと一緒

    ```assembly
    	LOADI A0, 0x00
    	PUSH A0 ; 引数を後ろから入れる、今回は0x00が入る
    	SYSCALL 0x00 ; Syscallを実行
    ```

    ```csharp
    private IEnumerator<SyscallReturn> Test(SyscallInfo info)
    {
        // sp, fpの退避
        info.Initiate();
        
        // 0番目の引数を取得
        Result<string> str = info.ReadArgString(0);
        if (str.IsErr())
            testText = str.Err;
        else
            testText = str.Ok;
    
        // 返り値を設定
        info.SetReturnUnit16(0x1234);
        
        // sp, fpの復元
        info.Terminate();
        yield return SyscallReturn.END;
    }
    ```
    
  * **戻り値はA0に格納する**
  
* Callの例

  ```assembly
  Func:
  	PUSH Fp ; 現在のFPを保存
  	MOVE Fp, Sp ; FPを現在のSPに変更
  	; 0番目の引数の位置を計算
  	SUB A1, Fp, 1 ; Fpには戻り値
  	; 何かしらの処理
  	LOADI A0, 0x1234 ; 返り値を設定
  	MOVE Sp, Fp ; SPを現在のFPに変更
  	POP Fp ; トップの値をFPに格納
  	RET
  MAIN:
  	LOADI A0, 0x00
  	PUSH A0 ; 引数を後ろから入れる、今回は0x00が入る
  	CALL Func ; 関数を実行
  ```

  

