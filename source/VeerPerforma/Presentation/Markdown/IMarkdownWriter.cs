using System.Collections.Generic;
using System.Threading.Tasks;
using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation.Markdown;

public interface IMarkdownWriter
{
    Task Present(List<CompiledResultContainer> result, string filePath);
}