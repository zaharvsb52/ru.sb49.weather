using System;
using System.Text;
using System.Threading;
using Android.Content;
using Android.Provider;
using Android.Webkit;

namespace Sb49.Common.Droid
{
    public sealed class FileUtil
    {
        public string BuildValidFatFilename(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "." || name == "..")
                return "(invalid)";

            var length = name.Length;
            var res = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                var c = name[i];
                res.Append(IsValidFatFilenameChar(c) ? c : '_');
            }

            // Even though vfat allows 255 UCS-2 chars, we might eventually write to
            // ext4 through a FUSE layer, so use that limit.
            TrimFilename(res, 255);
            return res.ToString();
        }

        public Java.IO.File BuildUniqueFile(Java.IO.File parent, string mimeType, string displayName)
        {
            var parts = SplitFileName(mimeType, displayName);
            return BuildUniqueFileWithExtension(parent, parts[0], parts[1]);
        }

        public string TrimFilename(string str, int maxBytes)
        {
            var res = new StringBuilder(str);
            TrimFilename(res, maxBytes);
            return res.ToString();
        }

        public void Copy(Context context, string fromFileName, Android.Net.Uri toUri, int bufferSize = 1024, CancellationToken? token = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrEmpty(fromFileName))
                throw new ArgumentNullException(nameof(fromFileName));

            if (toUri == null)
                throw new ArgumentNullException(nameof(toUri));

            if (!System.IO.File.Exists(fromFileName))
                throw new System.IO.FileNotFoundException(fromFileName);

            if(token == null)
                token = CancellationToken.None;

            var buffer = new byte[bufferSize];

            using (var readStream = System.IO.File.OpenRead(fromFileName))
            {
                using (var openFileDescriptor = context.ContentResolver.OpenFileDescriptor(toUri, "w"))
                {
                    using (var fileOutputStream = new Java.IO.FileOutputStream(openFileDescriptor.FileDescriptor))
                    {
                        using (var bufferedStream = new System.IO.BufferedStream(readStream))
                        {
                            int count;
                            while ((count = bufferedStream.Read(buffer, 0, bufferSize)) > 0)
                            {
                                if (token.Value.IsCancellationRequested)
                                    return;

                                fileOutputStream.Write(buffer, 0, count);
                            }

                            fileOutputStream.Close();
                            openFileDescriptor.Close();
                        }
                    }
                }
            }
        }

        private Java.IO.File BuildUniqueFileWithExtension(Java.IO.File parent, string name, string ext)
        {
            var file = BuildFile(parent, name, ext);

            // If conflicting file, try adding counter suffix
            var n = 0;
            while (file.Exists())
            {
                if (n++ >= 32)
                    throw new Java.IO.FileNotFoundException("Failed to create unique file.");
                file = BuildFile(parent, name + " (" + n + ")", ext);
            }

            return file;
        }

        private Java.IO.File BuildFile(Java.IO.File parent, string name, string ext)
        {
            if (string.IsNullOrEmpty(ext))
                return new Java.IO.File(parent, name);

            return new Java.IO.File(parent, name + "." + ext);
        }

        private bool IsValidFatFilenameChar(char c)
        {
            if (0x00 <= c && c <= 0x1f)
                return false;

            switch (c)
            {
                case '"':
                case '*':
                case '/':
                case ':':
                case '<':
                case '>':
                case '?':
                case '\\':
                case '|':
                //case 0x7F:
                    return false;
                default:
                    return Convert.ToByte(c) != 0x7F;
            }
        }

        private void TrimFilename(StringBuilder res, int maxBytes)
        {
            // byte[] raw = res.toString().getBytes(StandardCharsets.UTF_8);
            var raw = Encoding.UTF8.GetBytes(res.ToString()); 
            if (raw.Length > maxBytes)
            {
                maxBytes -= 3;
                while (raw.Length > maxBytes)
                {
                    // res.deleteCharAt(res.length() / 2);
                    res.Remove(res.Length / 2, 1);
                    // raw = res.toString().getBytes(StandardCharsets.UTF_8);
                    raw = Encoding.UTF8.GetBytes(res.ToString());
                }
                // res.insert(res.length() / 2, "...");
                res.Insert(res.Length / 2, "...");
            }
        }

        /// <summary>
        /// Splits file name into base name and extension. 
        /// If the display name doesn't have an extension that matches the requested MIME type, 
        /// the extension is regarded as a part of filename and default extension for that MIME type is appended.
        /// </summary>
        public string[] SplitFileName(string mimeType, string displayName)
        {
            string name;
            string ext;
            string mimeTypeFromExt = null;

            if (mimeType == DocumentsContract.Document.MimeTypeDir)
            {
                name = displayName;
                ext = null;
            }
            else
            {
                // Extract requested extension from display name
                var lastDot = displayName.LastIndexOf('.');
                if (lastDot >= 0)
                {
                    name = displayName.Substring(0, lastDot);
                    ext = displayName.Substring(lastDot + 1);
                    mimeTypeFromExt = MimeTypeMap.Singleton.GetMimeTypeFromExtension(ext.ToLower());
                }
                else
                {
                    name = displayName;
                    ext = null;
                }

                if (mimeTypeFromExt == null)
                    mimeTypeFromExt = "application/octet-stream";

                var extFromMimeType = MimeTypeMap.Singleton.GetExtensionFromMimeType(mimeType);
                if (mimeType != mimeTypeFromExt && ext != extFromMimeType)
                {
                    // No match; insist that create file matches requested MIME
                    name = displayName;
                    ext = extFromMimeType;
                }
            }

            return new[] {name, ext ?? string.Empty, mimeTypeFromExt};
        }
    }
}