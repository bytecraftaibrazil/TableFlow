using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableFlow.Api.Models
{
    public record SuggestedTable
    (
        int Id,
        int Number,
        int Capacity
    );
}