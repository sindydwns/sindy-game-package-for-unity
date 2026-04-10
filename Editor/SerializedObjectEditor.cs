#if UNITY_EDITOR
using System;
using System.Runtime.ExceptionServices;
using UnityEditor;
using UnityEngine;

namespace Sindy.Macro
{
    public class SerializedObjectEditor
    {
        private readonly string targetName;
        private readonly SerializedObject so;
        private readonly SerializedProperty sp;
        public string Name => targetName;

        public SerializedObjectEditor(UnityEngine.Object target)
        {
            targetName = target.name;
            so = new SerializedObject(target);
        }

        public SerializedObjectEditor(string name, SerializedObject so)
        {
            targetName = name;
            this.so = so;
        }

        public SerializedObjectEditor(string name, SerializedProperty sp)
        {
            targetName = name;
            this.sp = sp;
        }

        public void Apply()
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(so.targetObject);
        }

        public SerializedProperty GetProperty(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception($"SerializedObjectEditor: {Name} path is null or empty");
            }
            var paths = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (paths.Length == 0)
            {
                throw new Exception($"SerializedObjectEditor: {Name} path is null or empty");
            }
            SerializedProperty property = sp ?? so.FindProperty(paths[0]);
            var names = sp == null ? paths[1..] : paths;
            foreach (var name in names)
            {
                if (property == null)
                {
                    throw new Exception($"SerializedObjectEditor: {Name}:{path} is null or empty");
                }
                property = property.FindPropertyRelative(name);
            }
            if (property == null)
            {
                throw new Exception($"SerializedObjectEditor: {Name}:{path} is null or empty");
            }
            return property;
        }

        public SerializedObjectEditor SORef(string path, UnityEngine.Object @ref, bool ignoreNullWarning = false)
        {
            if (@ref == null && !ignoreNullWarning)
            {
                Debug.LogWarning($"SerializedObjectEditor: {Name}:{path} is null");
            }
            try
            {
                GetProperty(path).objectReferenceValue = @ref;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} is null");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOInt(string path, int value)
        {
            try
            {
                GetProperty(path).intValue = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} int value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOLong(string path, long value)
        {
            try
            {
                GetProperty(path).longValue = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} long value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOFloat(string path, float value)
        {
            try
            {
                GetProperty(path).floatValue = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} float value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOStr(string path, string value)
        {
            try
            {
                GetProperty(path).stringValue = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} string value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOBool(string path, bool value)
        {
            try
            {
                GetProperty(path).boolValue = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} bool value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOEnum(string path, int value)
        {
            try
            {
                GetProperty(path).enumValueIndex = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} enum value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOColor(string path, UnityEngine.Color value)
        {
            try
            {
                GetProperty(path).colorValue = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} color value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOVector2(string path, UnityEngine.Vector2 value)
        {
            try
            {
                GetProperty(path).vector2Value = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} vector2 value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOVector3(string path, UnityEngine.Vector3 value)
        {
            try
            {
                GetProperty(path).vector3Value = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} vector3 value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOQuaternion(string path, UnityEngine.Quaternion value)
        {
            try
            {
                GetProperty(path).quaternionValue = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} quaternion value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
        public SerializedObjectEditor SOVector4(string path, UnityEngine.Vector4 value)
        {
            try
            {
                GetProperty(path).vector4Value = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"SerializedObjectEditor: {Name}:{path} vector4 value");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
    }
}

#endif
