using System;
using System.Collections.Generic;

#nullable disable

namespace DevicePortal.Importer
{
    public partial class FnwiPortal
    {
        public string Naam { get; set; }
        public string Merk { get; set; }
        public string Type { get; set; }
        public string Serienummer { get; set; }
        public string Soort { get; set; }
        public string LoginGebruiker { get; set; }
        public string Email { get; set; }
        public string Klantorganisatie { get; set; }
        public string Status { get; set; }
        public DateTime? Aanschafdatum { get; set; }
        public string Kostenplaats { get; set; }
        public DateTime? DatumLaatstGezien { get; set; }
        public string ItracsGebouw { get; set; }
        public string ItracsRuimte { get; set; }
        public string ItracsOutlet { get; set; }
        public string Macadres { get; set; }
        public string NotitiesKlant { get; set; }
        public bool Versleuteld { get; set; }
        public string GeregistreerdDoorGebruiker { get; set; }
        public string Besturingssysteem { get; set; }
    }
}
