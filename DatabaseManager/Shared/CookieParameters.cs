﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class CookieParameters
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public int ExpirationDays { get; set; }
    }
}
