# Local Embeddings

TODO

## Storing and querying using Entity Framework

How to add an `Embedding` field to an entity, and then load and search through all the entities.

## Using memory/storage more efficiently

Quantization

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
