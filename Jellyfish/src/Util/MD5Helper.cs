using System.Security.Cryptography;
using System.Text;

namespace Jellyfish.Util;

/// <summary>
///     Helper class to covert string to MD5 hash text
/// </summary>
public static class Md5Helper
{
    /// <summary>
    ///     Covert string to MD5 text
    /// </summary>
    /// <param name="content">String content</param>
    /// <returns>MD5 text</returns>
    public static string ToMd5Hash(this string content)
    {
        return BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
