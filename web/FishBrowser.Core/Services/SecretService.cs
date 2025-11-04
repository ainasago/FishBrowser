using System;
using System.Security.Cryptography;
using System.Text;

namespace FishBrowser.WPF.Services;

public class SecretService
{
    // Windows DPAPI for encrypting small secrets (e.g., proxy passwords)
    public string? Encrypt(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return plain;
        var bytes = Encoding.UTF8.GetBytes(plain);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public string? Decrypt(string? encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return encrypted;
        try
        {
            var protectedBytes = Convert.FromBase64String(encrypted);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}
