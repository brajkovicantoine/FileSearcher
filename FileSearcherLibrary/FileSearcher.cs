using System.Text.RegularExpressions;
using System.Diagnostics;
using Xceed.Words.NET;

namespace FileSearcherLibrary
{
    public interface IFileSearcher
    {
        IEnumerable<string> patterns { get; }

        IEnumerable<string> EnumerateFiles(string path);

        FileModel SearchInFile(string file, IEnumerable<string> keywords);
    }

    public class DocxFileSearcher : IFileSearcher
    {
        private static EnumerationOptions options = new EnumerationOptions()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        public IEnumerable<string> patterns => new List<string>() { _pattern };

        private string _pattern => "*.docx";

        public IEnumerable<string> EnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path, _pattern, options);
        }

        public FileModel SearchInFile(string file, IEnumerable<string> keywords)
        {
            var fileModel = new FileModel();
            fileModel.Filename = Path.GetFileName(file);
            fileModel.Path = Path.GetFullPath(file);
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                using (DocX document = DocX.Load(fileModel.Path))
                {
                    foreach (var keyword in keywords)
                    {
                        //Finds all occurrences of a misspelled word and replaces with properly spelled word.
                        var hits = document.FindAll(keyword, RegexOptions.IgnoreCase).Count;
                        fileModel.KeyHits.Add(keyword, hits);
                    }
                }
                fileModel.Completed = true;
            }
            catch (Exception ex)
            {
                fileModel.Exception = ex;
                fileModel.Completed = false;
            }
            finally
            {
                sw.Stop();
                fileModel.SearchTime = sw.Elapsed;
            }

            return fileModel;
        }
    }
}
