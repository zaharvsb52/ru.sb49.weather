using Android.Provider;

namespace Sb49.Weather.Droid.Documents
{
    public static class Sb49DocumentsContract
    {
        public static class Root
        {
            public static readonly string[] DefaultRootProjection =
            {
                DocumentsContract.Root.ColumnRootId,
                DocumentsContract.Root.ColumnMimeTypes,
                DocumentsContract.Root.ColumnFlags,
                DocumentsContract.Root.ColumnIcon,
                DocumentsContract.Root.ColumnTitle,
                DocumentsContract.Root.ColumnSummary,
                DocumentsContract.Root.ColumnDocumentId,
                DocumentsContract.Root.ColumnAvailableBytes
            };
        }

        public static class Document
        {
            public const string ColumnParentDocumentId = "parent_document_id";
            public const string ColumnExtraFlags = "extar_flags";

            public static readonly string[] DefaultDocumentProjection =
            {
                DocumentsContract.Document.ColumnDocumentId,
                DocumentsContract.Document.ColumnMimeType,
                DocumentsContract.Document.ColumnDisplayName,
                DocumentsContract.Document.ColumnLastModified,
                DocumentsContract.Document.ColumnFlags,
                DocumentsContract.Document.ColumnSize
            };
        }
    }
}