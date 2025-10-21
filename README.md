# Game Off 2025
by Crunchy Time Studios  
![Unity 6 Compatible](https://img.shields.io/badge/Unity-6000.0.59f2%20LTS-blue?logo=unity)  

## Naming Conventions
#### Prevent whitespace in file names and directories. 
- Replace whitespaces with an underscore '_' or hypen '-' character instead. This helps command line navigation.  
- bad e.g.: "my cool shader.shader", "Assets/Prefabs/cool plant pot model.fbx"  
- good e.g.: "my_cool_shader.shader", "Assets/Prefabs/cool-plant-pot-model.fbx"

## Prefab Guidelines
Because scene files in the Unity engine are often worked on by two or more people simultaneously, merge conflicts often occur with these files in the YAML format. While a change like adding a component or changing a serialized field parameter merge conflict can be easy to resolve, designers are often working with many game objects at a time; it would be too obnoxious to commit and push to Git after tweaking any object position, for example.  

To prevent these merge conflicts, we can use Unity's [prefab workflow](https://docs.unity3d.com/6000.0/Documentation/Manual/Prefabs.html) to work in parallel on the same scene. To begin, open or create a scene you would like to work in. Then, create a new GameObject in the Hierarchy Window and [turn it into a prefab](https://docs.unity3d.com/6000.0/Documentation/Manual/CreatingPrefabs.html#create-a-prefab-asset) by dragging it into the Project Window. Now the GameObject should appear blue in the Hierarchy Window - this is a prefab instance. Modifying this prefab instance will only affect this instance with overrides. Changes to this instance will not affect other instances and will dirty the scene file - we want to avoid these changes to the scene file beyond the initial prefab creation.  

Now open the prefab asset file directly, you'll be taken to an 'in-context' mode called a prefab stage where you can begin making changes within the scene. From here you can add, remove, and change GameObjects from the Hierarchy Window without worrying about changes affecting the scene file. These changes affect all prefab instances and do not mark the scene file as dirty, so you can commit and push these changes to Git without worrying about merge conflicts.  

## Art Guidelines
