using Microsoft.Extensions.Logging;

namespace FileSearcherLibrary
{
    public interface IFileSearcherService
    {
        Task<IEnumerable<FileModel>> Search(string path, IEnumerable<string> keywords);

        Task<int> Count(string path);
    }

    public class FileSearcherService : IFileSearcherService
    {
        private readonly IEnumerable<IFileSearcher> _fileSearchers;
        private readonly ILogger<FileSearcherService> _logger;

        public FileSearcherService(ILogger<FileSearcherService> logger, IEnumerable<IFileSearcher> fileSearchers)
        {
            _logger = logger;
            _fileSearchers = fileSearchers;
        }

        public Task<int> Count(string path)
        {
            try
            {
                var dict = 0;
                foreach(var fileSearcher in _fileSearchers)
                {
                    dict += fileSearcher.EnumerateFiles(path).Count();
                }
                return Task.FromResult(dict);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "");
                throw;
            }
        }

        private List<FileInfo> Get(string path)
        {
            try
            {
                var dict = new List<FileInfo>();
                foreach(var fileSearcher in _fileSearchers)
                {
                    dict.AddRange(fileSearcher.EnumerateFiles(path)
                            .Select(x =>  new FileInfo( x, fileSearcher )) 
                        );
                }
                return dict;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "");
                throw;
            }
        }

        public Task<IEnumerable<FileModel>> Search(string path, IEnumerable<string> keywords)
        {
            var filedic = Get(path);

            var tasks = filedic.Select(x => Task<FileModel>.Factory.StartNew(() => 
                {
                    try
                    {
                        return x.FileSearcher!.SearchInFile(x.Path, keywords);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "");
                        throw;
                    }
                }
            ));

            Task.WaitAll(tasks.ToArray());

            return Task.FromResult(tasks.Select(x => x.Result));
        }

        private record FileInfo
        {
            public FileInfo(string path, IFileSearcher fileSearcher)
            {
                Path = path;
                FileSearcher = fileSearcher;
            }
             
            public string Path;
            public IFileSearcher FileSearcher;
        }
    }
}
