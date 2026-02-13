namespace ASC.Data.Stress.Command;

public interface IBaseCommand
{
    static abstract string Name { get; }
    static abstract string Description { get; }
}

public static class CommandConfiguratorExtension
{
    public static ICommandConfigurator AddBaseCommand<TCommand>(this IConfigurator configurator) where TCommand : class, ICommand, IBaseCommand
    {
        return configurator.AddCommand<TCommand>(TCommand.Name)
            .WithDescription(TCommand.Description);
    }
}