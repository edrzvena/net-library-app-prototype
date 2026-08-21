namespace LibraryAppPrototype.Services;

// BR-13: ISBN harus valid ISBN-10 atau ISBN-13 (checksum diverifikasi) dan disimpan
// ternormalisasi menjadi 13 digit tanpa '-' / spasi, supaya unique index tidak bocor
// (mis. "0-306-40615-2" dan "0306406152" tidak boleh dianggap dua buku berbeda).
public static class IsbnHelper
{
    /// <summary>
    /// Membersihkan input, memverifikasi checksum, lalu mengembalikan bentuk ISBN-13 (13 digit).
    /// ISBN-10 yang valid dikonversi ke ISBN-13 dengan prefix 978 + check digit dihitung ulang.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Buang '-' dan spasi. 'X' hanya sah sebagai check digit ISBN-10.
        var raw = new string(input.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();

        return raw.Length switch
        {
            10 => TryNormalizeIsbn10(raw, out normalized),
            13 => TryNormalizeIsbn13(raw, out normalized),
            _ => false
        };
    }

    // ISBN-10: sum(digit[i] * (10 - i)) mod 11 == 0, digit terakhir boleh 'X' (= 10).
    private static bool TryNormalizeIsbn10(string raw, out string normalized)
    {
        normalized = string.Empty;

        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            int value;
            if (raw[i] is >= '0' and <= '9') value = raw[i] - '0';
            else if (i == 9 && raw[i] == 'X') value = 10;
            else return false;

            sum += value * (10 - i);
        }

        if (sum % 11 != 0) return false;

        // Konversi ke ISBN-13: 978 + 9 digit pertama + check digit ISBN-13 yang baru.
        var body = "978" + raw[..9];
        normalized = body + ComputeIsbn13CheckDigit(body);
        return true;
    }

    // ISBN-13: sum(digit[i] * (i genap ? 1 : 3)) mod 10 == 0.
    private static bool TryNormalizeIsbn13(string raw, out string normalized)
    {
        normalized = string.Empty;
        if (!raw.All(char.IsAsciiDigit)) return false;

        if (ComputeIsbn13CheckDigit(raw[..12]) != raw[12]) return false;

        normalized = raw;
        return true;
    }

    private static char ComputeIsbn13CheckDigit(string first12)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (first12[i] - '0') * (i % 2 == 0 ? 1 : 3);

        return (char)('0' + (10 - sum % 10) % 10);
    }
}
