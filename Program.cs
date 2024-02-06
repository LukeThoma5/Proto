using Cocona;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = CoconaApp.CreateBuilder();

builder.Logging.AddDebug();
builder.Services.AddLogging();

var app = builder.Build();

app.AddCommand("duplicate", (bool dryRun = false) =>
{
    var currentDirectory = Directory.GetCurrentDirectory();

    Console.WriteLine("Copying directory {0}", currentDirectory);
    Console.WriteLine("Is that correct? (y/n)");
    var correct = Console.ReadLine();
    if (correct != "y")
    {
        Console.WriteLine("Aborting");
        return;
    }

    Console.WriteLine("Enter the singular version of the old name");
    var oldSingular = Console.ReadLine() switch
    {
        var x when string.IsNullOrWhiteSpace(x) => throw new Exception("No singular name entered"),
        var x => x!.Trim()
    };
    Console.WriteLine("Enter the singular version of the new name");
    var newSingular = Console.ReadLine() switch
    {
        var x when string.IsNullOrWhiteSpace(x) => throw new Exception("No singular name entered"),
        var x => x!.Trim()
    };

    Console.WriteLine(
        "Enter the plural version of the old name (leave blank to use the singular name with an 's' appended)");
    var oldPlural = Console.ReadLine() switch
    {
        var x when string.IsNullOrWhiteSpace(x) => oldSingular + "s",
        var x => x!.Trim()
    };
    Console.WriteLine(
        "Enter the plural version of the new name (leave blank to use the singular name with an 's' appended)");
    var newPlural = Console.ReadLine() switch
    {
        var x when string.IsNullOrWhiteSpace(x) => newSingular + "s",
        var x => x!.Trim()
    };


    string ToCamelCase(string input)
    {
        return input[..1].ToLower() + input[1..];
    }

    string Replace(string input)
    {
        input = input.Replace(oldPlural, newPlural);
        input = input.Replace(ToCamelCase(oldPlural), ToCamelCase(newPlural));
        input = input.Replace(oldSingular, newSingular);
        input = input.Replace(ToCamelCase(oldSingular), ToCamelCase(newSingular));
        return input;
    }

    var currentDirectoryInfo = new DirectoryInfo(currentDirectory);
    var newDirectoryName = Replace(currentDirectoryInfo.Name);
    var newDirectory = new DirectoryInfo(Path.Combine(currentDirectoryInfo.Parent!.FullName, newDirectoryName));

    if (newDirectory.Exists)
    {
        Console.WriteLine("Directory {0} already exists. Aborting", newDirectory.FullName);
        return;
    }

    if (!dryRun)
    {
        newDirectory.Create();
    }

    foreach (var file in currentDirectoryInfo.GetFiles("*", SearchOption.AllDirectories))
    {
        var fileDirectory = file.Directory!.FullName.Replace(currentDirectoryInfo.FullName, newDirectory.FullName);
        fileDirectory = Replace(fileDirectory);
        if (!Directory.Exists(fileDirectory) && !dryRun)
        {
            Directory.CreateDirectory(fileDirectory);
        }

        var newFileName = Replace(file.Name);
        var newFullName = Path.Combine(fileDirectory, newFileName);
        var contents = file.OpenText().ReadToEnd();
        contents = Replace(contents);
        Console.WriteLine("Copying {0} to {1}", file.FullName, newFullName);
        if (!dryRun)
        {
            File.WriteAllText(newFullName, contents);
        }
    }

    Console.WriteLine("Complete");
});

await app.RunAsync();