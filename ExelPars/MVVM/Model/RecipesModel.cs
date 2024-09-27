namespace ExcelPars.MVVM.Model
{
    public class RecipesModel
    {
        public string Year { get; set; }
        public string Direction { get; set; }
        public long CountHuman { get; set; }
        public decimal CountPackages { get; set; }
        public decimal SumCost { get; set; }

        public bool IsTotalRow { get; set; } = false;
    }
}