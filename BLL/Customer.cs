using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBank;

public class Customer
{
    public string Name { get; set; } = String.Empty;
    public string City { get; set; } = String.Empty;

    public Customer(string name, string city) {
        Name = name;
        City = city;
    }
}
