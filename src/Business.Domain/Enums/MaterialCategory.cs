using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum MaterialCategory
{
    [Display(Name = "Paslanmaz")]
    Stainless = 0,
    [Display(Name = "Çelik")]
    Steel = 1,
    [Display(Name = "Alüminyum")]
    Aluminum = 2,
    [Display(Name = "Bronz")]
    Bronze = 3,
    [Display(Name = "Plastikler")]
    Plastic = 4,
    [Display(Name = "Rulman")]
    Bearing = 5,
    [Display(Name = "Motor")]
    Motor = 6,
    [Display(Name = "Redüktör")]
    Gearbox = 7,
    [Display(Name = "Cıvata")]
    Bolt = 8,
    [Display(Name = "Kılavuz")]
    Tap = 9,
    [Display(Name = "Paslanmaz Saç Yüzey")]
    StainlessSheetSurface = 10,
    [Display(Name = "Diğer")]
    Other = 99
}
