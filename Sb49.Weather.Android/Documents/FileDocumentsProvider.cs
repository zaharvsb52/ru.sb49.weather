using System;
using System.Collections.Generic;
using System.Linq;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Webkit;
using Java.IO;
using Sb49.Common.Droid;

namespace Sb49.Weather.Droid.Documents
{
    public class FileDocumentsProvider : DocumentsProvider
    {
        private const string Root = "root";
        private const int MaxSearchResults = 24;

        private static readonly string ExternalStorageDirectoryPath =
            Android.OS.Environment.ExternalStorageDirectory.Path;

        private static readonly string[] SystemDirectoryPaths =
        {
            Android.OS.Environment.RootDirectory.Path,
            Android.OS.Environment.DataDirectory.Path,
            Android.OS.Environment.DownloadCacheDirectory.Path,
            System.IO.Path.DirectorySeparatorChar + "config",
            System.IO.Path.DirectorySeparatorChar + "d",
            System.IO.Path.DirectorySeparatorChar + "dev",
            System.IO.Path.DirectorySeparatorChar + "etc",
            System.IO.Path.DirectorySeparatorChar + "mnt",
            System.IO.Path.DirectorySeparatorChar + "persist",
            System.IO.Path.DirectorySeparatorChar + "proc",
            System.IO.Path.DirectorySeparatorChar + "root",
            System.IO.Path.DirectorySeparatorChar + "sbin",
            System.IO.Path.DirectorySeparatorChar + "sys"
        };

        private File _baseFolder;

        public override bool OnCreate()
        {
            _baseFolder = new File(System.IO.Path.DirectorySeparatorChar.ToString());
            return true;
        }

        public override ICursor QueryRoots(string[] projection)
        {
            // Create a cursor with either the requested fields, or the default projection.  This
            // cursor is returned to the Android system picker UI and used to display all roots from
            // this provider.
            var result = new MatrixCursor(ResolveRootProjection(projection));

            // It's possible to have multiple roots (e.g. for multiple accounts in the same app) -
            // just add multiple cursor rows. Construct one row for a root.
            var row = result.NewRow();
            row.Add(DocumentsContract.Root.ColumnRootId, Root);

            // Если не задано, то показывается размер. 
            //row.Add(DocumentsContract.Root.ColumnSummary, Context.GetString(Resource.String.root_summary));

            // FLAG_SUPPORTS_CREATE means at least one directory under the root supports creating
            // documents. FLAG_SUPPORTS_RECENTS means your application's most recently used
            // documents will show up in the "Recents" category.  FLAG_SUPPORTS_SEARCH allows users
            // to search all documents the application shares.
            row.Add(DocumentsContract.Root.ColumnFlags, (int) DocumentRootFlags.SupportsCreate |
                                                        (int) DocumentRootFlags.SupportsRecents |
                                                        (int) DocumentRootFlags.SupportsSearch);

            // COLUMN_TITLE is the root title (e.g. what will be displayed to identify your provider).
            row.Add(DocumentsContract.Root.ColumnTitle, System.IO.Path.DirectorySeparatorChar.ToString());

            // This document id must be unique within this provider and consistent across time.  The
            // system picker UI may save it and refer to it later.
            row.Add(DocumentsContract.Root.ColumnDocumentId, GetDocIdForFile(_baseFolder));

            // The child MIME types are used to filter the roots and only present to the user roots
            // that contain the desired type somewhere in their file hierarchy.
            //row.Add(DocumentsContract.Root.ColumnMimeTypes,
            //    "image/*\ntext/*\napplication/vnd.openxmlformats-officedocument.wordprocessingml.document\n");

            row.Add(DocumentsContract.Root.ColumnAvailableBytes, _baseFolder?.FreeSpace ?? 0);
            row.Add (DocumentsContract.Root.ColumnIcon, Resource.Mipmap.ic_launcher);

            return result;
        }

        public override ICursor QueryDocument(string documentId, string[] projection)
        {
            var result = new MatrixCursor(ResolveDocumentProjection(projection));
            IncludeFile(result, documentId, null, null);
            return result;
        }

        public override ICursor QueryChildDocuments(string parentDocumentId, string[] projection, string sortOrder)
        {
            var result = new MatrixCursor(ResolveDocumentProjection(projection));
            var parent = GetFileForDocumentId(parentDocumentId);

            var files = parent.ListFiles();
            if (files == null || files.Length == 0)
                return result;

            var parentDocId = GetDocIdForFile(parent);

            foreach (var file in files)
            {
                IncludeFile(result, null, file, parentDocId);
            }

            return result;
        }

        public override ICursor QuerySearchDocuments(string rootId, string query, string[] projection)
        {
            // Create a cursor with the requested projection, or the default projection.
            var result = new MatrixCursor(ResolveDocumentProjection(projection));
            var parent = GetFileForDocumentId(rootId);

            if (query.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                using (var file = new File(query))
                {
                    var parentDocId = GetDocIdForFile(file.ParentFile);
                    return QueryChildDocuments(parentDocId, projection, (string) null);
                }
            }

            var queryLower = query.ToLower();

            // This example implementation searches file names for the query and doesn't rank search
            // results, so we can stop as soon as we find a sufficient number of matches.  Other
            // implementations might use other data about files, rather than the file name, to
            // produce a match; it might also require a network call to query a remote server.

            // Iterate through all files in the file structure under the root until we reach the
            // desired number of matches.
            // Start by adding the parent to the list of files to be processed
            var pending = new List<File> {parent};

            // Do while we still have unexamined files, and fewer than the max search results
            while (pending.Any() && result.Count < MaxSearchResults)
            {
                // Take a file from the list of unprocessed files
                var file = pending[0];
                pending.RemoveAt(0);
                if (file.IsDirectory)
                {
                    if(!file.CanRead() || SystemDirectoryPaths.Contains(file.Path))
                        continue;

                    // If it's a directory, add all its children to the unprocessed list
                    var files = file.ListFiles();
                    if (files?.Length > 0)
                        pending.AddRange(files);
                }
                else
                {
                    // If it's a file and it matches, add it to the result cursor.
                    if (file.Name.ToLower().Contains(queryLower))
                        IncludeFile(result, null, file, GetDocIdForFile(file.ParentFile));
                }
            }
            return result;
        }

        public override ParcelFileDescriptor OpenDocument(string documentId, string mode, CancellationSignal signal)
        {
            throw new NotImplementedException();
        }

        public override string CreateDocument(string parentDocumentId, string mimeType, string displayName)
        {
            var fileUtil = new FileUtil();
            var displayNameValid = fileUtil.BuildValidFatFilename(displayName);

            var parent = GetFileForDocumentId(parentDocumentId);
            if (!parent.IsDirectory)
                throw new Java.Lang.IllegalArgumentException("Parent document isn't a directory.");

            using (var file = fileUtil.BuildUniqueFile(parent, mimeType, displayNameValid))
            {
                if (mimeType == DocumentsContract.Document.MimeTypeDir)
                {
                    if (!file.Mkdir())
                        throw new Java.Lang.IllegalStateException("Failed to mkdir '" + file + "'.");
                }
                else
                {
                    try
                    {
                        if (!file.CreateNewFile())
                            throw new Java.Lang.IllegalStateException("Failed to touch '" + file + "'.");
                    }
                    catch (IOException ex)
                    {
                        throw new Java.Lang.IllegalStateException("Failed to touch '" + file + "': " + ex);
                    }
                }

                return GetDocIdForFile(file);
            }
        }

        public string GetParent(string documentId)
        {
            using (var file = GetFileForDocumentId(documentId))
            {
                using (var parent = file.ParentFile)
                {
                    return parent == null ? null : GetDocIdForFile(parent);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            _baseFolder?.Dispose();
            _baseFolder = null;
            
            base.Dispose(disposing);
        }

        private int GetFlags(bool canWrite, bool isDirectory)
        {
            var flags = 0;
            if (canWrite)
            {
                // If the file is writable set FLAG_SUPPORTS_DELETE and FLAG_SUPPORTS_RENAME and Document.FLAG_SUPPORTS_MOVE
                flags |= (int)DocumentContractFlags.SupportsDelete |
                         (int)DocumentContractFlags.SupportsRename | (int)DocumentContractFlags.SupportsMove;

                // Add FLAG_DIR_SUPPORTS_CREATE or FLAG_SUPPORTS_WRITE.
                flags |= (int)(isDirectory
                    ? DocumentContractFlags.DirSupportsCreate
                    : DocumentContractFlags.SupportsWrite);
            }

            return flags;
        }

        private void IncludeFile(MatrixCursor result, string docId, File file, string parentDocumentId)
        {
            if (docId == null)
                docId = GetDocIdForFile(file);
            else
                file = GetFileForDocumentId(docId);

            var isDirectory = file.IsDirectory;
            var flags = GetFlags(file.CanWrite(), isDirectory);
            
            //if (isDirectory)
            //{
            //    // Request the folder to lay out as a grid rather than a list. This also allows a larger
            //    // thumbnail to be displayed for each image (FLAG_DIR_PREFERS_GRID).
            //    flags |= (int) DocumentContractFlags.DirPrefersGrid;
            //}

            var displayName = file.Name;
            var mimeType = GetTypeForFile(file);

            if (mimeType.StartsWith("image/"))
            {
                // Allow the image to be represented by a thumbnail rather than an icon
                flags |= (int) DocumentContractFlags.SupportsThumbnail;
            }

            var row = result.NewRow();
            row.Add(DocumentsContract.Document.ColumnDocumentId, docId);
            row.Add(DocumentsContract.Document.ColumnDisplayName, displayName);
            row.Add(DocumentsContract.Document.ColumnMimeType, mimeType);
            row.Add(DocumentsContract.Document.ColumnLastModified, file.LastModified());
            row.Add(DocumentsContract.Document.ColumnFlags, flags);

            var path = file.Path;
            long fileLength;
            if (isDirectory)
            {
                fileLength = file.TotalSpace;
                row.Add(DocumentsContract.Root.ColumnAvailableBytes, file.FreeSpace);

                if (path == ExternalStorageDirectoryPath)
                {
                    row.Add(Sb49DocumentsContract.Document.ColumnExtraFlags,
                        (int) DocumentContractExtraFlags.ExternalStorageDirectory);
                }
            }
            else
            {
                fileLength = file.Length();
            }

            row.Add(DocumentsContract.Document.ColumnSize, fileLength);

            if(!string.IsNullOrEmpty(parentDocumentId))
                row.Add(Sb49DocumentsContract.Document.ColumnParentDocumentId, parentDocumentId);
        }

        public File GetFileForDocumentId(string documentId)
        {
            if (documentId == Root)
                return _baseFolder;

            var splitIndex = documentId.IndexOf(':', 1);
            if (splitIndex < 0)
                throw new FileNotFoundException(string.Format("Missing root for '{0}'.", documentId));

            var path = documentId.Substring(splitIndex + 1);
            var target = new File(_baseFolder, path);
            if (!target.Exists())
                throw new FileNotFoundException(string.Format("Missing file for '{0}' at '{1}'.", documentId, target));

            return target;
        }

        public string GetDocIdForFile(File file)
        {
            var path = file.AbsolutePath;

            // Start at first char of path under root
            var rootPath = _baseFolder.Path;
            if (rootPath == path)
            {
                path = string.Empty;
            }
            else if (rootPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
            {
                path = path.Substring(rootPath.Length);
            }
            else
            {
                path = path.Substring(rootPath.Length + 1);
            }

            return string.Format("{0}:{1}", Root, path);
        }

        private static string GetTypeForFile(File file)
        {
            if (file.IsDirectory)
                return DocumentsContract.Document.MimeTypeDir;

            return GetTypeForName(file.Name);
        }

        private static string GetTypeForName(string name)
        {
            var lastDot = name.LastIndexOf('.');
            if (lastDot >= 0)
            {
                var extension = name.Substring(lastDot + 1);
                var mime = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
                if (mime != null)
                    return mime;
            }

            return "application/octet-stream";
        }

        private static string[] ResolveRootProjection(string[] projection)
        {
            return projection ?? Sb49DocumentsContract.Root.DefaultRootProjection;
        }

        private static string[] ResolveDocumentProjection(string[] projection)
        {
            return projection ?? Sb49DocumentsContract.Document.DefaultDocumentProjection;
        }
    }
}