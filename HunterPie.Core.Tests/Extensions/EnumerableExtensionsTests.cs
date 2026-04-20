using global::System.Collections;
using HunterPie.Core.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HunterPie.Core.Tests.Extensions;

[TestFixture]
public class EnumerableExtensionsTests
{
    #region PrependNotNull

    [TestCase(1, new[] { 2, 3, 4 }, ExpectedResult = new[] { 1, 2, 3, 4 })]
    [TestCase(42, new int[0], ExpectedResult = new[] { 42 })]
    public int[] PrependNotNull_WhenValueIsNotNull_PrependsValue(int value, int[] source)
    {
        return source.AsEnumerable().PrependNotNull(value).ToArray();
    }

    [Test]
    public void PrependNotNull_WhenValueIsNull_ReturnsSameEnumerable()
    {
        IEnumerable<string> source = new[] { "a", "b", "c" };

        IEnumerable<string> result = source.PrependNotNull(null);

        Assert.That(result, Is.EquivalentTo(source));
    }

    [Test]
    public void PrependNotNull_WhenSourceIsEmpty_AndValueIsNull_ReturnsEmpty()
    {
        IEnumerable<string> source = Enumerable.Empty<string>();

        IEnumerable<string> result = source.PrependNotNull(null);

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region DisposeAll

    [TestCase(1)]
    [TestCase(3)]
    public void DisposeAll_Array_DisposesAllElements(int count)
    {
        TrackingDisposable[] array = Enumerable.Range(0, count).Select(_ => new TrackingDisposable()).ToArray();

        array.DisposeAll();

        Assert.That(array.All(d => d.IsDisposed), Is.True);
    }

    [Test]
    public void DisposeAll_Array_WhenEmpty_DoesNotThrow()
    {
        Assert.That(() => Array.Empty<TrackingDisposable>().DisposeAll(), Throws.Nothing);
    }

    [TestCase(1)]
    [TestCase(2)]
    public void DisposeAll_IEnumerable_DisposesAllElements(int count)
    {
        List<TrackingDisposable> source = Enumerable.Range(0, count).Select(_ => new TrackingDisposable()).ToList();

        ((IEnumerable<TrackingDisposable>)source).DisposeAll();

        Assert.That(source.All(d => d.IsDisposed), Is.True);
    }

    [Test]
    public void DisposeAll_IEnumerable_WhenEmpty_DoesNotThrow()
    {
        Assert.That(() => Enumerable.Empty<TrackingDisposable>().DisposeAll(), Throws.Nothing);
    }

    #endregion

    #region TryCast

    [TestCaseSource(nameof(TryCastCases))]
    public void TryCast_ReturnsExpectedCount(object[] source, int expectedCount)
    {
        IEnumerable<int> result = ((IEnumerable)source).TryCast<int>();

        Assert.That(result.Count(), Is.EqualTo(expectedCount));
    }

    private static IEnumerable<TestCaseData> TryCastCases()
    {
        yield return new TestCaseData(new object[] { 1, 2, 3 }, 3).SetDescription("TryCast_WhenAllElementsMatch_ReturnsAllCasted");
        yield return new TestCaseData(new object[] { 1, "two", 3, "four" }, 2).SetDescription("TryCast_WhenSomeElementsDontMatch_ReturnsOnlyMatching");
        yield return new TestCaseData(new object[] { "a", "b", "c" }, 0).SetDescription("TryCast_WhenNoElementsMatch_ReturnsEmpty");
        yield return new TestCaseData(Array.Empty<object>(), 0).SetDescription("TryCast_WhenSourceIsEmpty_ReturnsEmpty");
    }

    #endregion

    #region SingleOrNull

    [TestCase(new[] { 42 }, ExpectedResult = 42)]
    [TestCase(new int[0], ExpectedResult = 0)]
    [TestCase(new[] { 1, 2 }, ExpectedResult = 0)]
    public int? SingleOrNull_Int_ReturnsExpected(int[] source)
    {
        return source.AsEnumerable().SingleOrNull();
    }

    [TestCaseSource(nameof(SingleOrNullStringCases))]
    public void SingleOrNull_String_ReturnsExpected(string[] source, string? expected)
    {
        string? result = source.AsEnumerable().SingleOrNull();

        Assert.That(result, Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> SingleOrNullStringCases()
    {
        yield return new TestCaseData(new[] { "hello" }, "hello").SetDescription("SingleOrNull_WhenExactlyOneString_ReturnsThatString");
        yield return new TestCaseData(new[] { "a", "b" }, null).SetDescription("SingleOrNull_WhenMultipleStrings_ReturnsNull");
    }

    #endregion

    #region ToObservableCollection

    [Test]
    public void ToObservableCollection_ReturnsObservableCollectionWithSameElements()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };

        ObservableCollection<int> result = source.ToObservableCollection();

        Assert.That(result, Is.EquivalentTo(source));
        Assert.That(result, Is.InstanceOf<ObservableCollection<int>>());
    }

    [Test]
    public void ToObservableCollection_WhenEmpty_ReturnsEmptyObservableCollection()
    {
        ObservableCollection<int> result = Enumerable.Empty<int>().ToObservableCollection();

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region FilterNull

    [TestCaseSource(nameof(FilterNullCases))]
    public void FilterNull_ReturnsExpectedCount(string?[] source, int expectedCount)
    {
        IEnumerable<string> result = source.AsEnumerable().FilterNull();

        Assert.That(result.Count(), Is.EqualTo(expectedCount));
    }

    private static IEnumerable<TestCaseData> FilterNullCases()
    {
        yield return new TestCaseData(new[] { "a", "b", "c" }, 3).SetDescription("FilterNull_WhenAllElementsAreNonNull_ReturnsAll");
        yield return new TestCaseData(new[] { "a", null, "c", null }, 2).SetDescription("FilterNull_WhenSomeElementsAreNull_FiltersThemOut");
        yield return new TestCaseData(new string?[] { null, null, null }, 0).SetDescription("FilterNull_WhenAllElementsAreNull_ReturnsEmpty");
        yield return new TestCaseData(Array.Empty<string?>(), 0).SetDescription("FilterNull_WhenSourceIsEmpty_ReturnsEmpty");
    }

    #endregion

    #region ForEach

    [TestCase(new[] { 1, 2, 3 }, ExpectedResult = 3)]
    [TestCase(new int[0], ExpectedResult = 0)]
    [TestCase(new[] { 1, 2, 3, 4, 5 }, ExpectedResult = 5)]
    public int ForEach_ExecutesActionExpectedNumberOfTimes(int[] source)
    {
        int callCount = 0;

        source.AsEnumerable().ForEach(_ => callCount++);

        return callCount;
    }

    [Test]
    public void ForEach_ExecutesActionOnEachElement()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };
        var visited = new List<int>();

        source.ForEach(x => visited.Add(x));

        Assert.That(visited, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    #endregion

    #region TakeRolling

    [TestCaseSource(nameof(TakeRollingCases))]
    public void TakeRolling_ReturnsExpectedSlice(int[] array, int skip, int take, int[] expected)
    {
        IEnumerable<int> result = array.TakeRolling(skip: skip, take: take);

        Assert.That(result, Is.EquivalentTo(expected));
    }

    private static IEnumerable<TestCaseData> TakeRollingCases()
    {
        yield return new TestCaseData(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, 3, new[] { 2, 3, 4 })
            .SetDescription("TakeRolling_WhenRangeIsWithinBounds_ReturnsThatSlice");
        yield return new TestCaseData(new[] { 0, 1, 2, 3, 4 }, 3, 4, new[] { 0, 1, 3, 4 })
            .SetDescription("TakeRolling_WhenRangeWrapsAround_WrapsFromStart");
        yield return new TestCaseData(new[] { 10, 20, 30, 40, 50 }, 0, 3, new[] { 10, 20, 30 })
            .SetDescription("TakeRolling_WhenSkipIsZero_StartsFromBeginning");
    }

    #endregion

    #region Count (non-generic IEnumerable)

    [Test]
    public void Count_WhenICollection_ReturnsCorrectCount()
    {
        IEnumerable source = new List<int> { 1, 2, 3 };

        Assert.That(source.Count(), Is.EqualTo(3));
    }

    [Test]
    public void Count_WhenNotICollection_ReturnsCorrectCount()
    {
        IEnumerable source = Iterate(1, 2, 3);

        Assert.That(source.Count(), Is.EqualTo(3));
    }

    [Test]
    public void Count_WhenEmpty_ReturnsZero()
    {
        IEnumerable source = Array.Empty<int>();

        Assert.That(source.Count(), Is.EqualTo(0));
    }

    private static IEnumerable Iterate(params object[] values)
    {
        foreach (object v in values)
            yield return v;
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Minimal IDisposable implementation that tracks whether
    /// Dispose() was called — used by DisposeAll tests.
    /// </summary>
    private class TrackingDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    #endregion
}