﻿namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using NUnit.Framework;
    using Transport;

    public class When_receiving_delayed_message : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_expose_receiving_address(TransportTransactionMode transactionMode)
        {
            var onError = CreateTaskCompletionSource<ErrorContext>();

            await StartPump(
                (context, _) =>
                {
                    Assert.AreEqual(receiver.ReceiveAddress, context.ReceiveAddress);
                    throw new Exception("Simulated exception");
                },
                (context, _) =>
                {
                    onError.SetResult(context);
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            var dispatchProperties = new DispatchProperties
            {
                DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(5))
            };
            await SendMessage(InputQueueName, dispatchProperties: dispatchProperties);

            var errorContext = await onError.Task;
            Assert.AreEqual(receiver.ReceiveAddress, errorContext.ReceiveAddress);
        }
    }
}