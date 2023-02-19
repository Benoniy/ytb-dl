using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace ytb_dl
{
    class Constants
    {
        public static string link = "";
        public static List<string[]> jobs = new List<string[]>();
        public static ObservableCollection<CheckBoxItem> checkedListBoxVid = new ObservableCollection<CheckBoxItem>();
        public static ObservableCollection<CheckBoxItem> checkedListBoxAud = new ObservableCollection<CheckBoxItem>();
        public static int totaljobs = 0;
        public static bool audOnly = false;
    }
}
