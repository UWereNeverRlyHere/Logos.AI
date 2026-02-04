using System.Security.Cryptography;
using System.Text;
namespace Logos.AI.Abstractions.Common;

public static class GuidUtils
{
	public static  Guid GenerateGuidFromSeed(string input)
	{
		using var md5 = MD5.Create();
		return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
	}
	public static Guid GenerateGuidFromSeed(byte[] input)
	{
		using var md5 = MD5.Create();
		return new Guid(md5.ComputeHash(input));
	}
}
