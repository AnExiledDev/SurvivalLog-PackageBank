using PackageBank.Core;
using Xunit;

namespace PackageBank.Tests;

/// <summary>What another mod sees of the bank and which package it gets when it asks for one.</summary>
public class BankShelfTests
{
    private static readonly BankEntry[] Shelf =
    {
        new(5, 30, 0, -1, 0, IsPurchased: false),
        new(2, 41, 0, -1, 0, IsPurchased: false),
        new(1, 30, 3, 450, 7, IsPurchased: true),
    };

    [Fact]
    public void Every_banked_package_is_listed_once_oldest_first()
    {
        Assert.Equal(new[] { 30, 41, 30 }, BankShelf.Furniture(Shelf));
    }

    [Fact]
    public void Asking_for_a_furniture_takes_the_longest_banked_one()
    {
        Assert.Equal(1, BankShelf.Oldest(Shelf, 30)?.Slot);
    }

    [Fact]
    public void Asking_for_a_furniture_that_is_not_banked_finds_nothing()
    {
        Assert.Null(BankShelf.Oldest(Shelf, 99));
    }
}
