namespace Rabbit.Core
{
  public interface IUnit<T>
  {
    void Init(IBaseVM<T> vm);
  }
}
