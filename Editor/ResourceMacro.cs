#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Sindy.Scriptables;
using Sindy.Inven;

namespace Sindy.Macro
{
    public class ResourceMacro : EditorWindow
    {
        protected static void SetListValues(UnityEngine.Object ob, string path, IEnumerable<UnityEngine.Object> items)
        {
            var so = new SerializedObject(ob);
            var prop = so.FindProperty(path);
            prop.ClearArray();
            prop.arraySize = 0;
            for (var i = 0; i < items.Count(); i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items.ElementAt(i);
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ob);
        }

        protected static void SetScriptableListValue(ScriptableListVariable list, IEnumerable<ScriptableObject> items)
        {
            if (list == null || items == null)
            {
                throw new System.Exception("List or items is null");
            }

            SetListValues(list, "Value", items);
        }

        protected static void AddScriptableListValue(ScriptableListVariable list, IEnumerable<ScriptableObject> items)
        {
            if (list == null || items == null)
            {
                throw new System.Exception("List or items is null");
            }

            var so = new SerializedObject(list);
            var prop = so.FindProperty("Value");
            foreach (var item in items)
            {
                prop.arraySize++;
                prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = item;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(list);
        }

        protected static void SetIter<T>(SerializedProperty prop, IEnumerable<T> items, Action<SerializedProperty, T> action)
        {
            if (prop == null)
            {
                Debug.LogError($"Property {prop.name} not found");
                return;
            }
            prop.ClearArray();
            prop.arraySize = items.Count();
            for (int i = 0; i < prop.arraySize; i++)
            {
                var item = items.ElementAt(i);
                action(prop.GetArrayElementAtIndex(i), item);
            }
        }

        protected static ScriptableListVariable GetGlobalList(string name)
        {
            return GetAssets<ScriptableListVariable>()
                .FirstOrDefault(x => x.name == name);
        }

        protected static GameObject GetGameObject(string name)
        {
            return GetAssets<GameObject>()
                .FirstOrDefault(x => x.name == name);
        }

        protected static List<T> GetAssets<T>(string namePattern = "") where T : UnityEngine.Object
        {
            var pattern = string.IsNullOrEmpty(namePattern) ? "" : $"{namePattern} ";
            string[] guids = AssetDatabase.FindAssets($"{pattern} t:{typeof(T).Name}");
            List<T> items = new();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        protected static List<UnityEngine.Object> GetAssets(string namePattern)
        {
            string[] guids = AssetDatabase.FindAssets($"{namePattern}");
            List<UnityEngine.Object> items = new();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        protected static void ValidDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        protected static T GetEntity<T>(string name, string dir) where T : Entity
        {
            var entities = GetAssets<T>();
            var entity = entities.FirstOrDefault(x => x.name == name);
            if (entity == null)
            {
                var ids = GetAssets<Entity>();
                var nextId = ids.Count == 0 ? 1 : ids.Max(x => x.id) + 1;
                entity = CreateResource<T>($"{Path.Join(dir, name)}.asset");
                var so = new SerializedObject(entity);
                so.FindProperty("id").intValue = nextId;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(entity);
            }
            return entity;
        }

        protected static T GetAsset<T>(string name, string dir) where T : ScriptableObject
        {
            var assets = GetAssets<T>();
            var asset = assets.FirstOrDefault(x => x.name == name);
            if (asset == null)
            {
                asset = CreateResource<T>($"{Path.Join(dir, name)}.asset");
            }
            return asset;
        }

        protected static T CreateResource<T>(string path) where T : ScriptableObject
        {
            ValidDirectory(Path.GetDirectoryName(path));
            var filename = Path.GetFileNameWithoutExtension(path);
            var asset = CreateInstance<T>();
            asset.name = filename;
            AssetDatabase.CreateAsset(asset, path);
            Save();
            Debug.Log($"Created {typeof(T).Name}: {path}");
            return asset;
        }

        protected static List<T> GetAssetsFromFile<T>(IEnumerable<string> fileNames) where T : UnityEngine.Object
        {
            return fileNames.SelectMany(x => AssetDatabase.LoadAllAssetsAtPath(x))
                .OfType<T>()
                .ToList();
        }

        protected static List<T> GetAssetsFromDirectory<T>(string[] path, string pattern, bool isRecursive = false) where T : UnityEngine.Object
        {
            var option = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = path.SelectMany(x => Directory.GetFiles(x, pattern, option));
            return GetAssetsFromFile<T>(files);
        }

        protected static Dictionary<string, T> GetAssetsInAseprite<T>(string[] directories, bool isRecursive = false) where T : UnityEngine.Object
        {
            var option = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = directories.SelectMany(x => Directory.GetFiles(x, "*.aseprite", option));
            var assets = new Dictionary<string, T>();
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var assetFiles = AssetDatabase.LoadAllAssetsAtPath(file).OfType<T>();
                foreach (var asset in assetFiles)
                {
                    assets[$"{name}.{asset.name}"] = asset;
                }
            }
            return assets;
        }

        private static bool editing = false;
        protected static void StartEdit()
        {
            if (editing)
            {
                return;
            }
            AssetDatabase.StartAssetEditing();
            editing = true;
        }

        protected static void Save()
        {
            if (!editing)
            {
                return;
            }
            editing = false;
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            StartEdit();
        }

        protected static void EndEdit()
        {
            if (!editing)
            {
                return;
            }
            editing = false;
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        protected static void Edit(Action action)
        {
            if (editing)
            {
                action();
                return;
            }
            StartEdit();
            try
            {
                action();
            }
            finally
            {
                EndEdit();
            }
        }

        protected static void Edit(string log, Action action)
        {
            if (editing)
            {
                action();
                return;
            }
            Debug.Log($"Start: {log}");
            StartEdit();
            try
            {
                action();
            }
            finally
            {
                EndEdit();
                Debug.Log($"End: {log}");
            }
        }
    }

#endif
}
