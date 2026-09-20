using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Escalations.Requests
{
    public class SaveEscalationParametersRequest
    {
        public Guid ContractId { get; set; }
        public decimal LabourWeight { get; set; }
        public decimal FuelWeight { get; set; }
        public decimal CpiWeight { get; set; }
        public int BaseMonth { get; set; }
        public int BaseYear { get; set; }
        public decimal LabourBaseValue { get; set; }
        public decimal FuelBaseValue { get; set; }
        public decimal CpiBaseValue { get; set; }
    }
}
