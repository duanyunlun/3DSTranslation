using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// 为了方便翻译而定义的条目格式，Id记录了该对话所在的头部序号信息，Text记录了对话内容
/// </summary>
public class Entry
{
    public int Id { get; set; }
    public string Text { get; set; }
}

/// <summary>
/// MBM文件处理类
/// </summary>
public static class MBM
{
    private static bool isInit;
    private static Encoding tranferEncoding;
    private static Dictionary<char, char> dicFullWidth2HalfWidth, dicHalfWidth2FullWidth;
    private static Dictionary<char, int> dicForSelectChineseWord;

    /// <summary>
    /// 初始化码表
    /// </summary>
    /// <param name="fullWidth2HalfWidth">全角转半角</param>
    /// <param name="halfWidth2FullWidth">半角转全角</param>
    /// <param name="chineseCode">中文字符</param>
    public static void Init(Dictionary<char, char> fullWidth2HalfWidth,
                            Dictionary<char, char> halfWidth2FullWidth,
                            Dictionary<char, int> chineseCode)
    {
        if (isInit)
            return;
        tranferEncoding = Encoding.GetEncoding("sjis");
        dicFullWidth2HalfWidth = fullWidth2HalfWidth;
        dicHalfWidth2FullWidth = halfWidth2FullWidth;
        dicForSelectChineseWord = chineseCode;
        isInit = true;
    }

    /// <summary>
    /// Altus的mbm文件格式首段有相关的文字组织信息，
    /// 第一行（16个字节）主要是相关的MagicCode，例如真4的MSG2，
    /// 第二行首个字节则是主要对话的段数（EntryCount)
    /// 第三行到第一段对话开始前的字节则是以下格式：
    /// 01 00 00 00 34 00 00 00 74 1A 00 00 00 00 00 00
    /// 对话ID(4字节=Int32)-对话内容长度(4字节-Int32)-该段对话偏移(8字节）
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static List<Entry> ByteArray2EntryList(byte[] bytes)
    {
        using (var br = new BinaryReader(new MemoryStream(bytes)))
        {
            br.ReadBytes(16);
            var entryCount = br.ReadInt32();
            br.ReadBytes(12);
            var entries = Enumerable.Range(0, 9999)
                          .Select(_ => (id: br.ReadInt32(), size: br.ReadInt32(), offset: (int)br.ReadInt64()))
                          .Where(x => x.offset != 0)
                          .Take(entryCount)
                          .ToList();
            return entries.Select(entry => new Entry { Id = entry.id, Text = ConvertBytesToString(br.ReadBytes(entry.size)) }).ToList();
        }
    }

    /// <summary>
    /// 将对话条目重新转成字节流，注意3DS的CPU是小端字节序（其实拿出来的数据也都是小端字节）
    /// </summary>
    /// <param name="entries"></param>
    /// <returns></returns>
    public static byte[] EntryList2ByteArray(this IEnumerable<Entry> entries)
    {
        var lst = entries.Select(entry => (id: entry.Id, bytes: ConvertStringToBytes(entry.Text))).ToList();

        var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            //头部数据写入
            bw.Write(0);
            bw.Write(0x3247534D); // MSG2
            bw.Write(0x10000);
            bw.Write(lst.Sum(x => x.bytes.Length + 16) + 32);
            bw.Write(lst.Count);
            bw.Write(0x20);
            bw.Write(0L);

            // Write entry table
            int prevId = -1;
            var stringOffset = lst.LastOrDefault().id * 16 + 48L;
            foreach (var (id, bytes) in lst)
            {
                bw.Write(new byte[16 * (id - prevId - 1)]);
                bw.Write(id);
                bw.Write(bytes.Length);
                bw.Write(stringOffset);

                prevId = id;
                stringOffset += bytes.Length;
            }

            // Write string data
            foreach (var (id, bytes) in lst)
                bw.Write(bytes);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// 将XML元素节点转换成对话条目
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static List<Entry> XElement2EntryList(XElement root)
    {
        return root.Elements("entry").Select(el => new Entry { Id = (int)el.Attribute("id"), Text = el.Element("target").Value }).ToList();
    }

    /// <summary>
    /// 将对话条目转换成XML元素节点
    /// </summary>
    /// <param name="entries"></param>
    /// <returns></returns>
    public static XElement EntryList2XElement(this IEnumerable<Entry> entries)
    {
        return new XElement("mbm", from entry in entries
                                   let idattr = new XAttribute("id", entry.Id)
                                   let source = new XElement("source", entry.Text)
                                   let target = new XElement("target", entry.Text)
                                   select new XElement("entry", idattr, source, target));
    }

    /// <summary>
    /// 根据真4的内容格式获取对话长度size
    /// </summary>
    /// <param name="byte0"></param>
    /// <param name="byte1"></param>
    /// <returns></returns>
    private static int[] getCodeSizes(byte byte0, byte byte1)
    {
        if (byte0 == 0x80)
        {
            switch (byte1)
            {
                case 1: case 2: case 0x12: case 0x16: case 0x17: case 0x18: case 0xFD: case 0xFE: return new int[0];
                default: return new[] { 1 };
            }
        }
        else
        {
            switch (byte1)
            {
                case 1: case 2: case 0x12: case 0x16: case 0x17: case 0x18: return new int[0];
                case 0x14: case 0x7C: return new[] { 1, 1 };
                case 0x13: return new[] { 2, 2 };
                case 0x7B: return new[] { 1, 1, 1, 1 };
                default: return new[] { 1 };
            }
        }
    }

    /// <summary>
    /// 将字符串转换成字节数组，此处需要注意与码表对应
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    static byte[] ConvertStringToBytes(string str)
    {
        return Regex.Split(str.Replace(@"\0", "{0000}") + "{FFFF}", "{([0-9A-F]{4}(?:,-?[0-9]+)*)}").SelectMany((s, i) =>
        {
            if (i % 2 == 0)
                return tranferEncoding.GetBytes(HW_FW_Transfer(s, dicHalfWidth2FullWidth));
            var byte0 = Convert.ToByte(s.Substring(0, 2), 16);
            var byte1 = Convert.ToByte(s.Substring(2, 2), 16);
            var sizes = getCodeSizes(byte0, byte1);
            return new[] { byte0, byte1 }.Concat(s.Split(',').Skip(1).Select(int.Parse).Zip(sizes, (num, size) =>
                size == 1 ? BitConverter.GetBytes((short)num) : BitConverter.GetBytes(num)).SelectMany(x => x));
        }).ToArray();
    }

    /// <summary>
    /// 将字节数组转换成字符串
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    static string ConvertBytesToString(byte[] bytes)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < bytes.Length / 2 - 1; i++)
        {
            var byte0 = bytes[2 * i];

            if (byte0 == 0x80 || byte0 == 0xf8)
            {
                var byte1 = bytes[2 * i + 1];
                sb.Append($"{{{byte0:X2}{byte1:X2}");
                foreach (var size in getCodeSizes(byte0, byte1))
                {
                    sb.Append($",{(size == 1 ? BitConverter.ToInt16(bytes, 2 * i + 2) : BitConverter.ToInt32(bytes, 2 * i + 2))}");
                    i += size;
                }
                sb.Append("}");
            }
            else
            {
                sb.Append(byte0 == 0 ? @"\0" : HW_FW_Transfer(tranferEncoding.GetString(bytes, 2 * i, 2), dicFullWidth2HalfWidth));
            }
        }
        return sb.ToString();
    }


    /// <summary>
    /// 用于从字典中找出对应的字符码/字符并串联起来（说白了就是在字典/码表中筛选并组成一串数据）
    /// </summary>
    /// <param name="str"></param>
    /// <param name="dic"></param>
    /// <returns></returns>
    private static string HW_FW_Transfer(string str, Dictionary<char, char> dic)
    {
        return string.Concat(str.Select(eachChar => dic.TryGetValue(eachChar, out var data) ? data : eachChar));
    }

}
