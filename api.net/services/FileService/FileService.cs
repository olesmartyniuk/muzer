﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;

namespace MuzerAPI.FileService
{
    public class FileService
    {
        public FileService()
        {
            StorageRoot = @"c:\Development\Muzer\FileService";
        }

        public string StorageRoot { get; set; }

        public string Add(Stream stream)
        {
            var fileId = GenerateUniqueId();
            var fullPath = GetFullPath(fileId);

            using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            }

            return fileId;
        }

        public Stream Get(string fileId)
        {
            if (!Exists(fileId))
            {
                return null;
            }

            var fullPath = GetFullPath(fileId);
            var result = new MemoryStream();

            using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.CopyTo(result);
            }

            result.Seek(0, SeekOrigin.Begin);
            return result;
        }

        public bool Exists(string fileId)
        {
            var fullPath = GetFullPath(fileId);

            return File.Exists(fullPath);
        }

        public void Delete(string fileId)
        {
            if (!Exists(fileId))
            {
                throw new FileNotFoundException("File not found", fileId);
            }

            var fullPath = GetFullPath(fileId);

            File.Delete(fullPath);
        }

        private string GetFullPath(string fileId)
        {
            var dir1 = fileId.Substring(0, 4);
            var dir2 = fileId.Substring(4, 4);
            var file = fileId.Substring(8);

            var path = Path.Combine(StorageRoot, dir1, dir2, file);
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return path;
        }

        private string GenerateUniqueId()
        {
            var guid = Guid.NewGuid().ToString();            
            return guid.Replace(@"-", @"");
        }
    }
}
