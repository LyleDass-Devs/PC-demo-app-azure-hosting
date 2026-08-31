using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts
{
    public abstract class BaseContract
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public Guid? ModifiedBy { get; set; }
    }
}
