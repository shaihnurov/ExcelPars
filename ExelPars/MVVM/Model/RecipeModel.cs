using System;

namespace ExcelPars.MVVM.Model
{
    public class RecipeModel
    {
        public string id { get; set; }
        public string date { get; set; }
        public int nomk_ls { get; set; }
        public int Owner { get; set; } 
        public decimal ko_all { get; set; }
        public decimal sl_all { get; set; }
    }
}