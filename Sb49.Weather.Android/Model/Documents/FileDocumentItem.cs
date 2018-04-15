using System;
using System.Collections.Generic;
using Android.Content;
using Android.Database;
using Android.Provider;
using Newtonsoft.Json;
using Sb49.Common;
using Sb49.Weather.Droid.Documents;

namespace Sb49.Weather.Droid.Model
{
    public sealed class FileDocumentItem
    {
        #region ctor

        static FileDocumentItem()
        {
            DocumentProjection = new List<string>(Sb49DocumentsContract.Document.DefaultDocumentProjection)
            {
                DocumentsContract.Root.ColumnAvailableBytes,
                Sb49DocumentsContract.Document.ColumnParentDocumentId,
                Sb49DocumentsContract.Document.ColumnExtraFlags
            }.ToArray();
        }

        public FileDocumentItem()
        {
        }

        public FileDocumentItem(Context context, ICursor cursor, bool useRoor = false)
        {
            if(context == null)
                throw new ArgumentNullException(nameof(context));
            if (cursor == null)
                throw new ArgumentNullException(nameof(cursor));

            int columnIndex;

            if (useRoor)
            {
                columnIndex = cursor.GetColumnIndex(DocumentsContract.Root.ColumnDocumentId);
                if (columnIndex >= 0)
                    DocumentId = cursor.GetString(columnIndex);

                columnIndex = cursor.GetColumnIndex(DocumentsContract.Root.ColumnTitle);
                if (columnIndex >= 0)
                    DisplayName = cursor.GetString(columnIndex);

                columnIndex = cursor.GetColumnIndex(DocumentsContract.Root.ColumnFlags);
                if (columnIndex >= 0)
                    Flags = cursor.GetInt(columnIndex);

                columnIndex = cursor.GetColumnIndex(DocumentsContract.Root.ColumnIcon);
                if (columnIndex >= 0)
                    IconResourceId = cursor.GetInt(columnIndex);

                columnIndex = cursor.GetColumnIndex(DocumentsContract.Root.ColumnAvailableBytes);
                if (columnIndex >= 0)
                    AvailableBytes = cursor.GetLong(columnIndex);
                return;
            }

            columnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDocumentId);
            if (columnIndex >= 0)
                DocumentId = cursor.GetString(columnIndex);

            columnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
            if (columnIndex >= 0)
                DisplayName = cursor.GetString(columnIndex);

            columnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnSize);
            if (columnIndex >= 0)
                Size = cursor.GetLong(columnIndex);

            columnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnMimeType);
            if (columnIndex >= 0)
                MimeType = cursor.GetString(columnIndex);

            columnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnFlags);
            if (columnIndex >= 0)
                Flags = cursor.GetInt(columnIndex);

            columnIndex = cursor.GetColumnIndex(Sb49DocumentsContract.Document.ColumnExtraFlags);
            if (columnIndex >= 0)
                ExtraFlags = cursor.GetInt(columnIndex);

            columnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnLastModified);
            if (columnIndex >= 0)
            {
                var lastModified = cursor.GetLong(columnIndex);
                if (lastModified >= 0)
                    LastModified = new DateTime(1970, 1, 1).AddMilliseconds(lastModified);
            }

            columnIndex = cursor.GetColumnIndex(DocumentsContract.Root.ColumnAvailableBytes);
            if (columnIndex >= 0)
                AvailableBytes = cursor.GetLong(columnIndex);

            if (IsFolder)
            {
                //TintColor = ContextCompat.GetColor(context, Resource.Color.vectorDrawableFillColorTint);
                IconResourceId = Util.HasFlags(ExtraFlags, (int) DocumentContractExtraFlags.ExternalStorageDirectory)
                    ? Resource.Drawable.ic_sd_card
                    : Resource.Drawable.ic_folder;
            }
            else
            {
                IconResourceId = Resource.Drawable.ic_file;
            }
        }

        #endregion ctor

        public static string[] DocumentProjection { get; }

        public string DocumentId { get; set; }
        public string DisplayName { get; set; }
        public long? Size { get; set; }
        public long? AvailableBytes { get; set; }
        public string MimeType { get; set; }
        public DateTime? LastModified { get; set; }
        public int Flags { get; set; }
        public int ExtraFlags { get; set; }
        public int? IconResourceId { get; set; }
        public int? TintColor { get; set; }

        [JsonIgnore]
        public bool IsFolder => MimeType == DocumentsContract.Document.MimeTypeDir;
    }
}