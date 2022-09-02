using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace ExtensionMethods{
    public static class StringExtensions{
       public static List<int> AllIndexesOf(this string str, string value) {
        if (String.IsNullOrEmpty(value))
            throw new ArgumentException("the string to find may not be empty", "value");
        List<int> indexes = new List<int>();
        for (int index = 0;; index += value.Length) {
            index = str.IndexOf(value, index);
            if (index == -1)
                return indexes;
            indexes.Add(index);
            }
        }
    }

    public static class FileExtensions{

          public static List<FileInfo> GetFilesByExtension(string path, params string[] extensions){
            List<FileInfo> list = new List<FileInfo>();
            foreach (string ext in extensions)
                list.AddRange(new DirectoryInfo(path).GetFiles("*" + ext).Where(p =>
                    p.Extension.Equals(ext,StringComparison.CurrentCultureIgnoreCase))
                    .ToArray());
                return list;
        }
    }
}
