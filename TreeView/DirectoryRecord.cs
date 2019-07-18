using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _3DSTranslationTool
{
    public class DirectoryRecord
    {
        public DirectoryInfo Info { get; set; }

        public IEnumerable<FileInfo> Files
        {
            get
            {
                return Info.GetFiles();
            }
        }

        public IEnumerable<DirectoryRecord> Directories
        {
            get
            {
                return from directoryInfo in Info.GetDirectories("*", SearchOption.TopDirectoryOnly)
                       select new DirectoryRecord { Info = directoryInfo };
            }
        }
    }
}
