# UnityTodo (Editor-Only)

![image](https://github.com/somedeveloper00/UnityTodo/assets/79690923/52630412-645f-49d7-966e-f843b60fdbe9)


## Features
* Add, remove, and edit tasks in form of List/Item like Trello and other famous Task Management apps
* Copy task lists to json and back to the editor
* Saves to file by default and shares with team members in git

## Installation
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
