using System.Security.Cryptography;
using BcgHub.Api.Application;

namespace BcgHub.Api.Infrastructure;

public sealed class UserPasswordGenerator : IUserPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";

    public string CreatePassword()
    {
        var chars = new char[18];
        for (var i = 0; i < chars.Length; i++) chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars);
    }
}
