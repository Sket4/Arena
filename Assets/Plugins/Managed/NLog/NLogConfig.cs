// using System;
// using UnityEngine;
// using NLog;
// using NLog.Config;
// using NLog.Targets;
//
// namespace TzarGames
// {
//     public static class NLogConfig
//     {
//         private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
//         
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//         static private void Configure()
//         {
//             string filename = null;
//             
//             var args = System.Environment.GetCommandLineArgs();
//             for (int i = 0; i < args.Length; i++)
//             {
//                 string arg = args[i].ToLower();
//
//                 if (arg== "-logfile")
//                 {
//                     if (i >= args.Length - 1)
//                     {
//                         Debug.LogError("no argument for -logfile command");
//                         return;
//                     }
//
//                     filename = args[i + 1];
//                 }
//             }
//
//             var path = System.IO.Path.Combine(Application.streamingAssetsPath, "NLog/NLog.config");
//             Debug.LogFormat("Загрузка конфигурации NLog из файла {0}", path);
//             var config = new XmlLoggingConfiguration(path);
//             LogManager.Configuration = config;
//             
//             
//             if (string.IsNullOrEmpty(filename) == false)
//             {
//                 var target = (FileTarget)LogManager.Configuration.FindTargetByName("logfile");
//                 target.FileName = $"${{logDirectory}}/{filename}.txt";
//                 
//                 Debug.Log("Log file name changed to : " + filename);
//             }
//
//             Application.quitting += onAppQuit;
//             
// #if UNITY_EDITOR
//             UnityEditor.EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
//
//             var unityTarget = new UnityConsoleLogTarget();
//             unityTarget.Name = "Unity";
//             LogManager.Configuration.AddTarget("Unity", unityTarget);
//
//             unityTarget.Layout = @"${date:format=HH\:mm\:ss} ${message} ${exception} ${event-context:item=MyValue}";
//             var rule1 = new LoggingRule("*", LogLevel.Debug, unityTarget);
//             LogManager.Configuration.LoggingRules.Add(rule1);
//
// #else
//             Application.logMessageReceived += (condition, trace, type) =>
//             {
//                switch (type)
//                {
//                    case LogType.Error:
//                        log.Error(condition);
//                        break;
//                    case LogType.Assert:
//                        log.Error(condition);
//                        break;
//                    case LogType.Warning:
//                        log.Warn(condition);
//                        break;
//                    case LogType.Log:
//                        log.Info(condition);
//                        break;
//                    case LogType.Exception:
//                        log.Error("{0} trace: {1}", condition, trace);
//                        break;
//                    default:
//                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
//                }
//             };
// #endif
//
//             LogManager.ReconfigExistingLoggers();
//         }
//
// #if UNITY_EDITOR
//         static void EditorApplication_PlayModeStateChanged(UnityEditor.PlayModeStateChange obj)
//         {
//             if(obj == UnityEditor.PlayModeStateChange.ExitingPlayMode)
//             {
//                 LogManager.Flush();
//                 LogManager.Shutdown();
//             }
//         }
// #endif
//
//
//         static void onAppQuit()
//         {
//             LogManager.Shutdown();
//         }
//     }
// }
