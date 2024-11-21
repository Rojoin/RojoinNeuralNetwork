public abstract class ECSSystem
{
    public void Run(float deltaTime)
    {
        PreExecute(deltaTime);
        Execute(deltaTime);
        PostExecute(deltaTime);
    }

    public abstract void Initialize();

    protected abstract void PreExecute(float deltaTime);

    protected abstract void Execute(float deltaTime);

    protected abstract void PostExecute(float deltaTime);
}

public abstract class ECSResponce
{
    public void Run(float deltaTime)
    {
        PreExecute(deltaTime);
        Execute(deltaTime);
        PostExecute(deltaTime);
    }

    public abstract void Initialize();

    protected abstract void PreExecute(float deltaTime);

    protected abstract void Execute(float deltaTime);

    protected abstract void PostExecute(float deltaTime);
}