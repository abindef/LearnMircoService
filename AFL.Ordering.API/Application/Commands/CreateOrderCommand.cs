﻿using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AFL.Ordering.API.Application.Commands
{
    public class CreateOrderCommand : IRequest<long>
    {

        //ublic CreateOrderCommand() { }
        public CreateOrderCommand(int itemCount)
        {
            ItemCount = itemCount;
        }

        public long ItemCount { get; private set; }
    }
}
