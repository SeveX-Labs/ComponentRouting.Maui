#if ANDROID
using System.Collections.Generic;
using System.Linq;

namespace ComponentRouting.Maui.Chrome;

public sealed class AndroidModalWindowIdentity
{
    internal AndroidModalWindowIdentity(
        IEnumerable<int> tokenHashes,
        IEnumerable<int> rootHashes,
        IEnumerable<int> viewHashes,
        bool isFallback)
    {
        TokenHashes = tokenHashes.Distinct().ToArray();
        RootHashes = rootHashes.Distinct().ToArray();
        ViewHashes = viewHashes.Distinct().ToArray();
        IsFallback = isFallback;
    }

    public IReadOnlyCollection<int> TokenHashes { get; }
    public IReadOnlyCollection<int> RootHashes { get; }
    public IReadOnlyCollection<int> ViewHashes { get; }
    public bool IsFallback { get; }
    public string Mode => IsFallback ? "fallback" : "strict";
    public bool HasCandidates => TokenHashes.Count > 0 || RootHashes.Count > 0 || ViewHashes.Count > 0;

    internal bool ContainsTokenHash(int hash)
    {
        return TokenHashes.Contains(hash);
    }

    internal bool ContainsRootHash(int hash)
    {
        return RootHashes.Contains(hash);
    }

    internal bool ContainsViewHash(int hash)
    {
        return ViewHashes.Contains(hash);
    }
}
#endif
