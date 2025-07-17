namespace Yarp;

public class BackendServiceCategory
{
    public string? Category { get; set; }
    public List<BackendService>? Services { get; set; }
}

public class BackendService
{
    public string? Name { get; set; }
    public string? IdentifierPath { get; set; }
    public string? Url { get; set; }
}