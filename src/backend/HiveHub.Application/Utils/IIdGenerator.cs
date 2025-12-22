namespace HiveHub.Application.Utils;

public interface IIdGenerator
{
    Guid GenereteGuid();
    string GenerateId(int length = 8);
}

public class IdGenerator : IIdGenerator
{
    public Guid GenereteGuid()
    {
        return Guid.NewGuid();
    }
    public string GenerateId(int length = 8)
    {
        return Guid.NewGuid().ToString("N").Substring(0, length);
    }
}
