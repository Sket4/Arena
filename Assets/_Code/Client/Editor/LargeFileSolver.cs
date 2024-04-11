using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;

namespace TzarGames.Editor
{


    public static class LargeFileSolver
    {
        [System.Serializable]
        class FileList
        {
            public List<string> Paths;
        }

        [InitializeOnLoadMethod]
        [MenuItem("Tzar Games/Утилиты/Обработать большие файлы")]
        static void findAndUnzipFiles()
        {
            var listFilePath = getPathToFile();

            FileList list;

            if (File.Exists(listFilePath) == false)
            {
                list = new FileList();
                list.Paths = new List<string>();
                list.Paths.Add("remove this line " + listFilePath);
                var serialized = JsonUtility.ToJson(list, true);
                File.WriteAllText(listFilePath, serialized);
            }
            else
            {
                var serialized = File.ReadAllText(listFilePath);
                list = JsonUtility.FromJson<FileList>(serialized);
            }

            foreach(var file in list.Paths)
            {
                var filePath = getFullPath(file);
                var zipPath = filePath + ".zip";

                if(File.Exists(zipPath))
                {
                    var metaZipPath = filePath + ".meta.zip";

                    if(File.Exists(metaZipPath) == false)
                    {
                        unzipFile(metaZipPath);
                    }

                    if (File.Exists(filePath) == false)
                    {
                        unzipFile(zipPath);
                    }
                }
                else
                {
                    if(File.Exists(filePath))
                    {
                        zipFile(filePath);
                        var metaPath = filePath + ".meta";
                        if(File.Exists(metaPath))
                        {
                            zipFile(metaPath);
                        }
                    }
                    else
                    {
                        Debug.LogError("Не найден файл " + filePath);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        static string getFullPath(string path)
        {
            return Path.Combine(Application.dataPath, path);
        }

        static void unzipFile(string compressedFilePath)
        {
            Debug.Log("Распаковка большого файла " + compressedFilePath);

            try
            {
                using (var zipFileStream = new FileStream(compressedFilePath, FileMode.Open))
                {
                    var uncompressedFilePath = compressedFilePath.Replace(".zip", "");

                    using (var stream = new FileStream(uncompressedFilePath, FileMode.Create))
                    {
                        using (var unzipStream = new GZipStream(zipFileStream, CompressionMode.Decompress))
                        {
                            unzipStream.CopyTo(stream);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        static void zipFile(string uncompressedFilePath)
        {
            Debug.Log("Архивация большого файла " + uncompressedFilePath);

            using (var uncompressedFileStream = new FileStream(uncompressedFilePath, FileMode.Open))
            {
                var compressedFilePath = uncompressedFilePath + ".zip";

                using (var stream = new FileStream(compressedFilePath, FileMode.Create))
                {
                    using (var zipStream = new GZipStream(stream, CompressionMode.Compress))
                    {
                        uncompressedFileStream.CopyTo(zipStream);
                    }
                }
            }
        }

        static string getPathToFile()
        {
            return Path.Combine(Application.dataPath, "LargeFileList.json");
        }
    }
}
