using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.Json.Diagnostics;

public abstract record DiagnosableResult<T>
{
#pragma warning disable CA1000 // Do not declare static members on generic types
	public static DiagnosableResult<T> Pass(T Keyword) =>
		new DiagnosableResult<T>.Success(Keyword);

	public static DiagnosableResult<T> Fail(NodeMetadata nodeInfo, DocumentRegistry registry, params DiagnosticException.ToDiagnostic[] diagnostics) =>
		new DiagnosableResult<T>.Failure(diagnostics.Select(d => d(registry.ResolveLocation(nodeInfo))).ToArray());

	public static DiagnosableResult<T> Fail(IReadOnlyList<DiagnosticBase> diagnostics) =>
		new DiagnosableResult<T>.Failure(diagnostics);

	public static DiagnosableResult<T> Fail(params DiagnosticBase[] diagnostics) =>
		new DiagnosableResult<T>.Failure(diagnostics);
#pragma warning restore CA1000 // Do not declare static members on generic types

	public abstract DiagnosableResult<TResult> Select<TResult>(Func<T, TResult> selector);

	public abstract TResult Fold<TResult>(Func<T, TResult> success, Func<IReadOnlyList<DiagnosticBase>, TResult> failure);

	protected DiagnosableResult() { }
	public record Success(T Value) : DiagnosableResult<T>
	{
		public override TResult Fold<TResult>(Func<T, TResult> success, Func<IReadOnlyList<DiagnosticBase>, TResult> failure) =>
			success(Value);

		public override DiagnosableResult<TResult> Select<TResult>(Func<T, TResult> selector) =>
			new DiagnosableResult<TResult>.Success(selector(Value));
	}
	public record Failure(IReadOnlyList<DiagnosticBase> Diagnostics) : DiagnosableResult<T>
	{
		public override DiagnosableResult<TResult> Select<TResult>(Func<T, TResult> selector) =>
			new DiagnosableResult<TResult>.Failure(Diagnostics);
		public override TResult Fold<TResult>(Func<T, TResult> success, Func<IReadOnlyList<DiagnosticBase>, TResult> failure) =>
			failure(Diagnostics);
	}
}

public static class DiagnosableResult
{
	public static DiagnosableResult<IEnumerable<T>> AggregateAll<T>(this IEnumerable<DiagnosableResult<T>> target)
	{
		return target
			.Aggregate(DiagnosableResult<IEnumerable<T>>.Pass(Enumerable.Empty<T>()),
				(prev, next) =>
					prev.Fold(
						list => next.Fold(
							(schema) => DiagnosableResult<IEnumerable<T>>.Pass(list.Concat([schema])),
							(diagnostics) => DiagnosableResult<IEnumerable<T>>.Fail(diagnostics.ToArray())
						),
						oldDiagnotics => next.Fold(
							_ => prev,
							(diagnostics) => DiagnosableResult<IEnumerable<T>>.Fail(oldDiagnotics.Concat(diagnostics).ToArray())
						))
			);
	}
}