namespace Rabbit.Core
{
  public struct Result<T>
  {
    public T Ok;
    public string Err;

    public Result(T ok, string err)
    {
      Ok = ok;
      Err = err;
    }

    public static implicit operator (T, string)(Result<T> option)
    {
      return (option.Ok, option.Err);
    }

    public static implicit operator Result<T>((T, string) x)
    {
      return new Result<T>(x.Item1, x.Item2);
    }

    public bool IsErr() => !string.IsNullOrEmpty(Err);
    public bool IsOk() => string.IsNullOrEmpty(Err);

    public T Unwrap()
    {
      if (IsErr()) throw new ResultException(Err);
      return Ok;
    }

    public T Expect(string msg)
    {
      if (IsErr()) throw new ResultException($"{Err}\n{msg}");
      return Ok;
    }
  } // struct
} // namespace
