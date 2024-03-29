﻿using AFL.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFL.Ordering.Domain.OrderAggregate
{
    public class Address : ValueObject
    {
        public string Street { get; private set; }
        public string City { get; private set; }
        public string ZipCode { get; private set; }

        public Address() { }
        public Address(string street, string city, string zipcode)
        {
            Street = street;
            City = city;
            ZipCode = zipcode;
        }
        //此处重载了获取原子值的方法
        protected override IEnumerable<object> GetAtomicValues()
        {

            // Using a yield return statement to return each element one at a time
            yield return Street;
            yield return City;
            yield return ZipCode;
        }
    }
}
