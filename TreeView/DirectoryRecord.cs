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
                return from di in Info.GetDirectories("*", SearchOption.TopDirectoryOnly)
                       select new DirectoryRecord { Info = di };
            }
        }
    }
}
