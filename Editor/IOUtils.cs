using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTodo {
    internal static class IOUtils {

        
        [Serializable] struct List_string {
            public List<string> strings;
        }
        
        public static List<string> GetActiveDirectoriesFromPrefs() {
            var r = EditorPrefs.GetString( "unity-todo.activedirs", "{}" );
            return JsonUtility.FromJson<List_string>( r ).strings ?? new List<string>();
        }
        
        public static void SaveActiveDirectoriesToPrefs(List<string> activeDirectories) {
            var ls = new List_string { strings = activeDirectories };
            var r = JsonUtility.ToJson( ls );
            EditorPrefs.SetString( "unity-todo.activedirs", r );
        }

        public static List<string> FindAllDirectoriesWithTaskList() {
            var taskListDirs = new HashSet<string>();
            var guids = AssetDatabase.FindAssets( "t:TaskList" );
            foreach (var guid in guids) {
                var path = AssetDatabase.GUIDToAssetPath( guid );
                taskListDirs.Add( Path.GetDirectoryName( path ) );
            }
            return taskListDirs.ToList();
        }

        public static List<string> GetAllTaskListsAtPath(string directory) {
            var taskListPaths = new HashSet<string>();
            var guids = AssetDatabase.FindAssets( "t:TaskList", new[] { directory } );
            foreach (var guid in guids) {
                var path = AssetDatabase.GUIDToAssetPath( guid );
                taskListPaths.Add( path );
            }
            return taskListPaths.ToList();
        }
        
        public static GithubWindow.GithubData GetGithubDataFromPrefs() {
            return JsonUtility.FromJson<GithubWindow.GithubData>(EditorPrefs.GetString( "unity-todo.githubtoken", "{}" ));
        }
        public static void SaveGithubDataToPrefs(GithubWindow.GithubData data) {
            EditorPrefs.SetString( "unity-todo.githubtoken", JsonUtility.ToJson( data ) );
        }
        
    
    }
}