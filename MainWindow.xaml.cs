using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _3DSTranslationTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            initDictionary();

            TransferHandler.XML2MBM("F:\\e001.mbm.xml");
        }

        private void initDictionary()
        {
            string[] FWHW_Pairs = File.ReadAllLines(".\\CodeTable\\FW-HW.txt").Where(str => str.Length == 2).ToArray();
            string[] Chinese_Pairs = File.ReadAllLines(".\\CodeTable\\Shift-JIS-A.txt");
            Dictionary<char, int> chineseDic = Chinese_Pairs.Where(pair => pair.Length > 0)
                                                            .ToDictionary(pair => pair.Split(new string[] { "=>" }, StringSplitOptions.None)[1].ToCharArray().First(), 
                                                                          pair => int.Parse(pair.Split(new string[] { "=>" }, StringSplitOptions.None)[0], NumberStyles.HexNumber));
            MBM.Init(FWHW_Pairs.ToDictionary(p => p[0], p => p[1]),
                     FWHW_Pairs.ToDictionary(p => p[1], p => p[0]),
                     chineseDic);
        }
    }
}
