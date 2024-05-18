using Generators;

namespace MakeQueryable.Sample;

// This code will not compile until you build the project with the Source Generators

[Report]
public class SampleEntity
{
    public int Id { get; } = 42;
    public string? Name { get; } = "Sample";
}

[Report]
public class CakeEntity
{
    internal int m_Id { get; }
}