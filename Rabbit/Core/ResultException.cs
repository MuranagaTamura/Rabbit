using System;
using System.Runtime.Serialization;

namespace Rabbit.Core
{
  [Serializable()]
  public class ResultException : Exception
  {
    public ResultException() : base() { }
    public ResultException(string msg) : base(msg) { }
    public ResultException(string msg, Exception innner) : base(msg, innner) { }
    protected ResultException(SerializationInfo info, StreamingContext context)
      : base(info, context) { }
  }
}
