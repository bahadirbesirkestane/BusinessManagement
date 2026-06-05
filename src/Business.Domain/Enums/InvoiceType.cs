using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum InvoiceType
{
    [Display(Name = "Satış Faturası")]
    Sales = 0,
    [Display(Name = "Alış Faturası")]
    Purchase = 1,
    [Display(Name = "Gider Faturası")]
    Expense = 2,
    [Display(Name = "İade Faturası")]
    Return = 3
}
