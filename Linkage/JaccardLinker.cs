using FactRepository.Utilities;
using Microsoft.Extensions.Options;

namespace Linkage;

public class JaccardLinker(IOptions<ThreadLinkerOptions> options, string name) : IThreadLinker
{
    public string Name { get; } = name;
    private readonly ThreadLinkerOptions _options = options.Value;

    public string? LinkToThread(HashSet<string> entities,  HashSet<string> keywords, IEnumerable<EventThread> candidateThreads)
    {
        string? bestId = null; double best = 0;
        foreach (var t in candidateThreads)
        {
            var sKw  = Jaccard(keywords, t.Keywords ?? []);
            var sEnt = Jaccard(entities, t.Entities ?? []);
            var score = _options.EntitiesScoreWeight * sEnt +
                        _options.KeywordScoreWeight * sKw;
            if (!(score > best)) continue;
            best = score; bestId = t.ThreadId;
        }

        var threadId = best >= _options.ThreadMatchingScoreThreshold ? bestId : Helpers.CreateNewThreadId(entities, keywords);
        Console.WriteLine($"Score of best was {best:F4} | Threshold score is {_options.ThreadMatchingScoreThreshold}");
        return threadId;
    }
    
    public double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0) return 0;
        var inter = a.Intersect(b).Count();
        var uni   = a.Union(b).Count();
        return uni == 0 ? 0 : (double)inter / uni;
    }
}