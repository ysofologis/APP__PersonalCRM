using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace PersonalCrm.Infrastructure.Security;

/// <summary>
/// Argon2id password hasher. Encodes parameters + salt + hash into a single
/// PHC-format string so we never need a side-table of parameters per user.
///
/// Format:
///   <c>$argon2id$v=19$m=&lt;memKiB&gt;,t=&lt;iter&gt;,p=&lt;par&gt;$&lt;saltB64&gt;$&lt;hashB64&gt;</c>
///
/// Parameters: memory=64 MiB, iterations=3, parallelism=4. Tuned for a server
/// doing one hash at a time on commodity hardware (~250 ms per verify).
/// </summary>
public sealed class PasswordHasher
{
    // Recommended minimum for argon2id (OWASP Password Storage Cheat Sheet, 2024).
    private const int MemoryKiB  = 64 * 1024;   // 64 MiB
    private const int Iterations = 3;
    private const int Parallelism = 4;
    private const int SaltBytes  = 16;          // 128 bits
    private const int HashBytes  = 32;          // 256 bits

    /// <summary>Hashes a UTF-8 password into a PHC-format string.</summary>
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);

        var hash = ComputeArgon2id(password, salt, MemoryKiB, Iterations, Parallelism, HashBytes);

        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "$argon2id$v=19$m={0},t={1},p={2}${3}${4}",
            MemoryKiB, Iterations, Parallelism,
            Base64UrlEncode(salt),
            Base64UrlEncode(hash));
    }

    /// <summary>
    /// Verifies a password against a previously-hashed value. Constant-time
    /// comparison avoids timing leaks. Returns false on any parse error
    /// (treats malformed hashes as no-match, never as "throw").
    /// </summary>
    public bool Verify(string password, string encoded)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentException.ThrowIfNullOrEmpty(encoded);

        if (!TryParseEncoded(encoded, out var memKiB, out var iters, out var par,
                             out var salt, out var expectedHash))
        {
            return false;
        }

        var actualHash = ComputeArgon2id(password, salt, memKiB, iters, par, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    // ----- internals --------------------------------------------------------

    private static byte[] ComputeArgon2id(
        string password, byte[] salt, int memoryKiB, int iterations, int parallelism, int hashBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt           = salt,
            DegreeOfParallelism = parallelism,
            MemorySize     = memoryKiB,
            Iterations     = iterations
        };

        return argon2.GetBytes(hashBytes);
    }

    private static bool TryParseEncoded(
        string encoded,
        out int memoryKiB, out int iterations, out int parallelism,
        out byte[] salt, out byte[] hash)
    {
        memoryKiB = iterations = parallelism = 0;
        salt = hash = Array.Empty<byte>();

        var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5) return false;
        if (parts[0] != "argon2id") return false;
        if (parts[1] != "v=19")     return false;

        var paramSegments = parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (paramSegments.Length != 3) return false;

        foreach (var segment in paramSegments)
        {
            var kv = segment.Split('=', 2);
            if (kv.Length != 2) return false;
            if (!int.TryParse(kv[1], System.Globalization.NumberStyles.Integer,
                              System.Globalization.CultureInfo.InvariantCulture, out var v))
            {
                return false;
            }
            switch (kv[0])
            {
                case "m": memoryKiB   = v; break;
                case "t": iterations  = v; break;
                case "p": parallelism = v; break;
                default:  return false;
            }
        }

        try
        {
            salt = Base64UrlDecode(parts[3]);
            hash = Base64UrlDecode(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }
        return true;
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "=";  break;
        }
        return Convert.FromBase64String(padded);
    }
}
