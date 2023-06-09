using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTodo {
    internal sealed class GithubWindow : PopupWindowContent {

        public static void Show(Rect position, TodoWindow todoWindow) {
            var window = new GithubWindow();
            window.data = IOUtils.GetGithubDataFromPrefs();
            window.todoWindow = todoWindow;
            window._directory = todoWindow.taskListPaths.Count > 0 ? todoWindow.taskListPaths[0].path : "";
            PopupWindow.Show( position, window );
        }

        public TodoWindow todoWindow;

        GithubData data;
        ProjectHeader[] _projectHeaders;
        GithubBoardColumn[] _boardColumns;
        string _directory;

        bool _busy;
        int? _errorStatusCode;

        [Serializable] internal struct ProjectHeader {
            public string title;
            public string id;
        }

        [Serializable] internal struct GithubData {
            public string token;
            public string username;
        }

        [Serializable] struct GithubBoardColumn {
            public string name;
            public Item[] items;

            [Serializable] public struct Item {
                public string id;
                public string name;
                public string body;
            }
        }


        public override Vector2 GetWindowSize() {
            return new Vector2( 800, 300 );
        }

        public override void OnGUI(Rect rect) {
            rect.x += 10;
            rect.width -= 20;
            rect.y += 10;

            using (new EditorGUI.DisabledScope( _busy )) {
                rect.height = GUIStyles.GetBigLabel().CalcHeight( new("Github Sync"), rect.width ) + 10;
                GUI.Label( rect, new GUIContent( "   Github Sync", GUIStyles.TodoWindow_GetGithubIconTex() ), GUIStyles.GetBigLabel() );
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing + 10;

                rect.height = EditorGUIUtility.singleLineHeight;
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    // token
                    data.token = EditorGUI.PasswordField( rect, "Token", data.token );
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    // username
                    data.username = EditorGUI.TextField( rect, "Username", data.username );
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    if (check.changed)
                        IOUtils.SaveGithubDataToPrefs( data );
                }

                rect.height = 30;
                if (GUI.Button( rect, "Get All Projects" )) GetProjects();
                rect.y += rect.height + 5;

                if (_errorStatusCode != null) {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.HelpBox( rect, $"Error: {_errorStatusCode}", MessageType.Error );
                }

                if (_projectHeaders != null) {
                    for (int i = 0; i < _projectHeaders.Length; i++) {
                        // remote project
                        var nrect = new Rect( rect );
                        nrect.height = EditorGUIUtility.singleLineHeight;

                        // title
                        nrect.width *= 0.4f;
                        EditorGUI.LabelField( nrect, $"<b>{_projectHeaders[i].title}</b>", GUIStyles.GetSmallLabel() );
                        nrect.x += nrect.width;

                        // id
                        nrect.width = rect.width * 0.3f;
                        EditorGUI.LabelField( nrect, $"({_projectHeaders[i].id})", GUIStyles.Task_GetFinishedDescText() );
                        nrect.x += nrect.width;

                        // open remote 
                        nrect.width = rect.width * 0.1f;
                        if (GUI.Button( nrect, "Open" )) OpenProject( i + 1 );

                        // pull from remote
                        nrect.x += nrect.width;
                        if (GUI.Button( nrect, "Pull" )) { GetAllBoardData( _projectHeaders[i].id ); }

                        // push to remote
                        nrect.x += nrect.width;
                        if (GUI.Button( nrect, "Push" )) { }

                        rect.y += nrect.height + EditorGUIUtility.standardVerticalSpacing;
                    }
                }

                // Pull from remote finalize
                if (_boardColumns != null) {
                    rect.y += 15;
                    rect.height = GUIStyles.GetSmallLabel().CalcHeight( new GUIContent( "Items fetched" ), rect.width );
                    using (new GUIUtilities.GUIColor( Color.green )) 
                        GUI.Label( rect, "Items fetched", GUIStyles.GetNormalLabel() );
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    rect.height = EditorGUIUtility.singleLineHeight;
                    GUI.Label( rect, "Select a directory and override local items.", GUIStyles.GetSmallLabel() );
                    rect.y += rect.height;

                    // dir label
                    var w = rect.width;
                    var x = rect.x;
                    rect.width = w * 0.2f;
                    GUI.Label( rect, "Directory: " );
                    rect.x += rect.width;

                    // dir selection
                    rect.width = w * 0.8f;
                    if (GUI.Button( rect, new GUIContent( _directory, GUIStyles.TodoWindow_GetTaskListDirectoriesTex() ) )) {
                        var menu = new GenericMenu();
                        foreach (var dir in IOUtils.FindAllDirectoriesWithTaskList()) 
                            menu.AddItem( new(dir), false, () => _directory = dir );
                        menu.ShowAsContext();
                        editorWindow.Repaint();
                    }

                    // confirm
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                    rect.width = w; rect.x = x;
                    rect.height = 30;
                    using (new GUIUtilities.GUIColor( Color.green )) {
                        if (GUI.Button( rect, "Confirm" ))
                            PerformPull();
                    }
                }
            }
        }

        void PerformPull() {
            if (!Directory.Exists( _directory )) {
                Debug.LogError( "Select a directory first" );
                return;
            }
            todoWindow.ForceSaveAllTaskEditors();

            var remoteColumnsList = _boardColumns.ToList();
            
            // assign task lists
            for (int i = 0; i < remoteColumnsList.Count; i++) {
                // check if any file name in directory matches this item's title
                var assetPath = Directory.GetFiles( _directory, "*.asset", SearchOption.TopDirectoryOnly )
                    .ToList()
                    .Find( f => f.Contains( remoteColumnsList[i].name ) );
                var taskList = AssetDatabase.LoadAssetAtPath<TaskList>( assetPath );
                if (taskList == null) {
                    // if no file matches, create a new one
                    taskList = ScriptableObject.CreateInstance<TaskList>();
                    AssetDatabase.CreateAsset( taskList, Path.Combine( _directory, remoteColumnsList[i].name + ".asset" ) );
                }


                // assign column/TaskList
                var rColumn = remoteColumnsList[i];
                taskList.title = rColumn.name;
                taskList.order = i;

                // assign tasks
                for (int j = 0; j < rColumn.items.Length; j++) {
                    if (taskList.tasks.Count <= j) { taskList.tasks.Add( new Task() ); }

                    taskList.tasks[j].title = rColumn.items[j].name;
                    taskList.tasks[j].description = rColumn.items[j].body;
                }

                // remove extra tasks
                while (taskList.tasks.Count > rColumn.items.Length) { taskList.tasks.RemoveAt( taskList.tasks.Count - 1 ); }
            }
            
            // remove extra task lists
            foreach (var dir in Directory.GetFiles( _directory, "*.asset", SearchOption.TopDirectoryOnly )) {
                var tasklist = AssetDatabase.LoadAssetAtPath<TaskList>( dir );
                if (tasklist != null) {
                    if (remoteColumnsList.Any( r => r.name == tasklist.title )) {
                        continue;
                    }
                }

                AssetDatabase.DeleteAsset( dir );
                Debug.Log( $"Deleted {dir}" );
            }

            AssetDatabase.SaveAssets();
            todoWindow.ForceReloadAllTaskEditors();
            editorWindow.Close();
        }

        void OpenProject(int index) {
            var url = $"https://github.com/users/{data.username}/projects/{index}";
            Application.OpenURL( url );
        }

        async void GetProjects() {
            _busy = true;
            editorWindow.Repaint();

            using (var client = new HttpClient()) {
                client.DefaultRequestHeaders.Add( "Accept", "application/vnd.github+json" );
                client.DefaultRequestHeaders.Add( "Authorization", $"Bearer {data.token}" );
                client.DefaultRequestHeaders.Add( "X-GitHub-Api-Version", "2022-11-28" );
                client.DefaultRequestHeaders.Add( "User-Agent", "UnityTodo" );
                var content =
                    new StringContent(
                        $@"{{""query"":""{{user(login: \""{data.username}\"") {{projectsV2(first: 20) {{nodes {{id title}}}}}}}}""}}" );
                var response = await client.PostAsync( "https://api.github.com/graphql", content );

                var json = await response.Content.ReadAsStringAsync();
                _errorStatusCode = response.StatusCode != HttpStatusCode.OK ? (int)response.StatusCode : null;

                Debug.Log( $"{response.StatusCode}: {json}" );

                _projectHeaders = JsonConvert.DeserializeObject<JObject>( json )["data"]["user"]["projectsV2"]["nodes"]
                    .Select( n => n.ToObject<ProjectHeader>() ).ToArray();
            }

            _busy = false;
            editorWindow.Repaint();
        }

        async void GetAllBoardData(string id) {
            _busy = true;
            editorWindow.Repaint();

            try {
                using (var client = new HttpClient()) {
                    client.DefaultRequestHeaders.Add( "Accept", "application/vnd.github+json" );
                    client.DefaultRequestHeaders.Add( "Authorization", $"Bearer {data.token}" );
                    client.DefaultRequestHeaders.Add( "X-GitHub-Api-Version", "2022-11-28" );
                    client.DefaultRequestHeaders.Add( "User-Agent", "UnityTodo" );

                    var content = new StringContent(
                        $@"{{""query"":""query{{ node(id: \""{id}\"") {{ ... on ProjectV2 {{ items(first: 20) {{ nodes{{ id fieldValues(first: 8) {{ nodes{{ ... on ProjectV2ItemFieldTextValue {{ text field {{ ... on ProjectV2FieldCommon {{  name }}}}}} ... on ProjectV2ItemFieldDateValue {{ date field {{ ... on ProjectV2FieldCommon {{ name }} }} }} ... on ProjectV2ItemFieldSingleSelectValue {{ name field {{ ... on ProjectV2FieldCommon {{ name }}}}}}}}}} content{{ ... on DraftIssue {{ title body }} ...on Issue {{ title assignees(first: 10) {{ nodes{{ login }}}}}} ...on PullRequest {{ title assignees(first: 10) {{ nodes{{ login }}}}}}}}}}}}}}}}}}""}}" );
                    var response = await client.PostAsync( "https://api.github.com/graphql", content );
                    var json = await response.Content.ReadAsStringAsync();
                    _errorStatusCode = response.StatusCode != HttpStatusCode.OK ? (int)response.StatusCode : null;

                    Debug.Log( $"{response.StatusCode}: {json}" );

                    var responseData = JsonConvert.DeserializeObject<JObject>( json )["data"]["node"]["items"]["nodes"]
                        .Select( n => new {
                            id = n["id"].ToString(),
                            name = n["content"]["title"].ToString(),
                            body = n["content"]["body"].ToString(),
                            column = n["fieldValues"]["nodes"][1]["name"].ToString()
                        } ).ToArray();

                    Debug.Log( string.Join( "\n", responseData.Select( d => $"{d.id} - {d.name} - {d.body} - {d.column}" ) ) );

                    // separate by their column
                    _boardColumns = responseData.Select( d => d.column ).Distinct()
                        .Select( d => new GithubBoardColumn { name = d } ).ToArray();
                    for (var i = 0; i < _boardColumns.Length; i++) {
                        _boardColumns[i].items = responseData.Where( d => d.column == _boardColumns[i].name ).Select(
                            d => new GithubBoardColumn.Item {
                                id = d.id,
                                name = d.name,
                                body = d.body,
                            } ).ToArray();
                    }

                    Debug.Log( string.Join( "\n",
                        _boardColumns.Select( b => $"{b.name}: [{string.Join( ", ", b.items.Select( i => $"{{{i.id}|{i.name} - {i.body}}}" ) )}]" ) ) );

                }
            }
            catch (Exception e) { Debug.LogException( e ); }

            editorWindow.Repaint();
            _busy = false;
        }
    }
}