using System.Text;
using System.IO.Hashing;

namespace EDUtils;

public static class Activity
{
    public static string PlayerActivityHash(string playerId, DateTimeOffset activity)
    {
        var input = playerId + activity.ToString("yyyyMMddHH");
        var hash = XxHash64.Hash(Encoding.UTF8.GetBytes(input));
        var result = BitConverter.ToString(hash);
        return result;
    }
}