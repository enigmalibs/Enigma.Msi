using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Enigma.Msi.Model;
using Xunit;

namespace Enigma.Msi.UnitTests.Model;

/// <summary>
/// Guards the fixture the round-trip tests depend on: <see cref="TestPackages.CreateFull"/> must
/// populate every member of the model graph. Without this, a member added to the model later would be
/// left null in the fixture and the "round-trip preserves every field" test would silently stop
/// covering it.
/// </summary>
public sealed class FullPackageCoverageTests
{
    [Fact]
    public void CreateFull_PopulatesEveryModelMember()
    {
        var unpopulated = new List<string>();

        Inspect(TestPackages.CreateFull(), string.Empty, unpopulated);

        Assert.Empty(unpopulated);
    }

    private static void Inspect(object instance, string path, ICollection<string> unpopulated)
    {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string memberPath = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
            object? value = property.GetValue(instance);

            switch (value)
            {
                case null:
                case string text when string.IsNullOrWhiteSpace(text):
                    unpopulated.Add(memberPath);
                    break;
                case Guid guid when guid == Guid.Empty:
                    unpopulated.Add(memberPath);
                    break;
                case string or Guid or Version:
                    break;
                case IEnumerable items:
                    InspectAll(items, memberPath, unpopulated);
                    break;
                default:
                    if (IsModelType(value.GetType()))
                    {
                        Inspect(value, memberPath, unpopulated);
                    }

                    break;
            }
        }
    }

    private static void InspectAll(IEnumerable items, string path, ICollection<string> unpopulated)
    {
        int count = 0;

        foreach (object? item in items)
        {
            string itemPath = $"{path}[{count}]";
            count++;

            if (item is null)
            {
                unpopulated.Add(itemPath);
            }
            else if (IsModelType(item.GetType()))
            {
                Inspect(item, itemPath, unpopulated);
            }
        }

        if (count == 0)
        {
            unpopulated.Add(path);
        }
    }

    private static bool IsModelType(Type type)
        => !type.IsEnum && type.Namespace == typeof(MsiPackage).Namespace;
}
