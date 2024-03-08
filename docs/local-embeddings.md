# Local Embeddings

*Embeddings* are used for semantic similarity search. Natural-language strings are converted into numerical vectors called *embeddings*. The more conceptually related are two strings, the closer their vectors.

While you can use an external AI service to compute embeddings, in many cases you can simply compute them locally on your server (no need for a GPU - the CPU will work fine). `SmartComponents.LocalEmbeddings` is a package to simplify doing this.

With `SmartComponents.LocalEmbeddings`, you can compute embeddings in under a millisecond, and perform semantic search over hundreds of thousands of candidates in single-digit milliseconds. However, there are limits. To understand the performance characteristics and when you might benefit from moving to an external vector database, see *Performance* below.

## Getting started

Reference the package `SmartComponents.LocalEmbeddings`:

 * Command line: `dotnet add package --prerelease SmartComponents.LocalEmbeddings`
 * Visual Studio: Right-click your project name, choose *Manage NuGet packages...*, and then search for and install `SmartComponents.LocalEmbeddings`.
   * Note: Check the *Include prerelease* option if needed.

You can now compute embeddings of strings:

```cs
using var embedder = new LocalEmbedder();
var cat = embedder.Embed("Cats can be blue");
var dog = embedder.Embed("Dogs can be red");
var snooker = embedder.Embed("Snooker world champion Stephen Hendry");
```

... and assess their semantic similarity:

```cs
var kitten = embedder.Embed("Kittens!!!");
Console.WriteLine(kitten.Similarity(kitten));  // 1.00
Console.WriteLine(kitten.Similarity(cat));     // 0.65
Console.WriteLine(kitten.Similarity(dog));     // 0.53
Console.WriteLine(kitten.Similarity(snooker)); // 0.37
```

As you can see, `"Kittens!!!"` is:

 * ... perfectly related to itself
 * ... fairly related to the statement about cats
 * ... less related to the statement about dogs
 * ... very unrelated to the statement about snooker

## Peforming similarity search

If you want, you can find the closest matches from a set of candidate embeddings simply using `candidates.OrderByDescending(x => x.Similarity(target)).Take(count)`.

However, it's a little more efficient to use `LocalEmbedder.FindClosest`, because it only sorts the best N matches, instead of sorting *all* the candidates. `FindClosest` accepts the following parameters:

 * `target`: An embedding previously returned by `embedder.Embed` or `embedder.EmbedRange`
 * `candidates`: An enumerable of tuples of the form `(item, embedding)`. The `item` can be the `string` that you embedded, or it can be any other object of generic type `T`.
 * `maxResults`: The maximum number of results
 * `minSimilarity`: Optional. If set, candidates with a similarity below this threshold won't be included.

The return value is an array of `T` values, ordered most-similar-first. If you want the similarity scores too, use `LocalEmbedder.FindClosestWithScore` instead, which returns an array of `SimilarityScore<T>` giving both the `T` and its score.

For example, given this class:

```cs
class Sport
{
    public string Name { get; init; }
    public EmbeddingF32 Embedding { get; init; }
}
```

... and this data:

```cs
var sportNames = new[] { "Soccer", "Tennis", "Swimming", "Horse riding", "Golf", "Gymnastics" };

var sports = sportNames.Select(name => new Sport
{
    Name = name,
    Embedding = embedder.Embed(name)
}).ToArray();
```

You can find the closest 3 `Sport` instances for the string `"ball game"`:

```cs
var candidates = sports.Select(a => (a, a.Embedding));
var target = embedder.Embed("ball game");
Sport[] closest = LocalEmbedder.FindClosest(target, candidates, maxResults: 3);

// Displays: Soccer, Golf, Tennis
Console.WriteLine(string.Join(", ", closest.Select(x => x.Name)));
```

While at first it might feel cumbersome to pass an enumerable of tuples for `candidates`, this allows you to get back any data type (e.g., the strings that were embedded, or entity objects holding those embeddings, or just their `int` ID values), and allows you to prefilter by applying a `.Where(...)` clause, all without any extra memory allocations.

Alternatively, as shorthand, you can use `EmbedRange` to produce the tuples over many inputs at once:

```cs
var candidates = embedder.EmbedRange(sports, x => x.Name);
```

## Reusing `LocalEmbedder` instances

`LocalEmbedder` instances are:

 * **Thread-safe**. You can share a singleton instance across many threads.
 * **Disposable**. It holds unmanaged resources since it uses the [Onnx runtime](https://onnxruntime.ai/docs/get-started/with-csharp.html) internally to run the embeddings ML model. Remember to dispose it.
 * **Expensive to create**. Each instance has to load the ML model and set up a session with Onnx.
   * Where possible, retain an instance as a singleton and reuse it. For example, register it as a DI service using `builder.Services.AddSingleton<LocalEmbedder>()`. In that case, you won't dispose it because the DI container will take care of that.

## Shrinking embeddings (quantization)

By default, `SmartComponents.LocalEmbeddings` uses an embeddings model that returns 384-dimensional embedding vectors. Each component is represented by a single-precision `float` value (4 bytes), so the total memory required for raw, unquantized embeddings is 384*4 = 1536 bytes.

In many scenarios this is too much data. For a million embeddings, it would be 1.5 GiB, which is a lot to hold in memory, and a lot to add to your database.

A common technique for reducing the space needed to store vector data is *quantization*. There are many forms of quantization. `LocalEmbeddings` has three built-in storage formats for embeddings, offering different quantizations:

| Type | Size (bytes) | Similarity | Info |
| --- | --- | --- | --- |
| `EmbeddingF32` | 1536 | Cosine | Raw, unquantized data. Each component is stored as a `float`. Maximum accuracy. |
| `EmbeddingI8` | 388 | Cosine | Each component is stored as a `sbyte` (signed byte), plus there's 4 bytes to hold a scale factor. This cuts storage significantly, while retaining good accuracy. It's similar to SQ8 quantization in Faiss. |
| `EmbeddingI1` | 48 | Hamming | Each component is stored as a single bit, equivalent to LSH quantization in Faiss. This is a massive reduction in storage, at the cost of moderate reduction in accuracy. |

When evaluating similarity, the scores are computed directly from the quantized representations, *without* expanding back to a 1536-byte representation. As such, similarity search works faster on the smaller quantizations, because the CPU is processing far fewer bytes.

You can only compute similarity within a type. That is, an `EmbeddingI1` can be compared to another `EmbeddingI1`, but not to an `EmbeddingF32`.

To get an embedding in a chosen format, pass it as a generic type to `Embed` or `EmbedRange`. Examples:

```cs
// To produce a single embedding:
var embedding = embedder.Embed<EmbeddingI1>(someString);

// Or to produce a set of (item, embedding) pairs:
var candidates = embedder.EmbedRange<Sport, EmbeddingI1>(sports, x => x.Name);
```

## Persisting embeddings

When you want to save embeddings to a file or database, you can use the `Buffer` property to access the raw memory as a `ReadOnlyMemory<byte>`. This property is available on any embedding type. Example:

```cs
// Normally you'd store embeddings in a database, not a file on disk,
// but for simplicity let's use a file
var originalEmbedding = embedder.Embed<EmbeddingF32>("The chickens are here to see you");
using (var file = File.OpenWrite("somefile"))
{
    await file.WriteAsync(originalEmbedding.Buffer);
}

// Now load it back from disk. Be sure to use the same embedding type.
var loadedBuffer = File.ReadAllBytes("somefile");
var loadedEmbedding = new EmbeddingF32(loadedBuffer);

// Displays "1" (the embeddings are identical)
Console.WriteLine(originalEmbedding.Similarity(loadedEmbedding));
```

If you want to access the numerical values of the vector components (e.g., to store them in an external vector database), you can use the following properties:

| Embedding type | Values properties | Values type |
| --- | --- | --- |
| `EmbeddingF32` | `Values` | `ReadOnlyMemory<float>` |
| `EmbeddingI8` | `Values` and `Magnitude` | `ReadOnlyMemory<sbyte>` and `float` |
| `EmbeddingI1` | Not available... | ... because the packed bits are simply what you find in `Buffer` |

## Storing and querying using Entity Framework

How to add an `Embedding` field to an entity, and then load and search through all the entities.

## How it works (ONNX and the model)

gte-tiny

Remember to dispose

## Customizing the model

E.g., use bge-tiny or other bert-based 384-dimensional embedding models

### Performance

As a rough approximation:

 * Using `embedder.Embed` for a paragraph of text may take around 0.5ms-2ms of CPU time (shorter text is quicker).
   * So, if you're computing embeddings over many thousands of strings (or very long strings), it's worth storing the computed embeddings in your existing database (e.g., each time a user saves changes to the corresponding text) instead of recomputing them all from scratch each time the app restarts.
 * An in-memory, single-threaded similarity search using `embedder.FindClosest` can search through 1,000 candidates in around 0.06ms, or 100,000 candidates in around 6ms (it's linear in the number of candidates, independent of the text length).
   * So, if you need to search through tens of millions of candidates, you should consider more advanced similarity search options such as using [Faiss](https://github.com/facebookresearch/faiss) or an external vector database.
