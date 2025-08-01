using System;
using System.Collections.Generic;

namespace TimeRecorderBACKEND.Dtos
{
    public class SummaryListDto
    {
        public List<SummaryDto> Summaries { get; set; } = new List<SummaryDto>();
    }
}