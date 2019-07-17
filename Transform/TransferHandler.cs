using System.IO;
using System.Xml.Linq;

public static class TransferHandler
{
    public const string XMLExtension = ".xml";
    public const string MBMExtension = ".mbm";
    public const string POExtension = ".po";

    public static void MBM2XML(string path)
    {
        FileInfo info = new FileInfo(path);

        if ((info.Attributes & FileAttributes.Directory) != 0)
        {
            foreach (var filefound in Directory.GetFiles(path, "*" + MBMExtension, SearchOption.AllDirectories))
            {
                File.WriteAllText(filefound + XMLExtension, MBM.ByteArray2EntryList(File.ReadAllBytes(filefound)).EntryList2XElement().ToString());
            }
        }
        else
        {
            File.WriteAllText(path + XMLExtension, MBM.ByteArray2EntryList(File.ReadAllBytes(path)).EntryList2XElement().ToString());
        }
    }
    public static void XML2MBM(string path)
    {
        FileInfo info = new FileInfo(path);

        if ((info.Attributes & FileAttributes.Directory) != 0)
        {
            foreach (var filefound in Directory.GetFiles(path, "*" + XMLExtension, SearchOption.AllDirectories))
            {
                File.WriteAllBytes(filefound + MBMExtension, MBM.XElement2EntryList(XElement.Load(filefound)).EntryList2ByteArray());
            }
        }
        else
        {
            File.WriteAllBytes(path + MBMExtension, MBM.XElement2EntryList(XElement.Load(path)).EntryList2ByteArray());
        }
    }

    public static void XML2PO(string path)
    {
        FileInfo info = new FileInfo(path);

        if ((info.Attributes & FileAttributes.Directory) != 0)
        {
            foreach (var filefound in Directory.GetFiles(path, "*" + XMLExtension, SearchOption.AllDirectories))
                PO.XML2PO(filefound);
        }
        else
        {
            PO.XML2PO(path);
        }
    }

    public static void PO2XML(string path)
    {
        FileInfo info = new FileInfo(path);

        if ((info.Attributes & FileAttributes.Directory) != 0)
        {
            foreach (var filefound in Directory.GetFiles(path, "*" + POExtension, SearchOption.AllDirectories))
                File.WriteAllText(filefound.Replace(POExtension, XMLExtension), PO.PO2XML(filefound).ToString());
        }
        else
        {
            File.WriteAllText(path.Replace(POExtension, XMLExtension), PO.PO2XML(path).ToString());
        }
    }
}