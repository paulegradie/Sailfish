using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Markdown;

public interface IMarkdownWriter
{
    Task Present(List<CompiledResultContainer> result, string filePath);
}