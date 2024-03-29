﻿using AFL.Infrastructure.Core.Behaviors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFL.Ordering.Infrastructure
{
    public class OrderingContextTransactionBehavior<TRequest, TResponse> : TransactionBehavior<OrderingContext, TRequest, TResponse>
    {
        public OrderingContextTransactionBehavior(OrderingContext dbContext, ILogger<OrderingContextTransactionBehavior<TRequest, TResponse>> logger) : base(dbContext, logger)
        {
        }
    }
}
