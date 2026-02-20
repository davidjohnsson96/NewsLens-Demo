using FactRepository.Utilities;

namespace Linkage;

public interface IThreadLinker
{
    string Name { get; }
    string? LinkToThread(HashSet<string> entities,  HashSet<string> keywords, IEnumerable<EventThread> candidateThreads);
}