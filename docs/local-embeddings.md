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
Sport[] closest = LocalEmbedder.FindClosest(
  embedder.Embed("ball game"),
  candidates,
  maxResults: 3);
```

## Reusing `LocalEmbedder` instances

`LocalEmbedder` instances are:

 * **Thread-safe**. You can share a singleton instance across many threads.
 * **Disposable**. It holds unmanaged resources since it uses the [ONNX runtime](https://onnxruntime.ai/docs/get-started/with-csharp.html) internally to run the embeddings ML model. Remember to dispose it.
 * **Expensive to create**. Each instance has to load the ML model and set up a session with ONNX.
   * Where possible, retain an instance as a singleton and reuse it. For example, register it as a DI service using `builder.Services.AddSingleton<LocalEmbedder>()`. In that case, you won't dispose it because the DI container will take care of that.

## Shrinking embeddings (quantization)

By default, `SmartComponents.LocalEmbeddings` uses an embeddings model that returns 384-dimensional embedding vectors. Each component is represented by a single-precision `float` value (4 bytes), so the memory required for a raw, unquantized embedding is 384*4 = 1536 bytes.

In many scenarios this is too much memory. For a million embeddings, it would be 1.5 GiB, which is a lot to hold in memory, and a lot to add to your database.

A common technique for reducing the space needed to store vector data is *quantization*. There are many forms of quantization. `LocalEmbeddings` has three built-in storage formats for embeddings, offering different quantizations:

| Type | Size (bytes) | Similarity | Info |
| --- | --- | --- | --- |
| `EmbeddingF32` | 1536 | Cosine | Raw, unquantized data. Each component is stored as a `float`. Maximum accuracy. |
| `EmbeddingI8` | 388 | Cosine | Each component is stored as a `sbyte` (signed byte), plus there's 4 bytes to hold a scale factor. This cuts storage significantly, while retaining good accuracy. It's similar to SQ8 quantization in Faiss. |
| `EmbeddingI1` | 48 | Hamming | Each component is stored as a single bit, equivalent to LSH quantization in Faiss. This is a massive reduction in storage, at the cost of moderate reduction in accuracy. |

When evaluating similarity, the scores are computed directly from the quantized representations, *without* expanding back to a 1536-byte representation. As such, similarity search works faster on the smaller quantizations, because the CPU is processing far fewer bytes.

You can only compute similarity within a type. That is, an `EmbeddingI1` can be compared to another `EmbeddingI1`, but not to an `EmbeddingF32`.

To get an embedding in a chosen format, pass it as a generic parameter to `Embed` or `EmbedRange`. Examples:

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

With Entity Framework, you can add a `byte[]` property onto an entity class to hold the raw data for an embedding. For example:

```cs
public class Document
{
    public int DocumentId { get; set; }
    public int OwnerId { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }

    // It's helpful to use the property name to keep track of which
    // format of embedding is being used
    public required byte[] EmbeddingI8Buffer { get; set; }
}
```

You can populate the `byte[]` property using `ToArray()`:

```cs
var doc = new Document
{
   // ... set other properties here ...
   EmbeddingI8Buffer = embedder.Embed<EmbeddingI8>(title).Buffer.ToArray()
};
```

You might want to recompute this embedding each time the user edits whatever text is used to compute it.

Next, if you need to search over a small number of entities (e.g., just the records created by the current user), it may be sufficient to load the data on demand and then run a similarity search:

```cs
using var dbContext = new MyDbContext();

// Load whatever subset of the data you want to consider
// No need to fetch all the columns - only need ID/embedding pairs
var currentUserDocs = await dbContext.Documents
    .Where(x => x.OwnerId == currentUserId)
    .Select(x => new { x.DocumentId, x.EmbeddingI8Buffer })
    .ToListAsync();

// Perform the similarity search
int[] matchingDocIds = LocalEmbedder.FindClosest(
    embedder.Embed<EmbeddingI8>(searchText),
    currentUserDocs.Select(x => (x.DocumentId, new EmbeddingI8(x.EmbeddingI8Buffer))),
    maxResults: 5);

// Load the complete entities for the matching documents
var matchingDocs = await dbContext.Documents
    .Where(x => matchingDocIds.Contains(x.DocumentId))
    .ToDictionaryAsync(x => x.DocumentId);
var matchingDocsInOrder = matchingDocIds.Select(x => matchingDocs[x]);
```

In many cases you'll want to search over a large number of entities, e.g., tens of thousands of entities shared across all users. You would not want to retrieve them all from the database for every search (especially for each keystroke in a Smart ComboBox). Instead, it would make sense to have the server cache the list of ID/embedding pairs in memory. An `(int Id, EmbeddingI1 Embedding)` pair would be only 52 bytes, so holding a million of them would not be problematic (52 MiB). You could cache them in a [MemoryCache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-8.0) that will expire at regular intervals, offering a tradeoff between database load and freshness of results.

## Customizing the underlying embeddings model

`LocalEmbedder` works by using the [ONNX runtime](https://onnxruntime.ai/docs/get-started/with-csharp.html), which can execute many different embeddings models on CPU or GPU (and often, CPU works faster for such small models).

The `SmartComponents.LocalEmbeddings` NuGet package does not actually contain any ML model, but it is configured to download a model when you first build your application. You can configure which model is downloaded.

The default model that gets downloaded on build is [bge-micro-v2](https://huggingface.co/TaylorAI/bge-micro-v2), an MIT-licensed BERT embedding model, which has been quantized down to just 22.9 MiB, runs efficiently on CPU, and [scores well on benchmarks](https://huggingface.co/spaces/mteb/leaderboard) - outperforming many gigabyte-sized models.

If you want to use a different model, specify the URL to its `.onnx` file and the vocabulary that should be used for tokenization. For example, to use [gte-tiny](https://huggingface.co/TaylorAI/gte-tiny), set the following in your `.csproj`:

```xml
<PropertyGroup>
  <LocalEmbeddingsModelUrl>https://huggingface.co/TaylorAI/gte-tiny/resolve/main/onnx/model_quantized.onnx</LocalEmbeddingsModelUrl>
  <LocalEmbeddingsVocabUrl>https://huggingface.co/TaylorAI/gte-tiny/resolve/main/vocab.txt</LocalEmbeddingsVocabUrl>
</PropertyGroup>
```

**Requirements:** The model must be in ONNX format, accept BERT-tokenized text, accept inputs labelled `input_ids`, `attention_mask`, `token_type_ids`, and return an output tensor suitable for mean pooling. Many [sentence transformer](https://www.sbert.net/) models on Hugging Face follow these patterns. These are often 384-dimensional embeddings.

## Performance

As a rough approximation, based on an Intel i9-11950H CPU:

 * Using `embedder.Embed` for a 50-character string may take around 0.5ms of CPU time (shorter text is quicker).
   * So, if you're computing embeddings over many thousands of strings (or very long strings), it's worth storing the computed embeddings in your existing database (e.g., each time a user saves changes to the corresponding text) instead of recomputing them all from scratch each time the app restarts.
 * An in-memory, single-threaded similarity search using `LocalEmbedder.FindClosest` with `EmbeddingF32` can search through 1,000 candidates in around 0.06ms, or 100,000 candidates in around 6ms (it's linear in the number of candidates, independent of the text length). This goes down to ~2.8ms if you use `EmbeddingI1`.
   * So, if you need to search through tens of millions of candidates, you should consider more advanced similarity search options such as using [Faiss](https://github.com/facebookresearch/faiss) or an external vector database.
   * From benchmarks, `LocalEmbedder.FindClosest` performance is equivalent to [Faiss using its `Flat` index type](https://github.com/facebookresearch/faiss/wiki/Faiss-indexes). You'll only get better speeds from Faiss using its more powerful indexes such as HNSW or IVF, which requires training on your data.

#### Recommendations for scaling up

The overall goal for `SmartComponents.LocalEmbeddings` is to make semantic search easy to get started with. It may be sufficient for many applications. But if you outgrow it:

 * You can use an external service to compute embeddings, e.g., [OpenAI embeddings](https://platform.openai.com/docs/guides/embeddings) or [Azure OpenAI embeddings](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/embeddings)
 * You can perform similarity search using [Faiss](https://github.com/facebookresearch/faiss) on your server (e.g., in .NET via [FaissSharp](https://gitlab.com/josetruyol/faisssharp), [faissmask](https://github.com/andyalm/faissmask), or [FaissNet](https://github.com/fwaris/FaissNet)). This allows you to set up much more powerful indexes that can be trained on your own data. It's a lot more to learn.
 * Or instead of Faiss, you can use an external vector database such as [pgvector](https://github.com/pgvector/pgvector) or cloud-based vector database services.

## Usage with Semantic Kernel

If you want to use this ONNX-based local embeddings generator with [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/), the only package you need to reference is `SmartComponents.LocalEmbeddings.SemanticKernel`.

Once you've referenced that package, you can use `AddLocalTextEmbeddingGeneration` to add a local embeddings generator to your `Kernel`:

```cs
var builder = Kernel.CreateBuilder();
builder.AddLocalTextEmbeddingGeneration();
```

... and then you can generate embeddings in the usual way for Semantic Kernel:

```cs
var kernel = builder.Build();
var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();

var embedding = await embeddingGenerator.GenerateEmbeddingAsync("Some text here");
```
