﻿using System.Linq;
using System.Xml.Linq;
using Yarhl.FileFormat;
using Yarhl.Media.Text;

public static class PO
{
    public static void XML2PO(string file)
    {
        Po POHeader = new Po
        {
            Header = new PoHeader("Shin Megami Tensei IV", "smtivesp@gmail.com", "es")
            {
                LanguageTeam = "TraduSquare (SMTIVesp)",
            }
        };

        XElement XML = XElement.Load(file);
        int c = 0;
        foreach (XElement entry in XML.Descendants("entry"))
        {
            string target;
            string source = entry.Element("source").Value;
            if (string.IsNullOrEmpty(source))
                source = "<empty>";
            target = entry.Element("target").Value;
            if (string.IsNullOrEmpty(target))
                target = "";
            POExport(POHeader, source.Replace("{F801}", "\n"), target.Replace("{F801}", "\n"), c);
            c++;
        }
        POWrite(POHeader, file.Replace(".xml", ""));
    }

    public static XElement PO2XML(string file)
    {
        Po POBuffer;
        using (var binary = new BinaryFormat(file))
        {
            POBuffer = binary.ConvertTo<Po>();
            return new XElement("mbm", from entry in POBuffer.Entries
                                       let idattr = new XAttribute("id", entry.Context)
                                       let source = new XElement("source", entry.Original.Replace("\n", "{F801}").Replace("<empty>", ""))
                                       let target = new XElement("target", entry.Text.Replace("\n", "{F801}").Replace("<empty>", ""))
                                       select new XElement("entry", idattr, source, target));
        }
    }

    public static void POExport(Po POHeader, string source, string target, int i)
    {
        POHeader.Add(new PoEntry(source) { Translated = target, Context = i.ToString() });
    }

    public static void POWrite(Po POHeader, string file)
    {
        POHeader.ConvertTo<BinaryFormat>().Stream.WriteTo(file + ".po");
    }
}
