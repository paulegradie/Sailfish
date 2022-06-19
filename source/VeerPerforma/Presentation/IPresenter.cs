using VeerPerforma.Statistics;

namespace VeerPerforma.Presentation;

public interface IConsoleWriter : IPresenter
{
}

public interface ICsvWriter : IPresenter
{
}

public interface IMarkdownWriter : IPresenter
{
}

public interface IPresenter
{
    void Present(CompiledResultContainer result);
}