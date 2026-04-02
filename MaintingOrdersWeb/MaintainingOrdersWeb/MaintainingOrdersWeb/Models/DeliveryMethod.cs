using System;
using System.Collections.Generic;

namespace MaintainingOrdersWeb.Models;

public partial class DeliveryMethod
{
    public int MethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public decimal Price { get; set; }

    public string? EstimatedTime { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
