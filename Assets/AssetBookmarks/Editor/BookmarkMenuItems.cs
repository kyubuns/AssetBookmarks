using System;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBookmarks.Editor
{
    internal static class BookmarkMenuItems
    {
        private static readonly string[] BuiltInRoots =
        {
            "File",
            "Edit",
            "Assets",
            "GameObject",
            "Component",
            "Tools",
            "Services",
            "Window",
            "Help",
        };

        private static string[] paths;
        private static HashSet<string> pathSet;

        internal static IReadOnlyList<string> All
        {
            get
            {
                EnsureLoaded();
                return paths;
            }
        }

        internal static bool Contains(string menuPath)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(menuPath) && pathSet.Contains(menuPath);
        }

        internal static void Refresh()
        {
            paths = LoadPaths();
            pathSet = new HashSet<string>(paths, StringComparer.Ordinal);
        }

        private static void EnsureLoaded()
        {
            if (paths != null && paths.Length > 0)
            {
                return;
            }

            Refresh();
        }

        private static string[] LoadPaths()
        {
            var uniquePaths = new HashSet<string>(StringComparer.Ordinal);
            var menuItems = new List<string>();
            foreach (var root in GetMenuRoots())
            {
                var submenuPaths = Unsupported.GetSubmenus(root) ?? Array.Empty<string>();
                foreach (var path in submenuPaths)
                {
                    if (string.IsNullOrWhiteSpace(path) ||
                        path.StartsWith("CONTEXT/", StringComparison.Ordinal) ||
                        path.StartsWith("internal:", StringComparison.OrdinalIgnoreCase) ||
                        !uniquePaths.Add(path))
                    {
                        continue;
                    }

                    menuItems.Add(path);
                }
            }

            menuItems.Sort(StringComparer.OrdinalIgnoreCase);
            return menuItems.ToArray();
        }

        private static HashSet<string> GetMenuRoots()
        {
            var roots = new HashSet<string>(BuiltInRoots, StringComparer.Ordinal);
            foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
            {
                foreach (MenuItem attribute in method.GetCustomAttributes(typeof(MenuItem), false))
                {
                    var menuPath = attribute.menuItem;
                    var separatorIndex = menuPath?.IndexOf('/') ?? -1;
                    if (separatorIndex > 0)
                    {
                        var root = menuPath.Substring(0, separatorIndex);
                        if (root != "CONTEXT" &&
                            !root.StartsWith("internal:", StringComparison.OrdinalIgnoreCase))
                        {
                            roots.Add(root);
                        }
                    }
                }
            }

            return roots;
        }
    }
}
