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

    [Test]
    public void PrependNotNull_WhenValueIsNotNull_PrependsValue()
    {
        IEnumerable<int> source = new[] { 2, 3, 4 };

        IEnumerable<int> result = source.PrependNotNull(1);

        Assert.That(result.First(), Is.EqualTo(1));
    }

    [Test]
    public void PrependNotNull_WhenValueIsNotNull_ResultHasOneMoreElement()
    {
        IEnumerable<int> source = new[] { 2, 3, 4 };

        IEnumerable<int> result = source.PrependNotNull(1);

        Assert.That(result.Count(), Is.EqualTo(4));
    }

    [Test]
    public void PrependNotNull_WhenValueIsNull_ReturnsSameEnumerable()
    {
        IEnumerable<string> source = new[] { "a", "b", "c" };

        IEnumerable<string> result = source.PrependNotNull(null);

        Assert.That(result, Is.EquivalentTo(source));
    }

    [Test]
    public void PrependNotNull_WhenSourceIsEmpty_AndValueIsNotNull_ReturnsSingleElement()
    {
        IEnumerable<int> source = Enumerable.Empty<int>();

        IEnumerable<int> result = source.PrependNotNull(42);

        Assert.That(result.Single(), Is.EqualTo(42));
    }

    [Test]
    public void PrependNotNull_WhenSourceIsEmpty_AndValueIsNull_ReturnsEmpty()
    {
        IEnumerable<string> source = Enumerable.Empty<string>();

        IEnumerable<string> result = source.PrependNotNull(null);

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region DisposeAll (array)

    [Test]
    public void DisposeAll_Array_DisposesAllElements()
    {
        var disposable1 = new TrackingDisposable();
        var disposable2 = new TrackingDisposable();
        var disposable3 = new TrackingDisposable();
        TrackingDisposable[] array = { disposable1, disposable2, disposable3 };

        array.DisposeAll();

        Assert.That(disposable1.IsDisposed, Is.True);
        Assert.That(disposable2.IsDisposed, Is.True);
        Assert.That(disposable3.IsDisposed, Is.True);
    }

    [Test]
    public void DisposeAll_Array_WhenEmpty_DoesNotThrow()
    {
        TrackingDisposable[] array = Array.Empty<TrackingDisposable>();

        Assert.That(() => array.DisposeAll(), Throws.Nothing);
    }

    [Test]
    public void DisposeAll_Array_WhenSingleElement_DisposesIt()
    {
        var disposable = new TrackingDisposable();
        TrackingDisposable[] array = { disposable };

        array.DisposeAll();

        Assert.That(disposable.IsDisposed, Is.True);
    }

    #endregion

    #region DisposeAll (IEnumerable)

    [Test]
    public void DisposeAll_IEnumerable_DisposesAllElements()
    {
        var disposable1 = new TrackingDisposable();
        var disposable2 = new TrackingDisposable();
        IEnumerable<TrackingDisposable> source = new List<TrackingDisposable> { disposable1, disposable2 };

        source.DisposeAll();

        Assert.That(disposable1.IsDisposed, Is.True);
        Assert.That(disposable2.IsDisposed, Is.True);
    }

    [Test]
    public void DisposeAll_IEnumerable_WhenEmpty_DoesNotThrow()
    {
        IEnumerable<TrackingDisposable> source = Enumerable.Empty<TrackingDisposable>();

        Assert.That(() => source.DisposeAll(), Throws.Nothing);
    }

    #endregion

    #region TryCast

    [Test]
    public void TryCast_WhenAllElementsMatch_ReturnsAllCasted()
    {
        IEnumerable source = new object[] { 1, 2, 3 };

        IEnumerable<int> result = source.TryCast<int>();

        Assert.That(result.Count(), Is.EqualTo(3));
    }

    [Test]
    public void TryCast_WhenSomeElementsDontMatch_ReturnsOnlyMatching()
    {
        IEnumerable source = new object[] { 1, "two", 3, "four" };

        IEnumerable<int> result = source.TryCast<int>();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public void TryCast_WhenNoElementsMatch_ReturnsEmpty()
    {
        IEnumerable source = new object[] { "a", "b", "c" };

        IEnumerable<int> result = source.TryCast<int>();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void TryCast_WhenSourceIsEmpty_ReturnsEmpty()
    {
        IEnumerable source = Array.Empty<object>();

        IEnumerable<int> result = source.TryCast<int>();

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region SingleOrNull

    [Test]
    public void SingleOrNull_WhenExactlyOneElement_ReturnsThatElement()
    {
        IEnumerable<int> source = new[] { 42 };

        int? result = source.SingleOrNull();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void SingleOrNull_WhenEmpty_ReturnsDefault()
    {
        IEnumerable<int> source = Enumerable.Empty<int>();

        int? result = source.SingleOrNull();

        Assert.That(result, Is.EqualTo(default(int)));
    }

    [Test]
    public void SingleOrNull_WhenMoreThanOneElement_ReturnsDefault()
    {
        IEnumerable<int> source = new[] { 1, 2 };

        int? result = source.SingleOrNull();

        Assert.That(result, Is.EqualTo(default(int)));
    }

    [Test]
    public void SingleOrNull_WhenExactlyOneString_ReturnsThatString()
    {
        IEnumerable<string> source = new[] { "hello" };

        string? result = source.SingleOrNull();

        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void SingleOrNull_WhenMultipleStrings_ReturnsNull()
    {
        IEnumerable<string> source = new[] { "a", "b" };

        string? result = source.SingleOrNull();

        Assert.That(result, Is.Null);
    }

    #endregion

    #region ToObservableCollection

    [Test]
    public void ToObservableCollection_ReturnsObservableCollectionWithSameElements()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };

        ObservableCollection<int> result = source.ToObservableCollection();

        Assert.That(result, Is.EquivalentTo(source));
    }

    [Test]
    public void ToObservableCollection_WhenEmpty_ReturnsEmptyObservableCollection()
    {
        IEnumerable<int> source = Enumerable.Empty<int>();

        ObservableCollection<int> result = source.ToObservableCollection();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ToObservableCollection_ReturnTypeIsObservableCollection()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };

        object result = source.ToObservableCollection();

        Assert.That(result, Is.InstanceOf<ObservableCollection<int>>());
    }

    #endregion

    #region FilterNull

    [Test]
    public void FilterNull_WhenAllElementsAreNonNull_ReturnsAll()
    {
        IEnumerable<string?> source = new[] { "a", "b", "c" };

        IEnumerable<string> result = source.FilterNull();

        Assert.That(result.Count(), Is.EqualTo(3));
    }

    [Test]
    public void FilterNull_WhenSomeElementsAreNull_FiltersThemOut()
    {
        IEnumerable<string?> source = new[] { "a", null, "c", null };

        IEnumerable<string> result = source.FilterNull();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public void FilterNull_WhenAllElementsAreNull_ReturnsEmpty()
    {
        IEnumerable<string?> source = new string?[] { null, null, null };

        IEnumerable<string> result = source.FilterNull();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FilterNull_WhenSourceIsEmpty_ReturnsEmpty()
    {
        IEnumerable<string?> source = Enumerable.Empty<string?>();

        IEnumerable<string> result = source.FilterNull();

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region ForEach

    [Test]
    public void ForEach_ExecutesActionOnEachElement()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };
        var visited = new List<int>();

        source.ForEach(x => visited.Add(x));

        Assert.That(visited, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ForEach_WhenEmpty_DoesNotExecuteAction()
    {
        IEnumerable<int> source = Enumerable.Empty<int>();
        int callCount = 0;

        source.ForEach(_ => callCount++);

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void ForEach_ExecutesActionExactlyOncePerElement()
    {
        IEnumerable<int> source = new[] { 1, 2, 3, 4, 5 };
        int callCount = 0;

        source.ForEach(_ => callCount++);

        Assert.That(callCount, Is.EqualTo(5));
    }

    #endregion

    #region TakeRolling

    [Test]
    public void TakeRolling_WhenRangeIsWithinBounds_ReturnsThatSlice()
    {
        int[] array = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        IEnumerable<int> result = array.TakeRolling(skip: 2, take: 3);

        Assert.That(result, Is.EquivalentTo(new[] { 2, 3, 4 }));
    }

    [Test]
    public void TakeRolling_WhenRangeWrapsAround_WrapsFromStart()
    {
        int[] array = { 0, 1, 2, 3, 4 };

        // skip=3, take=4 → lastIndex=7 > length=5, so wraps
        IEnumerable<int> result = array.TakeRolling(skip: 3, take: 4);

        Assert.That(result, Is.EquivalentTo(new[] { 0, 1, 3, 4 }));
    }

    [Test]
    public void TakeRolling_WhenSkipIsZero_StartsFromBeginning()
    {
        int[] array = { 10, 20, 30, 40, 50 };

        IEnumerable<int> result = array.TakeRolling(skip: 0, take: 3);

        Assert.That(result, Is.EquivalentTo(new[] { 10, 20, 30 }));
    }

    #endregion

    #region Count (non-generic IEnumerable)

    [Test]
    public void Count_WhenICollection_ReturnsCorrectCount()
    {
        IEnumerable source = new List<int> { 1, 2, 3 };

        int result = source.Count();

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Count_WhenNotICollection_ReturnsCorrectCount()
    {
        // Use a custom IEnumerable that is NOT an ICollection
        IEnumerable source = Iterate(1, 2, 3);

        int result = source.Count();

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Count_WhenEmpty_ReturnsZero()
    {
        IEnumerable source = Array.Empty<int>();

        int result = source.Count();

        Assert.That(result, Is.EqualTo(0));
    }

    // Helper: yields values as a plain IEnumerable (not ICollection)
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