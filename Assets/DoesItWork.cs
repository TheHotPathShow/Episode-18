using Generators;
using UnityEngine;


namespace Generators
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ReportAttribute : System.Attribute
    {
    }
}

[Report]
public class Customer
{
    public int Id { get; } = 42;
    public string Name { get; } = "Sample";
}

public class DoesItWork : MonoBehaviour
{
    void Start()
    {
        var customer = new Customer();
        foreach (var line in customer.Report())
        {
            Debug.Log(line);
        }
    }
}