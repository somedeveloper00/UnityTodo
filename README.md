# UnityTodo (Editor-Only)

![image](https://github.com/somedeveloper00/UnityTodo/assets/79690923/a2d82ecf-96a3-4cd9-8cac-33f183278e65)


## ✨ Features
* Add, remove, and edit tasks in form of List/Item like Trello and other famous Task Management apps
* Copy task lists to json and back to the editor
* Saves to file by default and shares with team members in git
* Light mode and Dark mode support
* Directory-based grouping of task lists (like the *workplace* feature of Trello. useful for when working on multiple projects in a single Unity session)
* Shortcuts to make power users more productive
* Add references to your Unity objects for tasks (saves by asset path)

## 🛠️ Installation
clone the repository into your project's Assets/Plugins folder as a submodule:
```bash
git submodule add --force https://github.com/somedeveloper00/UnityTodo/ Assets/Plugins/UnityTodo
git submodule update Assets/Plugins/UnityTodo
```
or if you don't have git, simply download the zip and extract it into your project's Assets/Plugins folder:
> Linux / MacOS
> ```
> wget https://github.com/somedeveloper00/UnityTodo/archive/refs/heads/main.zip -O UnityTodo.zip
> unzip UnityTodo.zip -d Assets/Plugins
> rm UnityTodo.zip

> Windows
> ```
> mkdir Assets\Plugins
> curl -L -o UnityTodo.zip https://github.com/somedeveloper00/UnityTodo/archive/main.zip
> tar -xf UnityTodo.zip -C Assets/Plugins
> del UnityTodo.zip
> ```


## 💡 Tips
* You can click on a task to enter edit mode.
* You can access task's menu bar with right clicking the task (otherwise clicking the menu button (three dots) will do the same)
* You can save an editing task just by hitting `Enter` after writing the title.
* You can cancel any editing task and remove all selections (and focus) by hitting `Escape`.
