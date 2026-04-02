using System;
using System.Collections.Generic;

namespace MaintainingOrdersWeb.Models;

public partial class Client
{
    public int ClientId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? ContactPerson { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
