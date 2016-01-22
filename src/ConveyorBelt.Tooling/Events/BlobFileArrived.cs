﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBelt.Tooling.Events
{
    public class BlobFileArrived
    {
        public DiagnosticsSourceSummary Source { get; set; }

        public string BlobId { get; set; }

        public long? Position { get; set; }
    }
}
