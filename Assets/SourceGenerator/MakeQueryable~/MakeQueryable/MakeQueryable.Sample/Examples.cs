using System;
using System.Collections.Generic;
using Generators;

namespace MakeQueryable.Sample;

public class Examples
{
    // Execute generated method Report
    public IEnumerable<string> CreateEntityReport(SampleEntity entity)
    {
        return entity.Report();
    }
    
    public static void Main()
    {
        var examples = new Examples();
        var entity = new SampleEntity();
        foreach (var line in examples.CreateEntityReport(entity))
        {
            Console.WriteLine(line);
        }
    }
}