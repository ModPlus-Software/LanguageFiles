namespace ModPlus.Languages.Tests;

using FluentAssertions;

public class LanguageStorageTests
{
    [Fact]
    public void GetDocument_CorrectXPath_Success()
    {
        LanguageStorage.GetLanguageDocument("ModPlus/Common").Should().NotBeNull();
    }

    [Fact]
    public void GetDocument_IncorrectXPath_Success()
    {
        LanguageStorage.GetLanguageDocument("ModPlus/Common1").Should().NotBeNull();
    }

    [Fact]
    public void GetItem_CorrectKey_Success()
    {
        LanguageStorage.GetItem("Common", "accept").Should().Be("Принять");
    }

    [Fact]
    public void GetItem_IncorrectKey_Success()
    {
        LanguageStorage.GetItem("Common", "accept1").Should().Be("Localization error");
    }

    [Fact]
    public void GetAttributeValue_CorrectAttributeName_Success()
    {
        LanguageStorage.GetAttributeValue("AutocadDlls", "LocalName").Should().Be("Рабочие библиотеки для AutoCAD");
    }

    [Fact]
    public void GetAttributeValue_IncorrectAttributeName_Success()
    {
        LanguageStorage.GetAttributeValue("AutocadDlls", "LocalName1").Should().Be("Localization error");
    }
}