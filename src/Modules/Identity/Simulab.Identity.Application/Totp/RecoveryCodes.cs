using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Security;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Totp;

/// <summary>
/// The ten single-use recovery codes (F-11 BR5-BR7). ASP.NET Identity's own recovery codes are stored in plain
/// text, so the module writes its own row in <c>user_tokens</c> holding only a keyed SHA-256 of each code (v3).
/// </summary>
public sealed class RecoveryCodes(UserManager<User> userManager, ITotpSecretProtector protector)
{
    public const int Count = 10;

    /// <summary>The <c>user_tokens</c> provider and name of the row; the erasure removes it with every other token (BR13).</summary>
    public const string TokenProvider = "[Simulab]";
    public const string TokenName = "TotpRecoveryCodes";

    // No 0/O or 1/I/L: a code is read off paper and typed back.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int HalfLength = 5;

    /// <summary>Ten new codes for the user, replacing any earlier ones at once (BR7). Returns the plain codes, shown once.</summary>
    public async Task<IReadOnlyList<string>> ReplaceAsync(User user)
    {
        var codes = Enumerable.Range(0, Count).Select(_ => NewCode()).ToList();
        // F-47 BR1b: codes that were never stored must not be shown as if they were.
        (await userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenName, string.Join(';', codes.Select(code => Hash(user, code)))))
            .ThrowIfFailed("Storing the recovery codes");
        return codes;
    }

    /// <summary>How many codes are still unused.</summary>
    public async Task<int> CountAsync(User user) =>
        HashesOf(await userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenName)).Count;

    /// <summary>BR6: true when <paramref name="code"/> is one of the user's unused codes, which is then spent.</summary>
    public async Task<bool> RedeemAsync(User user, string code)
    {
        if (Normalize(code).Length != HalfLength * 2)
        {
            return false;
        }

        var hashes = HashesOf(await userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenName));
        if (!hashes.Remove(Hash(user, code)))
        {
            return false;
        }

        await userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenName, string.Join(';', hashes));
        return true;
    }

    /// <summary>BR8: turning two-factor off leaves no code behind.</summary>
    public Task RemoveAsync(User user) => userManager.RemoveAuthenticationTokenAsync(user, TokenProvider, TokenName);

    /// <summary>What is hashed: the code without the dash or spaces, in capitals, so <c>abcde-fghjk</c> and <c>ABCDEFGHJK</c> match.</summary>
    public static string Normalize(string? code) =>
        new((code ?? string.Empty).Where(character => character is not ('-' or ' ')).Select(char.ToUpperInvariant).ToArray());

    /// <summary>
    /// Keyed by the account and by the encryption key: a code has about 50 bits, few enough to guess offline
    /// from a plain hash, and the key never sits in the database next to it.
    /// </summary>
    private string Hash(User user, string code) => protector.Hash($"{user.Id:N}:{Normalize(code)}");

    private static List<string> HashesOf(string? stored) =>
        string.IsNullOrEmpty(stored) ? [] : [.. stored.Split(';', StringSplitOptions.RemoveEmptyEntries)];

    private static string NewCode()
    {
        var characters = new char[HalfLength * 2];
        for (var i = 0; i < characters.Length; i++)
        {
            characters[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return $"{new string(characters, 0, HalfLength)}-{new string(characters, HalfLength, HalfLength)}";
    }
}
