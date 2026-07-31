using System.Text.Json;
using Bluto.Validation;

var root = Directory.GetCurrentDirectory();
var evidenceDirectory = Path.Combine(root, "repository", "evidence", "rvec");
var reportPath = Path.Combine(root, "implementation", "validation", "report.json");

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--root" && i + 1 < args.Length)
    {
        root = Path.GetFullPath(args[++i]);
        evidenceDirectory = Path.Combine(root, "repository", "evidence", "rvec");
        reportPath = Path.Combine(root, "implementation", "validation", "report.json");
    }
    else if (args[i] == "--evidence-dir" && i + 1 < args.Length)
    {
        evidenceDirectory = Path.GetFullPath(Path.Combine(root, args[++i]));
    }
    else if (args[i] == "--report" && i + 1 < args.Length)
    {
        reportPath = Path.GetFullPath(Path.Combine(root, args[++i]));
    }
    else if (args[i] is "--help" or "-h")
    {
        Console.WriteLine("Usage: Bluto.Validation.Cli [--root <path>] [--evidence-dir <path>] [--report <path>]");
        return 0;
    }
}

var result = RepositoryValidator.Validate(new ValidationOptions(root, evidenceDirectory, reportPath));
Console.WriteLine(JsonSerializer.Serialize(result, RepositoryValidator.JsonOptions));
return result.ExitCode;
