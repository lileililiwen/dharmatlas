using Dharmatlas.Domain;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public sealed class NameNormalizerTests
{
    [Theory]
    [InlineData("Nalanda", "nalanda")]
    [InlineData("Nālandā", "nalanda")]
    [InlineData("  Nalanda  ", "nalanda")]
    [InlineData("Xuánzàng", "xuanzang")]
    [InlineData("Ｘｕａｎｚａｎｇ", "xuanzang")]
    [InlineData("Asaṅga", "asanga")]
    [InlineData("Wŏnhyo", "wonhyo")]
    [InlineData("Shōtoku", "shotoku")]
    [InlineData("Ryūju", "ryuju")]
    [InlineData("Genjō", "genjo")]
    [InlineData("Kumārajīva", "kumarajiva")]
    [InlineData("Straße", "strasse")]
    [InlineData("Æsop", "aesop")]
    [InlineData("玄奘", "玄奘")]
    [InlineData("那爛陀", "那爛陀")]
    [InlineData("현장", "현장")]
    [InlineData("しょうとくたいし", "しょうとくたいし")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Normalize_vectors(string raw, string expected)
    {
        Assert.Equal(expected, NameNormalizer.Normalize(raw));
    }

    [Fact]
    public void Normalize_null_yields_empty()
    {
        Assert.Equal(string.Empty, NameNormalizer.Normalize(null));
    }

    [Fact]
    public void Normalize_preserves_kana_voicing()
    {
        Assert.NotEqual(NameNormalizer.Normalize("か"), NameNormalizer.Normalize("が"));
        Assert.NotEqual(NameNormalizer.Normalize("は"), NameNormalizer.Normalize("ば"));
    }

    [Theory]
    [InlineData("Hsüan-tsang", "Xuanzang")]
    [InlineData("hsuantsang", "xuanzang")]
    [InlineData("Fa-hsien", "Faxian")]
    [InlineData("fahsien", "faxian")]
    [InlineData("Sakya Thub pa", "SakyaThubpa")]
    [InlineData("Jo khang", "Jokhang")]
    [InlineData("Xuánzàng", "xuanzang")]
    public void Transliteration_keys_match_across_schemes(string variant, string canonical)
    {
        Assert.Equal(NameNormalizer.SearchKey(canonical), NameNormalizer.SearchKey(variant));
    }

    [Fact]
    public void Transliteration_key_differs_from_base_normalization_for_variant_pairs()
    {
        // The variant must NOT collapse to exact: ranking stays transparent.
        Assert.NotEqual(NameNormalizer.Normalize("Hsüan-tsang"), NameNormalizer.Normalize("Xuanzang"));
    }

    [Theory]
    [InlineData("那爛陀", new[] { "那爛", "爛陀" })]
    [InlineData("玄奘", new[] { "玄奘" })]
    [InlineData("玄", new[] { "玄" })]
    public void Cjk_bigrams_cover_han_runs(string raw, string[] expected)
    {
        Assert.Equal(expected, NameNormalizer.CjkBigrams(raw));
    }

    [Fact]
    public void Cjk_bigrams_ignore_latin()
    {
        Assert.Empty(NameNormalizer.CjkBigrams("Xuanzang"));
        Assert.Empty(NameNormalizer.CjkBigrams(null));
    }

    [Theory]
    [InlineData('玄', true)]
    [InlineData('원', true)]
    [InlineData('あ', true)]
    [InlineData('ア', true)]
    [InlineData('འ', true)]
    [InlineData('a', false)]
    [InlineData('ü', false)]
    public void IsCjk_vectors(char ch, bool expected)
    {
        Assert.Equal(expected, NameNormalizer.IsCjk(ch));
    }

    [Theory]
    [InlineData("nalanda", "nalanda", 0)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("xuanzang", "xuanzagn", 2)]
    [InlineData("ashoka", "ashkoa", 2)]
    [InlineData("nalanda", "nālandā-normalized", 11)]
    public void Levenshtein_vectors(string a, string b, int expected)
    {
        var left = NameNormalizer.Normalize(a);
        var right = NameNormalizer.Normalize(b);
        Assert.Equal(expected, NameNormalizer.LevenshteinDistance(left, right));
    }
}
