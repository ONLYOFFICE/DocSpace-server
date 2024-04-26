namespace ASC.PluginsLibrary;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }

    int Execute();
}
