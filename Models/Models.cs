namespace FreeKantar.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public override string ToString() => Name;
    }

    public class WeighingRecord
    {
        public int Id { get; set; }
        public string TransactionType { get; set; } // SEVK, IADE
        public int ProductId { get; set; }
        public string ProductName { get; set; } // For display
        public string DriverName { get; set; }
        public string DriverSurname { get; set; }
        public string DriverPhone { get; set; }
        public string Plate { get; set; }
        public string Destination { get; set; }
        public string Description { get; set; }
        public double FirstWeight { get; set; }
        public double? SecondWeight { get; set; }
        public double? ThirdWeight { get; set; }
        public string WeightType { get; set; } = "Kantardan Tartıldı";
        
        public double NetWeight {
            get {
                if (TransactionType == "İADE + İLAVE SEVK" && ThirdWeight.HasValue) 
                    return Math.Abs((SecondWeight ?? 0) - ThirdWeight.Value);
                if (SecondWeight.HasValue)
                    return Math.Abs(SecondWeight.Value - FirstWeight);
                return 0;
            }
        }

        public double AdditionalWeight => (TransactionType == "İADE + İLAVE SEVK" && SecondWeight.HasValue) ? (SecondWeight.Value - FirstWeight) : 0;
        public double OriginalReturnWeight => (TransactionType == "İADE + İLAVE SEVK" && ThirdWeight.HasValue) ? (FirstWeight - ThirdWeight.Value) : 0;

        public DateTime FirstWeightDate { get; set; }
        public DateTime? SecondWeightDate { get; set; }
        public DateTime? ThirdWeightDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; }

        public string DisplayStatus => IsDeleted ? "SİLİNDİ" : (IsCompleted ? "Bitti" : "İlk Tartım Yapıldı");
        public string DisplayDate => (SecondWeightDate ?? FirstWeightDate).ToString("dd.MM.yyyy HH:mm");
    }

    public class AppSettings
    {
        public string ComPort { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
    }
}
