using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.IO;
using UnityEditorInternal;

namespace TzarGames.CodeGeneration
{
    public class CodeGeneratorBase : Builder
    {
        public static System.Type[] GetTypes(Assembly assembly)
        {
            var ass = System.Reflection.Assembly.Load(assembly.name);
            return ass.GetTypes();
        }

        protected static string GetGenericMainName(System.Type type)
        {
            var index = type.FullName.IndexOf('`');
            var result = type.FullName.Substring(0, index);
            result = result.Replace("+", ".");
            return result;
        }

        protected static string GetFilteredName(string name)
        {
            var result = name.Replace("+", "");
            result = result.Replace("-", "");
            result = result.Replace(" ", "");
            result = result.Replace("&", "");
            result = result.Replace("<", "");
            result = result.Replace(">", "");
            result = result.Replace("`", "");
            result = result.Replace(".", "");
            return result;
        }

        protected static string GetTypeName(System.Type type)
        {
            return GetTypeName(type.FullName);
        }
        protected static string GetTypeName(string name)
        {
            name = name.Replace('+', '.');
            name = name.Replace("&", "");
            return name;
        }

        protected static string GetGenericTypeName(System.Type type)
        {
            var builder = new System.Text.StringBuilder();
            var splittedName = GetGenericMainName(type);
            builder.Append(splittedName);
            builder.Append("<");
            var genTypes = type.GenericTypeArguments;
            for (int i = 0; i < genTypes.Length; i++)
            {
                System.Type gtype = genTypes[i];

                if (gtype.IsGenericType)
                {
                    builder.Append(GetGenericTypeName(gtype));
                }
                else
                {
                    builder.Append(GetTypeName(gtype.FullName));
                }
                if (i < genTypes.Length - 1)
                {
                    builder.Append(",");
                }
            }
            builder.Append(">");
            return builder.ToString();
        }

        protected List<System.Type> GetInterfaceImplementations(System.Type interfaceType, Assembly assembly)
        {
            if (interfaceType.IsInterface == false)
            {
                throw new System.ArgumentException($"{interfaceType.Name} is not an interface type");
            }

            var ass = System.Reflection.Assembly.Load(assembly.name);
            var types = ass.GetTypes();
            var result = new List<System.Type>();

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces();

                foreach (var implInterface in interfaces)
                {
                    if (interfaceType.IsGenericType)
                    {
                        if (IsSubclassOfRawGeneric(interfaceType, implInterface))
                        {
                            if (result.Contains(type) == false)
                            {
                                result.Add(type);
                            }
                        }
                    }
                    else
                    {
                        if (interfaceType.IsAssignableFrom(implInterface))
                        {
                            if (result.Contains(type) == false)
                            {
                                result.Add(type);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static bool IsSubclassOfRawGeneric(System.Type generic, System.Type toCheck)
        {
            if (generic == toCheck)
            {
                return false;
            }

            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static bool IsAssignableToGenericType(System.Type givenType, System.Type genericType)
        {
            if(givenType.IsGenericType == false)
            {
                return false;
            }

            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            var genericTypeDef = givenType.GetGenericTypeDefinition();
            if (givenType.IsGenericType && genericTypeDef == genericType)
                return true;

            var baseType = givenType.BaseType;

            if (baseType == null) 
                return false;

            return IsAssignableToGenericType(baseType, genericType);
        }
    }

    public abstract class AssemblyCodeGenerator : CodeGeneratorBase
    {
        public abstract string GetUniqueName();
        public void GenerateForAssembly(Assembly assembly)
        {
            Clear();
            OnGenerateForAssembly(assembly);
        }

        public void GenerateCommonCode()
        {
            Clear();
            OnGenerateCommonCode();
        }
        public void Prepare(Assembly assembly)
        {
            Clear();
            OnPrepare(assembly);
        }

        public virtual int GetPriority()
        {
            return 0;
        }

        protected virtual void OnGenerateForAssembly(Assembly assembly)
        {
        }

        protected virtual void OnGenerateCommonCode()
        {
        }

        protected virtual void OnPrepare(Assembly assembly) {}

        public virtual void Reset()
        {
            Clear();
        }

        public virtual void PostGenerate()
        {
        }
    }

    [InitializeOnLoad]
    public static class CodeGeneratorTools
    {
        static List<AssemblyCodeGenerator> generators = new List<AssemblyCodeGenerator>();

        public static void RegisterAssemblyCodeGenerator(AssemblyCodeGenerator generator)
        {
            if (generators.Contains(generator))
            {
                return;
            }
            generators.Add(generator);
        }

        public static T GetGenerator<T>() where T : AssemblyCodeGenerator
        {
            foreach(var gen in generators)
            {
                if(gen is T)
                {
                    return gen as T;
                }
            }
            return null;
        }

        static List<Assembly> GetAssemblies(AssemblyDefinitionAsset[] assets)
        {
            var assembliesArray = CompilationPipeline.GetAssemblies();

            var assemblies = new List<Assembly>(assembliesArray);

            for (int i = assemblies.Count - 1; i >= 0; i--)
            {
                Assembly assembly = assemblies[i];
                bool delete = true;

                foreach(var asset in assets)
                {
                    if(assembly.name == asset.name)
                    {
                        delete = false;
                        break;
                    }
                }

                if(delete)
                {
                    assemblies.Remove(assembly);
                }
            }

            assemblies.Sort((a, b) => a.name.CompareTo(b.name));
            return assemblies;
        }

        public static void GenerateAssemblyCode(AssemblyCodeGenerationSettingsAsset codegenAsset)
        {
            var assemblies = GetAssemblies(codegenAsset.Assemblies.ToArray());
            var prepareOnlyAssemblies = GetAssemblies(codegenAsset.PrepareOnlyAssemblies.ToArray());

            resetGenerators();

            foreach (var gen in generators)
            {
                foreach (var ass in prepareOnlyAssemblies)
                {
                    gen.Prepare(ass);
                }
            }

            foreach (var gen in generators)
            {
                foreach (var ass in assemblies)
                {
                    generateForAssembly(ass, gen, codegenAsset.FullSavePath);
                }
                generateCommonCode(gen, codegenAsset.FullSavePath);
            }

            postGenerate();

            AssetDatabase.Refresh();
        }

        static void generateForAssembly(Assembly ass, AssemblyCodeGenerator gen, string savePath)
        {
            gen.GenerateForAssembly(ass);

            var name = GetModifiedAssemblyName(ass.name);
            var fileSavePath = getFileSavePath(savePath, name, gen.GetUniqueName());

            writeGeneratedText(gen, fileSavePath);
        }

        static void generateCommonCode(AssemblyCodeGenerator gen, string savePath)
        {
            gen.GenerateCommonCode();
            var fileSavePath = getFileSavePath(savePath, "", gen.GetUniqueName());
            writeGeneratedText(gen, fileSavePath);
        }

        static void writeGeneratedText(AssemblyCodeGenerator gen, string fileSavePath)
        {
            var result = gen.ToString();
            if(string.IsNullOrEmpty(result))
            {
                return;
            }
            var dir = Path.GetDirectoryName(fileSavePath);
            if(Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(fileSavePath, result);
        }
        
        

        static void postGenerate()
        {
            foreach (var gen in generators)
            {
                gen.PostGenerate();
            }
        }

        static void resetGenerators()
        {
            foreach (var gen in generators)
            {
                gen.Reset();
            }

            generators.Sort((x, y) =>
            {
                if(x.GetPriority() > y.GetPriority())
                {
                    return -1;
                }
                else if(x.GetPriority() < y.GetPriority())
                {
                    return 1;
                }
                return 0;
            });
        }

        public static void Fix(AssemblyCodeGenerationSettingsAsset codegenAsset)
        {
            var assemblies = GetAssemblies(codegenAsset.Assemblies.ToArray());

            for (int i = assemblies.Count-1; i >= 0; i--)
            {
                var assembly = assemblies[i];
                
                var name = GetModifiedAssemblyName(assembly.name);

                foreach(var gen in generators)
                {
                    var fileSavePath = getFileSavePath(codegenAsset.FullSavePath, name, gen.GetUniqueName());
                    if (File.Exists(fileSavePath))
                    {
                        Debug.Log("Trying to fix " + fileSavePath);
                        File.Delete(fileSavePath);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        static bool isAssemblyValidForGeneration(Assembly ass)
        {
            var name = ass.name.ToLower();
            
            if(name.StartsWith("unity") || name.StartsWith("com.unity"))
            {
                return false;
            }
            return true;
        }

        static Assembly getUnityAssemblyFromPath(string path)
        {
            var assemblies = CompilationPipeline.GetAssemblies();

            foreach (var ass in assemblies)
            {
                if (ass.outputPath == path)
                {
                    return ass;
                }
            }
            return null;
        }

        static string getScriptPathForUnityAssembly(UnityEditor.Compilation.Assembly unityAssembly)
        {
            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(unityAssembly.name);
            path = Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(path))
            {
                path = Application.dataPath;
            }
            return path;
        }

        public static string GetModifiedAssemblyName(string assemblyName)
        {
            var name = Path.GetFileNameWithoutExtension(assemblyName);

            name = name.Replace("-", "");
            name = name.Replace(".", "");
            return name;
        }
        
        static string getFileSavePath(string folderPath, string name, string type)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Path.Combine(folderPath, $"{type}.Generated.cs");    
            }
            return Path.Combine(folderPath, $"{name}.{type}.Generated.cs");
        }
    }

    public class Builder
    {
        System.Text.StringBuilder builder;
        int tabIndex;

        public void Clear()
        {
            builder.Clear();
        }

        public Builder()
        {
            builder = new System.Text.StringBuilder();
        }

        public void Line()
        {
            builder.AppendLine();
        }

        public void Line(string text)
        {
            builder.Append(tab());
            builder.Append(text);
            builder.Append(System.Environment.NewLine);
        }

        public void Line(string text, params object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                builder.Append(tab());
                builder.Append(text);
                builder.Append(System.Environment.NewLine);
            }
            else
            {
                builder.Append(tab());
                builder.Append(string.Format(text, parameters));
                builder.Append(System.Environment.NewLine);
            }
        }

        public void AppendWithIndent(string str)
        {
            builder.Append(tab());
            builder.Append(str);
        }

        public void Append(string str)
        {
            builder.Append(str);
        }
        public void Append(char ch)
        {
            builder.Append(ch);
        }

        public void AppendNewLine()
        {
            builder.Append(System.Environment.NewLine);
        }

        public override string ToString()
        {
            return builder.ToString();
        }

        public BlockDefinition Block(string caption, params object[] parameters)
        {
            return new BlockDefinition { Builder = this, Caption = caption, Parameters = parameters };
        }

        public struct BlockDefinition
        {
            public Builder Builder;
            public string Caption;
            public object[] Parameters;
            public void WithCode(System.Action action, bool addSemicolon = false)
            {
                Builder.Begin(Caption, Parameters);
                action.Invoke();
                Builder.End(addSemicolon);
            }
        }

        public void Begin(string caption, params object[] parameters)
        {
            if(string.IsNullOrEmpty(caption) == false)
            {
                Line(caption, parameters);
            }
            Line("{");
            tabIndex++;
        }

        public void End(bool addSemiColon = false)
        {
            tabIndex--;
            if(addSemiColon)
            {
                Line("};");
            }
            else
            {
                Line("}");
            }
        }

        public static Builder operator ++(Builder a) { a.tabIndex++; return a; }
        public static Builder operator --(Builder a) { a.tabIndex--; return a; }

        string tab()
        {
            switch (tabIndex)
            {
                case 1:
                    return "\t";
                case 2:
                    return "\t\t";
                case 3:
                    return "\t\t\t";
                case 4:
                    return "\t\t\t\t";
                case 5:
                    return "\t\t\t\t\t";
                case 6:
                    return "\t\t\t\t\t\t";
                case 7:
                    return "\t\t\t\t\t\t\t";
                case 8:
                    return "\t\t\t\t\t\t\t\t";
                default:
                    return "";
            }
        }
    }
}
