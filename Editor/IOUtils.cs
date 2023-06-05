using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityTodo {
    internal static class IOUtils {
        public static List<string> FindAllDirectoriesWithTaskList() {
            var taskListDirs = new HashSet<string>();
            var guids = UnityEditor.AssetDatabase.FindAssets( "t:TaskList" );
            foreach (var guid in guids) {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath( guid );
                taskListDirs.Add( Path.GetDirectoryName( path ) );
            }
            return taskListDirs.ToList();
        }

        public static List<string> GetAllTaskListsAtPath(string directory) {
            var taskListPaths = new HashSet<string>();
            var guids = UnityEditor.AssetDatabase.FindAssets( "t:TaskList", new[] { directory } );
            foreach (var guid in guids) {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath( guid );
                taskListPaths.Add( path );
            }
            return taskListPaths.ToList();
        }
    }
}