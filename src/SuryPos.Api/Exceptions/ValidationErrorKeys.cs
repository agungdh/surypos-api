namespace SuryPos.Api.Exceptions;

// Key error validasi harus cocok dengan nama field JSON (camelCase) supaya
// FE bisa binding langsung, mis. errors['items[0].quantity'].
// FluentValidation & ModelState memakai nama properti C# (PascalCase),
// JSON path error memakai prefix '$.' -> semuanya dinormalisasi di sini.
public static class ValidationErrorKeys
{
    public static string Normalize(string key)
    {
        if (key.StartsWith("$.", StringComparison.Ordinal))
            key = key[2..];

        var chars = key.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 0 || chars[i - 1] is '.' or '[')
                chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }
}
