namespace Mechahouse.Core.Domain;

public class Entity
{
    private readonly Dictionary<Type, object> _components = [];

    protected Entity(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyCollection<object> Components => _components.Values;

    public bool HasComponent<T>() where T : class => _components.ContainsKey(typeof(T));

    public bool TryGetComponent<T>(out T? component) where T : class
    {
        if (_components.TryGetValue(typeof(T), out var value))
        {
            component = (T)value;
            return true;
        }

        component = null;
        return false;
    }

    public T GetComponent<T>() where T : class
    {
        if (!TryGetComponent<T>(out var component) || component is null)
        {
            throw new InvalidOperationException($"Entity '{Name}' does not have a {typeof(T).Name}.");
        }

        return component;
    }

    protected void AddComponent<T>(T component) where T : class
    {
        _components[typeof(T)] = component;
    }
}
