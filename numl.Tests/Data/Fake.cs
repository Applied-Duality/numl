﻿using numl.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace numl.Tests.Data
{
    public class Fake1
    {
        [Feature]
        public int Age { get; set; }

        [Feature]
        public double Height { get; set; }

        [Feature]
        public string FirstName { get; set; }

        [StringFeature]
        public string LastName { get; set; }

        [Label]
        public string MiddleName { get; set; }
    }

    public class Fake2
    {
        [Feature]
        public string FirstName { get; set; }

        [StringFeature]
        public string LastName { get; set; }

        [Feature]
        public FakeEnum Fake { get; set; }

        [Label]
        public bool Purchased { get; set; }
    }
}
